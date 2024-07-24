namespace DbUpdater;

public static partial class Regexes
{
    public static string RegexReplaceSchema(this string content) => _schemaRegex().Replace(content, _schemaEval);
    public static string RegexReplaceDrop(this string content) => _dropRegex().Replace(content, _dropEval);
    public static string RegexReplaceDefaultNumVals(this string content) => _smallintRegex().Replace(content, _smallintEval);
    public static string RegexReplaceTruncate(this string content) => _truncateRegex().Replace(content, _truncateEval);


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

    [GeneratedRegex("(?<type>smallint|numeric) DEFAULT '\\((?<num>\\d+?)\\)'", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex _smallintRegex();

    private static MatchEvaluator _smallintEval = new
    (
        match => $"{match.Groups["type"]} DEFAULT '{match.Groups["num"]}'"
    );

    [GeneratedRegex("TRUNCATE(?<what>(.(?!CASCADE))+?);", RegexOptions.ExplicitCapture)]
    private static partial Regex _truncateRegex();

    private static MatchEvaluator _truncateEval = new
    (
        match => $"TRUNCATE{match.Groups["what"]} CASCADE;"
    );
}
