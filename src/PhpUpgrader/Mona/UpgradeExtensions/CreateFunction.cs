namespace PhpUpgrader.Mona.UpgradeExtensions;

public static class CreateFunction
{
    public static FileWrapper UpgradeCreateFunction(this FileWrapper file)
    {
        if (file.Content.Contains("create_function"))
        {
            var evaluator = new MatchEvaluator(CreateFunctionToAnonymousFunction);
            var content = file.Content.ToString();
            var updated = Regex.Replace(content,
                                        @"(?<ws>(\t| )*)@?create_function\s?\(\s*'(?<args>.*)'\s?,\s*(?<quote>'|"")(?<code>(.|\n)*?)\k<quote>\s*\)",
                                        evaluator,
                                        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                                        TimeSpan.FromSeconds(5));

            file.Content.Replace(content, updated);
        }
        return file;
    }

    private static string CreateFunctionToAnonymousFunction(Match match)
    {
        var whitespace = match.Groups["ws"].Value;
        var args = match.Groups["args"].Value;
        var code = match.Groups["code"].Value.Replace("\n", $"\n{whitespace}", StringComparison.Ordinal)
                                             .Replace($"\n{whitespace}{whitespace}", $"\n{whitespace}", StringComparison.Ordinal);

        if (string.Equals(match.Groups["quote"].Value, "\"", StringComparison.Ordinal))
        {
            var evaluator = new MatchEvaluator(DoubleQuoteStringCodeEvaluator);
            code = Regex.Replace(code,
                                 @"(\\(\$|\\|""))|('\$.*?')",
                                 evaluator,
                                 RegexOptions.ExplicitCapture,
                                 TimeSpan.FromSeconds(3));
        }
        return $"{whitespace}function ({args}) {{\n{whitespace}    {code}\n{whitespace}}}";
    }

    private static string DoubleQuoteStringCodeEvaluator(Match match)
    {
        if (match.Value.Length == 2) //escapovaný znak
        {
            return match.Value[1].ToString();
        }
        //proměnná, jejíž string reprezentace měla být v jednoduchých uvozovkách
        //'$var' >> "$var" zajistí, že její hodnota bude v tomto stringu.
        return match.Value.Replace('\'', '"');
    }
}

/* TODO: piwika/libs/HTML/QuickForm2/Rule/Compare.php
protected function validateOwner()
    {
        $value  = $this->owner->getValue();
        $config = $this->getConfig();
        if (!in_array($config['operator'], array('===', '!=='))) {
            $compareFn = create_function(
                '$a, $b', 'return floatval($a) ' . $config['operator'] . ' floatval($b);'
            );
        } else {
            $compareFn = create_function(
                '$a, $b', 'return strval($a) ' . $config['operator'] . ' strval($b);'
            );
        }
        return $compareFn($value, $config['operand'] instanceof HTML_QuickForm2_Node
                                  ? $config['operand']->getValue(): $config['operand']);
    }
*/

/* TODO: piwika/plugins/CustomVariables/API.php
public function getCustomVariablesValuesFromNameId($idSite, $period, $date, $idSubtable, $segment = false, $_leavePriceViewedColumn = false)
    {
        $dataTable = $this->getDataTable($idSite, $period, $date, $segment, $expanded = false, $idSubtable);

        if (!$_leavePriceViewedColumn) {
            $dataTable->deleteColumn('price_viewed');
        } else {
            // Hack Ecommerce product price tracking to display correctly
            $dataTable->renameColumn('price_viewed', 'price');
        }
        $dataTable->queueFilter('ColumnCallbackReplace', array('label', create_function('$label', '
			return $label == Piwik_CustomVariables::LABEL_CUSTOM_VALUE_NOT_DEFINED 
				? "' . Piwik_Translate('General_NotDefined', Piwik_Translate('CustomVariables_ColumnCustomVariableValue')) . '"
				: $label;')));
        return $dataTable;
    }
*/

/* TODO: piwika/plugins/MobileMessaging/ReportRenderer/Sms.php
public function renderReport($processedReport)
    {
        $isGoalPluginEnabled = Piwik_Common::isGoalPluginEnabled();
        $prettyDate = $processedReport['prettyDate'];
        $reportData = $processedReport['reportData'];

        $evolutionMetrics = array();
        $multiSitesAPIMetrics = Piwik_MultiSites_API::getApiMetrics($enhanced = true);
        foreach ($multiSitesAPIMetrics as $metricSettings) {
            $evolutionMetrics[] = $metricSettings[Piwik_MultiSites_API::METRIC_EVOLUTION_COL_NAME_KEY];
        }

        // no decimal for all metrics to shorten SMS content (keeps the monetary sign for revenue metrics)
        $reportData->filter(
            'ColumnCallbackReplace',
            array(
                 array_merge(array_keys($multiSitesAPIMetrics), $evolutionMetrics),
                 create_function(
                     '$value',
                     '
                     return preg_replace_callback (
                         "' . self::FLOAT_REGEXP . '",
						create_function (
							\'$matches\',
							\'return round($matches[0]);\'
						),
						$value
					);
					'
                 )
            )
        );
 */

/* TODO: piwika/plugins/SitesManager/API.php (DIFF)
public function getCurrencyList()
    {
        $currencies = Piwik::getCurrencyList();
        return array_map(create_function('$a', 'return $a[1]." (".$a[0].")";'), $currencies);
    }

public function getCurrencySymbols()
{
        $currencies = Piwik::getCurrencyList();
    return array_map(create_function('$a', 'return $a[0];'), $currencies);
}

===========================================================================================================

public function getCurrencyList()
    {
        $currencies = Piwik::getCurrencyList();
        return array_map(function ($a) {
    return $a[1]." (".$a[0].")";
}, $currencies);
    }


public function getCurrencySymbols()
{
        $currencies = Piwik::getCurrencyList();
    return array_map(function($a) {
        return $a[0];
    }, $currencies);
}
 */
