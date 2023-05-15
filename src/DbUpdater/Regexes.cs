namespace DbUpdater;

public partial class Regexes
{
    public static string Schema(string content) => _schemaRegex().Replace(content, _schemaEval);
    public static string Drop(string content) => _dropRegex().Replace(content, _dropEval);


    [GeneratedRegex(@".*?(SCHEMA|(FUNCTION(?!\w|.*?')(.|\n)*?;))", RegexOptions.ExplicitCapture)]
    private static partial Regex _schemaRegex();

    private static MatchEvaluator _schemaEval = new
    (
        match => $"--{match.Value.Replace("\n", "\n--")}"
    );

    [GeneratedRegex("DROP(?<what>(.(?!CASCADE))+?);", RegexOptions.ExplicitCapture)]
    private static partial Regex _dropRegex();

    private static MatchEvaluator _dropEval = new
    (
        match => $"DROP{match.Groups["what"]} CASCADE;"
    );
}
