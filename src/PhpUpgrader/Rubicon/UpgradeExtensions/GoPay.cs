namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class GoPay
{
    private static readonly string _mcGoPaySetupPHP = Path.Join("classes", "McGoPay.setup.php");
    private static readonly string _mcGoPayPHP = Path.Join("classes", "McGoPay.php");
    private static readonly string _cartStep02PHP = Path.Join("card", "step_02.php");
    private static readonly string _cartStep04PHP = Path.Join("card", "step_04.php");
    private static readonly string _aegisxOrderDetailPHP = Path.Join("aegisx", "objednavka.php");
    private static readonly string _aegisxOrdersListPHP = Path.Join("aegisx", "objednavky.php");
    private static string? _mcGoPayHelperPHP = null;

    private static readonly string[] _allowedWebNames = { "nicom" };

    public static FileWrapper UpgradeGoPay(this FileWrapper file, PhpUpgraderBase upgrader)
    {
        UpgradeMcGoPay(file);
        if (_allowedWebNames.Contains(upgrader.WebName, StringComparer.Ordinal))
        {
            EnsureMcGoPayHelperExists(upgrader);
            UpgradeCartStep02(file);
            UpgradeCookies(file);
            UpgradeCartStep04(file);
            UpgradeAegisx(file, upgrader);
        }
        return file;
    }

    private static void EnsureMcGoPayHelperExists(PhpUpgraderBase upgrader)
    {
        _mcGoPayHelperPHP ??= Path.Join(upgrader.WebFolder, "classes", "McGoPayHelper.php");
        if (!File.Exists(_mcGoPayHelperPHP))
        {
            File.WriteAllText(_mcGoPayHelperPHP, File.ReadAllText(Path.Join(upgrader.BaseFolder, "important", "McGoPayHelper.php")));
        }
    }

    private static void UpgradeCartStep02(FileWrapper file)
    {
        if (file.Path.EndsWith(_cartStep02PHP, StringComparison.Ordinal) && file.Path.Contains("templates", StringComparison.Ordinal))
        {
            var insertIndex = file.Content.IndexOf("require_once('gopay/vlozeni.php');");
            if (insertIndex != -1)
            {
                file.Content.Insert(insertIndex, "(new McGoPayHelper($DOMAIN_ID))->renderPaymentMethods($UserCookiePlatba); //");
            }
        }
    }

    private static void UpgradeCookies(FileWrapper file)
    {
        file.Content.Replace("$_SESSION['UserCookiePlatbaName'] = \"GoPay\";$UserCookiePlatbaName =  $_SESSION['UserCookiePlatbaName'];",
                             "$_SESSION['UserCookiePlatbaName'] = McGoPayHelper::paymentMethodName($_POST[\"card_form_adress_platba\"]) /*\"GoPay\"*/; $UserCookiePlatbaName = $_SESSION['UserCookiePlatbaName'];");
    }

    private static void UpgradeCartStep04(FileWrapper file)
    {
        if (file.Path.EndsWith(_cartStep04PHP, StringComparison.Ordinal) && file.Path.Contains("templates", StringComparison.Ordinal))
        {
            var paymentIndex = file.Content.IndexOf("require_once('gopay/soap/payment.php');");
            if (paymentIndex == -1)
                return;

            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine();
            sb.AppendLine("    $mcgopayHelper = new McGoPayHelper($DOMAIN_ID);");
            sb.AppendLine();
            sb.AppendLine("    $isNewPayment = !$mcgopayHelper->renderPaymentStatus();");
            sb.AppendLine("    if ($isNewPayment) {");
            sb.AppendLine("      $contact = [");
            sb.AppendLine("        'first_name' => $UserCookieName,");
            sb.AppendLine("        'last_name' => $UserCookiePrijmeni,");
            sb.AppendLine("        'email' => $UserCookieEmail,");
            sb.AppendLine("        'phone_number' => $UserCookieTel,");
            sb.AppendLine("        'city' => $UserCookieCity,");
            sb.AppendLine("        'street' => $UserCookieStreet,");
            sb.AppendLine("        'postal_code' => $UserCookieZip,");
            sb.AppendLine("      ];");
            sb.AppendLine("      $paymentLink = $mcgopayHelper->createPayment($doklad_n, $platba, $id_objednavky, $contact);");
            sb.AppendLine("      $mcgopayHelper->renderPaymentButton($paymentLink);");
            sb.AppendLine("    }");
            sb.Append("/* ");
            file.Content.Insert(paymentIndex, sb.ToString());

            var fwdButtonIndex = file.Content.IndexOf("Přejít na gopay a provést platbu</a></p>", paymentIndex);
            var endIndex = file.Content.IndexOf("<?php", fwdButtonIndex);
            if (endIndex == -1)
                return;
            file.Content.Insert(endIndex + 6, "*/")
                .Replace("window.location.href=\"<?= $gopay_odkaz; ?>\";",
                         "document.getElementById('payment-invoke-checkout').click();")
                .Replace("if($_SESSION['UserCookieGopayProbehnout'] == 1) { ?>",
                         "if($_SESSION['UserCookieGopayProbehnout'] == 1 && !isset($_GET['id'])) { ?>");
        }
    }

    private static void UpgradeMcGoPay(FileWrapper file)
    {
        if (file.Path.EndsWith(_mcGoPaySetupPHP, StringComparison.Ordinal))
        {
            file.Content
                .Replace("// ===== Gate definition by DOMAIN_ID (for PHP < 7.0) =====\nclass gateway_params { static function getparams() { return [",
                         "// ===== Gate definition by DOMAIN_ID (for PHP 7+) =====\ndefine('gateway_params', [")
                .Replace("// ===== Gate definition by DOMAIN_ID (for PHP < 7.0) =====\r\nclass gateway_params { static function getparams() { return [",
                         "// ===== Gate definition by DOMAIN_ID (for PHP 7+) =====\r\ndefine('gateway_params', [")
                .Replace("];}}\n// ===== Gate definition by DOMAIN_ID (for PHP 7+) =====\n/*\ndefine('gateway_params', [",
                         "]);\n// ===== Gate definition by DOMAIN_ID (for PHP < 7.0) =====\n/*\nclass gateway_params { static function getparams() { return [")
                .Replace("];}}\r\n// ===== Gate definition by DOMAIN_ID (for PHP 7+) =====\r\n/*\r\ndefine('gateway_params', [",
                         "]);\r\n// ===== Gate definition by DOMAIN_ID (for PHP < 7.0) =====\r\n/*\r\nclass gateway_params { static function getparams() { return [")
                .Replace("];}}\n// ===== Gate definition by DOMAIN_ID (for PHP 7+) =====\ndefine('gateway_params', [",
                         "]);\n// ===== Gate definition by DOMAIN_ID (for PHP < 7.0) =====\nclass gateway_params { static function getparams() { return [")
                .Replace("];}}\r\n// ===== Gate definition by DOMAIN_ID (for PHP 7+) =====\r\ndefine('gateway_params', [",
                         "]);\r\n// ===== Gate definition by DOMAIN_ID (for PHP < 7.0) =====\r\nclass gateway_params { static function getparams() { return [")
                .Replace("]);\n*/", "];}}\n*/")
                .Replace("]);\r\n*/", "];}}\r\n*/")
                .Replace("]);\n?>", "];}}\n?>")
                .Replace("]);\r\n?>", "];}}\r\n?>");
            return;
        }
        if (file.Path.EndsWith(_mcGoPayPHP, StringComparison.Ordinal))
        {
            file.Content
                .Replace("$gateway_params = gateway_params::getparams();",
                         "if ((float)phpversion() > 7.0) $gateway_params = gateway_params; else $gateway_params = gateway_params::getparams();")
                .Replace("if ((float)phpversion() > 7.0) $gateway_params = gateway_params; else if ((float)phpversion() > 7.0) $gateway_params = gateway_params; else $gateway_params = gateway_params::getparams();",
                         "if ((float)phpversion() > 7.0) $gateway_params = gateway_params; else $gateway_params = gateway_params::getparams();");
        }
    }

    private static void UpgradeAegisx(FileWrapper file, PhpUpgraderBase upgrader)
    {
        bool? aegisxOrders = file.Path.EndsWith(_aegisxOrderDetailPHP, StringComparison.Ordinal)
            ? true
            : file.Path.EndsWith(_aegisxOrdersListPHP, StringComparison.Ordinal)
                ? false
                : null;
        if (aegisxOrders is not null)
        {
            var isAegisxOrderDetail = aegisxOrders.Value;
            var inputIndex = file.Content.IndexOf(isAegisxOrderDetail ? "Neodesílat mail s fakturou" : "Platba: <?php echo $row_data['platba_name']; ?>");
            if (inputIndex == -1)
                return;
            var insertIndex = file.Content.IndexOf(isAegisxOrderDetail ? "<?php }/* else { ?>" : "<br />", inputIndex);
            if (insertIndex == -1)
                return;

            using var sb = ZString.CreateStringBuilder();
            var spaces = isAegisxOrderDetail ? "\t\t" : "\t\t\t\t";

            sb.AppendLine(isAegisxOrderDetail ? string.Empty : "<?php");
            sb.Append(spaces);
            sb.AppendLine("require_once 'autoloader.php';");
            sb.Append(spaces);
            sb.AppendLine("$mcgopayHelper = new McGoPayHelper($row_data['domain_id']);");
            sb.AppendLine();
            sb.Append(spaces);
            sb.AppendLine("if ($mcgopayHelper->isGopay($row_data['platba_id'])) {");
            sb.Append(spaces);
            sb.Append("\tDatabase::connect('");
            sb.Append(upgrader.Hostname);
            sb.Append("', '");
            sb.Append(upgrader.Username);
            sb.Append("', '");
            sb.Append(upgrader.Password);
            sb.Append("', '");
            sb.Append(upgrader.Database);
            sb.AppendLine("', '5432');");
            sb.Append(spaces);
            sb.Append("\t$mcgopayHelper->renderPaymentStatusAdmin");
            sb.AppendLine(isAegisxOrderDetail ? "Detail($row_data['order_id'], $row_data['platba_name']);" : "List($row_data['order_id']);");
            sb.Append(spaces);
            sb.Append(isAegisxOrderDetail ? "}\n" : "} ?>");

            file.Content.Insert(insertIndex + 6, sb.ToString());
        }
    }
}
