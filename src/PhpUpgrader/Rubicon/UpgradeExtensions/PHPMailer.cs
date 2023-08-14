namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class PHPMailer
{
    private static string? _createOrderSendMailPath;

    public static FileWrapper UpgradePHPMailer(this FileWrapper file, PhpUpgraderBase upgrader)
    {
        _createOrderSendMailPath ??= Path.Join("rubicon", "modules", "card", "create_order_send_mail.php");

        if (file.Path.EndsWith(_createOrderSendMailPath, StringComparison.Ordinal))
        {
            file.Content.Insert(file.Content.IndexOf("<?php") + 6, "include_once \"mail/Exception.php\";");
            if (file.Content.Contains("new PHPMailer"))
            {
                UpdateMailerConfig(file, upgrader);
            }
        }
        return file;
    }

    private static void UpdateMailerConfig(FileWrapper file, PhpUpgraderBase upgrader)
    {
        _InsertHost(_IndexFunc);
        _InsertHost(_LastIndexFunc);
        file.Content
            .Replace("//$mail->SMTPSecure = \"ssl\";", "$mail->SMTPSecure = \"ssl\";")
            .Replace("$mail->SMTPSecure = \"tls\";", "$mail->SMTPSecure = \"ssl\";")
            .Replace("$mail->Port       = 587;", "$mail->Port       = 465;");
        
        var mailPasswordLine = File.ReadAllLines(Path.Join(upgrader.BaseFolder, "mail_passwords.txt"))
                        .FirstOrDefault(line => line.StartsWith(upgrader.WebName, StringComparison.Ordinal))
                        ?.Split(':');
        if (mailPasswordLine is not null and not { Length: < 2 })
        {
            var mailPassword = mailPasswordLine[1].AsSpan().Trim();
            _InsertPassword(_IndexFunc, mailPassword);
            _InsertPassword(_LastIndexFunc, mailPassword);
            return;
        }
        file.Warnings.Add("Obsahuje PHPMailer, ale není možné automaticky doplnit heslo k mailu.");

        int _IndexFunc(StringBuilder sb, string s) => sb.IndexOf(s);
        int _LastIndexFunc(StringBuilder sb, string s) => sb.LastIndexOf(s);

        void _InsertPassword(Func<StringBuilder, string, int> indexFunc, ReadOnlySpan<char> password)
        {
            var mailPasswordCodeIndex = indexFunc(file.Content, "$mail->Password   =");
            if (mailPasswordCodeIndex != -1)
            {
                file.Content.Insert(mailPasswordCodeIndex, $"$mail->Password   = \"{password}\"; //");
            }
        }

        void _InsertHost(Func<StringBuilder, string, int> indexFunc)
        {
            var hostCodeIndex = indexFunc(file.Content, "$mail->Host       =");
            if (hostCodeIndex != -1)
            {
                file.Content.Insert(hostCodeIndex, $"$mail->Host       = \"mcrai-upgrade.vshosting.cz\"; //");
            }
        }
    }
}
