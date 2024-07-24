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

//Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(filesToUpdate));
//return;

Parallel.ForEach(filesToUpdate, file =>
{
    var content = File.ReadAllText(file).Replace("\\connect", "--\\connect")
        .RegexReplaceDefaultNumVals()
        .RegexReplaceDrop()
        //.RegexReplaceSchema()
        .Replace("CREATE SEQUENCE", "--CREATE SEQUENCE")
        .Replace("DROP SEQUENCE", "--DROP SEQUENCE")
        //.RegexReplaceTruncate()
        ;

    File.WriteAllText(file.Replace(".sql", "_UPDATED.sql"), content);
});
