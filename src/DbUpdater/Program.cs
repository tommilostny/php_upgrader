string webName;
try
{
    webName = args.First();
}
catch
{
    Console.Error.WriteLine("Please provide a web name as a program argument.");
    return;
}

var filesToUpdate = Directory
    .EnumerateFiles("/McRAI/dumps/")
    .Where(f => Regex
        .IsMatch(f, $@"[^\\/]*?{webName}(.(?!_UPDATED))*?\.sql"));

Parallel.ForEach(filesToUpdate, file =>
{
    var content = File.ReadAllText(file).Replace("\\connect", "--\\connect")
        .RegexReplaceDefaultNumVals()
        .RegexReplaceDrop()
        .RegexReplaceSchema();

    File.WriteAllText(file.Replace(".sql", "_UPDATED.sql"), content);
});
