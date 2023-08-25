namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class PHPMailer
{
    private static string? _exceptionPhp = null;

    public static FileWrapper UpgradePHPMailer(this FileWrapper file, PhpUpgraderBase upgrader)
    {
        if (file.Content.Contains("include_once \"mail/PHPMailer.php\";"))
        {
            _exceptionPhp ??= Path.Join(upgrader.WebFolder, "mail", "Exception.php");
            if (!File.Exists(_exceptionPhp))
            {
                File.Copy(Path.Join(upgrader.BaseFolder, "important", "Exception.txt"), _exceptionPhp);
            }
            file.Content.Insert(file.Content.IndexOf("<?php") + 6, "include_once \"mail/Exception.php\";");
            UpdateMailerConfig(file, upgrader);
        }
        return file;
    }

    private static void UpdateMailerConfig(FileWrapper file, PhpUpgraderBase upgrader)
    {
        _InsertHost(_IndexFunc);
        _InsertHost(_LastIndexFunc);
        file.Content
            .Replace("//$mail->SMTPSecure = \"ssl\";", "$mail->SMTPSecure = \"\";")
            .Replace("$mail->SMTPSecure = \"tls\";", "$mail->SMTPSecure = \"\";")
            .Replace("$mail->Port       = 587;", "$mail->Port       = 465;")
            .Replace("$mail->Port       = 465;", "$mail->Port       = 25;");
        //file.Content
        //    .Replace("//$mail->SMTPSecure = \"ssl\";", "$mail->SMTPSecure = \"ssl\";")
        //    .Replace("$mail->SMTPSecure = \"tls\";", "$mail->SMTPSecure = \"ssl\";")
        //    .Replace("$mail->Port       = 587;", "$mail->Port       = 465;");
        
        var mailPassword = File.ReadAllText(Path.Join(upgrader.BaseFolder, "mail_password.txt")).AsSpan().Trim();
        if (file.Content.Contains("$mail->Username   = \"obchodni-podminky@vestavne-spotrebice.cz\""))
        {
            _InsertPassword(_IndexFunc, mailPassword);
            _InsertPassword(_LastIndexFunc, mailPassword);
        }

        static int _IndexFunc(StringBuilder sb, string s) => sb.IndexOf(s);
        static int _LastIndexFunc(StringBuilder sb, string s) => sb.LastIndexOf(s);

        void _InsertPassword(Func<StringBuilder, string, int> indexFunc, ReadOnlySpan<char> password)
        {
            var index = indexFunc(file.Content, "$mail->Password   =");
            if (index != -1 && !(file.Content[index - 1] == '/' && file.Content[index - 2] == '/'))
            {
                file.Content.Insert(index, $"$mail->Password   = \"{password}\"; //");
            }
        }

        void _InsertHost(Func<StringBuilder, string, int> indexFunc)
        {
            var hostCodeIndex = indexFunc(file.Content, "$mail->Host       =");
            if (hostCodeIndex != -1)
            {
                //file.Content.Insert(hostCodeIndex, $"$mail->Host       = \"mcrai-upgrade.vshosting.cz\"; //");
                file.Content.Insert(hostCodeIndex, $"$mail->Host       = \"mcrai.vshosting.cz\"; //");
            }
        }
    }
}
