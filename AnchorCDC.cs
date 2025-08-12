public const int DefaultMinEntries = 400;
public const int DefaultMaskBits = 10;

private static List<(string FilePath, List Entries)>
    GetChunkedEntriesGroupByFilename(
        ILocalizationTable table,
        string targetPath,
        string tableName,
        string className,
        int minEntries = DefaultMinEntries,
        int maskBits = DefaultMaskBits)
{
    var entries = table.GetAllEntries(null).ToList();

    if (entries.Count <= MAX_ENTRIES_PER_FILE)
    {
        var csFilePath = $"{targetPath}/{className}.gen.cs";
        return new List<(string FilePath, List Entries)>
        {
            (csFilePath, entries),
        };
    }

    var keys = entries.Select(e => e.Key).ToList();
    var keyChunks = AnchorCdc(keys, minEntries, maskBits);

    var map = entries.ToDictionary(e => e.Key, e => e);
    var results = new List<(string FilePath, List Entries)>(keyChunks.Count);

    foreach (var chunk in keyChunks)
    {
        var firstKey = chunk[0];

        var h128 = default(Hash128);
        h128.Append(className);
        h128.Append(firstKey);
        var shard = h128.ToString()[..8];

        var filePath = $"{targetPath}/{tableName}/{className}.gen.{shard}.cs";
        var list = new List(chunk.Count);
        foreach (var k in chunk) list.Add(map[k]);

        results.Add((filePath, list));
    }

    return results;
}

private static IList> AnchorCdc(
    IList keys,
    int minEntries = DefaultMinEntries,
    int maskBits = DefaultMaskBits)
{
    if (keys == null) throw new ArgumentNullException(nameof(keys));
    if (minEntries <= 0) throw new ArgumentOutOfRangeException(nameof(minEntries));
    if (maskBits < 0) throw new ArgumentOutOfRangeException(nameof(maskBits));

    var chunks = CreateChunks(keys, minEntries, maskBits);
    return chunks.ConvertAll>(chunk => (IList)chunk);
}

private static List> CreateChunks(
    IList source,
    int minEntries,
    int maskBits)
{
    var mask = maskBits >= 64
        ? ulong.MaxValue
        : (1UL << maskBits) - 1;

    var result = new List>();
    var current = new List(minEntries);

    foreach (var key in source)
    {
        current.Add(key);

        // Unity Hash128 사용
        var h = default(Hash128);
        h.Append(key);
        var hex = h.ToString(); // 32 hex chars
        var h64 = Convert.ToUInt64(hex.Substring(0, 16), 16); // 상위 64비트 사용

        var isAnchor = (h64 & mask) == 0;
        if (current.Count >= minEntries && isAnchor)
        {
            result.Add(current);
            current = new List(minEntries);
        }
    }

    if (current.Count > 0)
        result.Add(current);

    return result;
}
