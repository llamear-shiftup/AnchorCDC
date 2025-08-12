private static List<(string FilePath, List Entries)> GetChunkedEntriesGroupByFilename(
    ILocalizationTable table,
    string targetPath,
    string tableName,
    string className)
{
    var entries = table.GetAllEntries(null).ToList();
    foreach (var e in entries)
    {
        Debug.Log(e.Key);
    }

    if (entries.Count <= MAX_ENTRIES_PER_FILE)
    {
        var csFilePath = $"{targetPath}/{className}.gen.cs";
        return new List<(string FilePath, List Entries)>
        {
            (csFilePath, entries),
        };
    }

    var digit = Math.Max(0, (int)MathF.Floor(MathF.Log(1.0f * entries.Count / MAX_ENTRIES_PER_FILE, 16)) + 1);
    return entries.Select((x, i) => (x, i))
        .GroupBy(t =>
        {
            var hash = default(Hash128);
            hash.Append(className);
            hash.Append(t.x.Key);
            var shard = hash.ToString()[..digit];
            return $"{targetPath}/{tableName}/{className}.gen.{shard}.cs";
        })
        .Select(g => (g.Key, List: g.Select(t => t.x).ToList()))
        .ToList();;
}
