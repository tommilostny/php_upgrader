using System.Text.RegularExpressions;

string webName;
try
{
    webName = args.First();
}
catch (Exception)
{
    Console.Error.WriteLine("Please provide a web name as a program argument.");
    return;
}

var evaluator = new MatchEvaluator(match => $"--{match.Value.Replace("\n", "\n--")}");

foreach (var file in Directory.EnumerateFiles("/McRAI/dumps/").Where(f => Regex.IsMatch(f, $@"[^\\/]*?{webName}(.(?!_UPDATED))*?\.sql")))
{
    var content = File.ReadAllText(file);
    File.WriteAllText(file.Replace(".sql", "_UPDATED.sql"), SchemaRegex().Replace(content, evaluator));
}

partial class Program
{
    [GeneratedRegex(".*?(SCHEMA|(FUNCTION(.|\n)*?;))", RegexOptions.ExplicitCapture)]
    private static partial Regex SchemaRegex();
}
