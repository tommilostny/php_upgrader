namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static partial class GoPay
{
    private static readonly string _gopayHelperPHP = Path.Join("gopay", "api", "gopay_helper.php");
    private static readonly string _gopaySoapPHP = Path.Join("gopay", "api", "gopay_soap.php");
    private static readonly string _mcGoPaySetupPHP = Path.Join("classes", "McGoPay.setup.php");
    private static readonly string _mcGoPayPHP = Path.Join("classes", "McGoPay.php");

    public static FileWrapper UpgradeGoPay(this FileWrapper file)
    {
        UpgradeGopaySOAPHelper(file);
        UpgradeMcGoPay(file);
        return file;
    }

    private static void UpgradeGopaySOAPHelper(FileWrapper file)
    {
        if (file.Path.EndsWith(_gopayHelperPHP, StringComparison.Ordinal))
        {
            file.Content.Replace("Předpokladem je PHP verze 5.1.2 a vyšší s modulem mcrypt.",
                                 "Předpokladem je PHP verze 7.4. a vyšší s modulem OpenSSL.");
            var encryptIndex = file.Content.IndexOf("public static function encrypt($data, $secret) {") + 48;
            file.Content.Insert(encryptIndex, "/*");
            file.Content.Insert(file.Content.IndexOf('}', encryptIndex),
                "    */\n        $key = substr($secret, 0, 24); // 3DES requires a 24-byte key\n\n        // Pad the input data to a multiple of the block size\n        $block_size = 8; // 3DES uses 8-byte blocks\n        $padding = $block_size - (strlen($data) % $block_size);\n        $data .= str_repeat(chr($padding), $padding);\n\n        $encrypted_data = openssl_encrypt($data, 'des-ede3', $key, OPENSSL_RAW_DATA);\n\n        return bin2hex($encrypted_data);\n    ");

            var decryptIndex = file.Content.IndexOf("public static function decrypt($data, $secret) {") + 48;
            file.Content.Insert(decryptIndex, "/*");
            file.Content.Insert(file.Content.IndexOf('}', decryptIndex),
                "    */\n        $key = substr($secret, 0, 24);\n        $binary_data = hex2bin($data);\n\n        $decrypted_data = openssl_decrypt($binary_data, 'des-ede3', $key, OPENSSL_RAW_DATA);\n\n        // Remove padding\n        $padding = ord($decrypted_data[strlen($decrypted_data) - 1]);\n        $decrypted_data = substr($decrypted_data, 0, -$padding);\n\n        return trim($decrypted_data);\n    ");
        }
        else if (file.Path.EndsWith(_gopaySoapPHP, StringComparison.Ordinal))
        {
            file.Content.Replace(SoapCallRegex().Replace(file.Content.ToString(), _SoapCallEvaluator));
        }

        static string _SoapCallEvaluator(Match match)
        {
            using var sb = ZString.CreateStringBuilder();
            sb.Append("$go_client->__soapCall('");
            sb.Append(match.Groups["method"].Value);

            var args = match.Groups["args"].Value;
            sb.Append(args.StartsWith("array()", StringComparison.OrdinalIgnoreCase)
                ? $"', {args});"
                : $"', array({args}));");

            return sb.ToString();
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
                .Replace("]);\n*/", "];}}\n*/")
                .Replace("]);\r\n*/", "];}}\r\n*/");
            return;
        }
        if (file.Path.EndsWith(_mcGoPayPHP, StringComparison.Ordinal))
        {
            file.Content.Replace("$gateway_params = gateway_params::getparams();",
                                 "if ((float)phpversion() > 7.0) $gateway_params = gateway_params; else $gateway_params = gateway_params::getparams();");
        }
    }

    [GeneratedRegex(@"\$go_client->__call\s?\(\s?(?<q>['""])(?<method>\w+?)\k<q>,\s*(?<args>.+?)\);", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 66666)]
    private static partial Regex SoapCallRegex();
}
