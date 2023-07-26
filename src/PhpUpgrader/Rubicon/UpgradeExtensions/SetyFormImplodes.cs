namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class SetyFormImplodes
{
    public static FileWrapper UpgradeSetyFormImplodes(this FileWrapper file)
    {
        if (file.Path.EndsWith("sety_form.php", StringComparison.Ordinal))
        {
            var startIndex = file.Content.IndexOf("$ins_query = \"INSERT INTO demand_sets VALUES");
            var endIndex = file.Content.IndexOf("$result = pg_query($ins_query);", startIndex);
            var count = endIndex - startIndex;

            Span<char> area = stackalloc char[count];
            file.Content.CopyTo(startIndex, area, count);
            var areaStr = area.ToString();

            file.Content.Replace(areaStr, ImplodeRegex().Replace(areaStr, _wrapInTernaryEval))
                .Replace("if (count($_POST['obsluha'] > 0)) {",
                         "if (isset($_POST['obsluha']) && count($_POST['obsluha'] > 0)) {")
                .Replace(".\"', '\".$cena_od.\"', '\".$cena_do.\"', '\".",
                         ".\"', \".(empty($cena_od) ? 'NULL' : $cena_od).\", \".(empty($cena_od) ? 'NULL' : $cena_do).\", '\".");
        }
        return file;
    }

    [GeneratedRegex(@"implode.*?(?<var>\$.*?)\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex ImplodeRegex();

    private static MatchEvaluator _wrapInTernaryEval = new(match =>
    {
        return $"(isset({match.Groups["var"]}) ? {match.Value} : '')";
    });
}
