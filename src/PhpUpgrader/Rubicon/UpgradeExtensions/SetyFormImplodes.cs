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
                         ".\"', \".(empty($cena_od) ? 'NULL' : $cena_od).\", \".(empty($cena_do) ? 'NULL' : $cena_do).\", '\".")
                .Replace("<?PHP /*\r\n            <div class=\"obal_posuvnik\">\r\n              <div class=\"noUi-control noUi-danger\" id=\"slider_form\"></div>\r\n              <div class=\"obal_pod_posuvnik_form\">\r\n                <input id=\"slider_form_input_start\" type=\"text\" class=\"posuvnik_input_form\" name=\"from\">\r\n                <span class=\"posuvnik_text_mezi_form\">do</span>\r\n                <input id=\"slider_form_input_end\" type=\"text\" class=\"posuvnik_input_form\" name=\"to\">\r\n                <span class=\"posuvnik_text_mena_form\">Kč</span>\r\n              </div>\r\n            </div>\r\n\t\t\t*/ ?>",
                         "<div class=\"obal_posuvnik\" style=\"display:none\">\r\n              <div class=\"noUi-control noUi-danger\" id=\"slider_form\"></div>\r\n              <div class=\"obal_pod_posuvnik_form\">\r\n                <input id=\"slider_form_input_start\" type=\"text\" class=\"posuvnik_input_form\" name=\"from\">\r\n                <span class=\"posuvnik_text_mezi_form\">do</span>\r\n                <input id=\"slider_form_input_end\" type=\"text\" class=\"posuvnik_input_form\" name=\"to\">\r\n                <span class=\"posuvnik_text_mena_form\">Kč</span>\r\n              </div>\r\n            </div>")
                .Replace("<?PHP /*\n            <div class=\"obal_posuvnik\">\n              <div class=\"noUi-control noUi-danger\" id=\"slider_form\"></div>\n              <div class=\"obal_pod_posuvnik_form\">\n                <input id=\"slider_form_input_start\" type=\"text\" class=\"posuvnik_input_form\" name=\"from\">\n                <span class=\"posuvnik_text_mezi_form\">do</span>\n                <input id=\"slider_form_input_end\" type=\"text\" class=\"posuvnik_input_form\" name=\"to\">\n                <span class=\"posuvnik_text_mena_form\">Kč</span>\n              </div>\n            </div>\n\t\t\t*/ ?>",
                         "<div class=\"obal_posuvnik\" style=\"display:none\">\n              <div class=\"noUi-control noUi-danger\" id=\"slider_form\"></div>\n              <div class=\"obal_pod_posuvnik_form\">\n                <input id=\"slider_form_input_start\" type=\"text\" class=\"posuvnik_input_form\" name=\"from\">\n                <span class=\"posuvnik_text_mezi_form\">do</span>\n                <input id=\"slider_form_input_end\" type=\"text\" class=\"posuvnik_input_form\" name=\"to\">\n                <span class=\"posuvnik_text_mena_form\">Kč</span>\n              </div>\n            </div>")
                .Replace("<input id=\"cenova_relace_1\" class=\"checkbox_pod_form\" type=\"radio\" name=\"cena\" value=\"do 15.000\" onchange=\"zmena_cen(0,15000);\" />",
                         "<input id=\"cenova_relace_1\" class=\"checkbox_pod_form\" type=\"radio\" name=\"cena\" value=\"do 50.000\" onchange=\"zmena_cen(0,50000);\" />");
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
