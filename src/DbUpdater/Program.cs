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

var schemaEval = new MatchEvaluator
(
    match => $"--{match.Value.Replace("\n", "\n--")}"
);
var dropEval = new MatchEvaluator
(
    match => $"DROP{match.Groups["what"]} CASCADE;"
);
var dropCreateEval = new MatchEvaluator
(
    match => match.Groups["w"].Value == "TABLE" ? $"{match.Groups["s"]}VIEW{match.Groups["e"]}" : match.Value
);

foreach (var file in Directory.EnumerateFiles("/McRAI/dumps/").Where(f => Regex.IsMatch(f, $@"[^\\/]*?{webName}(.(?!_UPDATED))*?\.sql")))
{
    var content = File.ReadAllText(file).Replace("\\connect", "--\\connect");
    //content = DropCreateRegex().Replace(content, dropCreateEval);
    content = DropRegex().Replace(content, dropEval);
    content = SchemaRegex().Replace(content, schemaEval);

    File.WriteAllText(file.Replace(".sql", "_UPDATED.sql"), content);
}

partial class Program
{
    [GeneratedRegex(@".*?(SCHEMA|(FUNCTION(?!\w)(.|\n)*?;))", RegexOptions.ExplicitCapture)]
    private static partial Regex SchemaRegex();

    [GeneratedRegex("DROP(?<what>(.(?!CASCADE))+?);", RegexOptions.ExplicitCapture)]
    private static partial Regex DropRegex();

    [GeneratedRegex(@"(?<s>DROP VIEW(.|\n)*?CREATE\s)(?<w>\w+?)(?<e>\s(.|\n)*?;)", RegexOptions.ExplicitCapture)]
    private static partial Regex DropCreateRegex();
}
