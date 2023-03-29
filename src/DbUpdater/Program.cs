using System.Text.RegularExpressions;

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

var schemaEvaluator = new MatchEvaluator(match => $"--{match.Value.Replace("\n", "\n--")}");
var dropEvaluator = new MatchEvaluator(match => $"DROP{match.Groups["what"]} CASCADE;");

foreach (var file in Directory.EnumerateFiles("/McRAI/dumps/").Where(f => Regex.IsMatch(f, $@"[^\\/]*?{webName}(.(?!_UPDATED))*?\.sql")))
{
    var content = File.ReadAllText(file).Replace("\\connect", "--\\connect");
    content = DropRegex().Replace(content, dropEvaluator);
    content = SchemaRegex().Replace(content, schemaEvaluator);

    File.WriteAllText(file.Replace(".sql", "_UPDATED.sql"), content);
}

partial class Program
{
    [GeneratedRegex(".*?(SCHEMA|(FUNCTION(?!\\w)(.|\n)*?;))", RegexOptions.ExplicitCapture)]
    private static partial Regex SchemaRegex();

    [GeneratedRegex("DROP(?<what>(.(?!CASCADE))+?);", RegexOptions.ExplicitCapture)]
    private static partial Regex DropRegex();
}
