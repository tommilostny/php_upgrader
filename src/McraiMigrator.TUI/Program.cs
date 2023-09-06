#pragma warning disable MA0076 // Year is just a number

using McraiMigrator.TUI;
using Spectre.Console;

var config = UpgraderConfig.Load();
bool? runPhpUpgrader = null;

AnsiConsole.Clear();
AnsiConsole.Write(new FigletText("McRAI PHP Migration Tool").Centered().Color(Color.GreenYellow));
AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule($"(c) Tomáš Milostný, {DateTime.Now.Year}").Centered());

while (runPhpUpgrader is null)
{
    var option = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .HighlightStyle(Color.Orange1)
            .PageSize(8)
            .MoreChoicesText("[grey](Další možnosti...)[/]")
            .AddChoices(new[] {
                "[green]Spustit[/]",
                $"Název webu: [yellow]{config.WebName}[/]",
                $"Synchonizovat FTP: {config.SyncFtp.ToYesNo()}",
                $"Nahrát upravené soubory na FTP: {config.UploadFtp.ToYesNo()}",
                $"Rubicon: {config.Rubicon.ToYesNo()}",
                $"Maximální velikost souboru pro FTP synchronizaci: [white]{config.MaxFileSizeMB} MB[/]",
                $"Mazat redundantní soubory: {config.DeleteRedundantFiles.ToYesNo()}",
                $"Host: [white]{config.Host}[/]",
                $"Databáze: [white]{config.Database}[/]",
                $"Uživatel: [white]{config.UserName}[/]",
                $"Heslo: [white]{config.Password}[/]",
                $"Výchozí složka: [white]{config.BaseFolder}[/]",
                $"Spustit PHP upgrade: {config.RunPhpUpgrade.ToYesNo()}",
                $"Použít zálohu: {config.UseBackup.ToYesNo()}",
                "[blue]Reset údajů k databázi[/]",
                "[blue]Reset všech hodnot[/]",
                $"Admin složky (Mona): [white]{string.Join(", ", config.AdminFolders)}[/]",
                "[red]Ukončit[/]",
            }));
    if (option.StartsWith("[green]Spustit[/]", StringComparison.Ordinal))
    {
        runPhpUpgrader = true;
        break;
    }
    if (option.StartsWith("Název webu: ", StringComparison.Ordinal))
    {
        config.WebName = AnsiConsole.Ask<string>("Zadejte název webu: ");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        continue;
    }
    if (option.StartsWith("Synchonizovat FTP:", StringComparison.Ordinal))
    {
        config.SyncFtp = !config.SyncFtp;
        continue;
    }
    if (option.StartsWith("Nahrát upravené soubory na FTP:", StringComparison.Ordinal))
    {
        config.UploadFtp = !config.UploadFtp;
        continue;
    }
    if (option.StartsWith("Rubicon:", StringComparison.Ordinal))
    {
        config.Rubicon = !config.Rubicon;
        continue;
    }
    if (option.StartsWith("Maximální velikost souboru pro FTP synchronizaci:", StringComparison.Ordinal))
    {
        Console.Write("Zadejte maximální velikost souboru pro FTP synchronizaci [");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("MB");
        Console.ResetColor();
        Console.WriteLine(']');
        config.MaxFileSizeMB = AnsiConsole.Ask<double>("(0 a menší => přenést všechny soubory nehledě na velikost): ");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.Write("                                                                   ");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.Write("                                                                   \r");
        continue;
    }
    if (option.StartsWith("Mazat redundantní soubory:", StringComparison.Ordinal))
    {
        config.DeleteRedundantFiles = !config.DeleteRedundantFiles;
        continue;
    }
    if (option.StartsWith("Výchozí složka:", StringComparison.Ordinal))
    {
        config.BaseFolder = AnsiConsole.Ask<string>("Zadejte výchozí složku: ");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        continue;
    }
    if (option.StartsWith("Host:", StringComparison.Ordinal))
    {
        config.Host = AnsiConsole.Ask<string>("Zadejte hosta: ");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        continue;
    }
    if (option.StartsWith("Databáze:", StringComparison.Ordinal))
    {
        config.Database = AnsiConsole.Ask<string>("Zadejte databázi: ");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        continue;
    }
    if (option.StartsWith("Uživatel:", StringComparison.Ordinal))
    {
        config.UserName = AnsiConsole.Ask<string>("Zadejte uživatele: ");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        continue;
    }
    if (option.StartsWith("Heslo:", StringComparison.Ordinal))
    {
        config.Password = AnsiConsole.Ask<string>("Zadejte heslo: ");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        continue;
    }
    if (option.StartsWith("Spustit PHP upgrade:", StringComparison.Ordinal))
    {
        config.RunPhpUpgrade = !config.RunPhpUpgrade;
        continue;
    }
    if (option.StartsWith("Použít zálohu:", StringComparison.Ordinal))
    {
        config.UseBackup = !config.UseBackup;
        continue;
    }
    if (option.StartsWith("[blue]Reset všech hodnot[/]", StringComparison.Ordinal))
    {
        config = new();
        config.Save();
        continue;
    }
    if (option.StartsWith("[blue]Reset údajů k databázi[/]", StringComparison.Ordinal))
    {
        config.Host = "127.0.0.1";
        config.Database = config.UserName = config.Password = string.Empty;
        config.Save();
        continue;
    }
    if (option.StartsWith("Admin složky (Mona):", StringComparison.Ordinal))
    {
        config.AdminFolders = AnsiConsole.Ask<string>("Zadejte admin složky (oddělené čárkou): ").Split(',').Select(x => x.Trim()).ToArray();
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        continue;
    }
    runPhpUpgrader = false;
}
config.Save();

if (runPhpUpgrader is false)
{
    AnsiConsole.Clear();
    return;
}
AnsiConsole.WriteLine();
await PhpUpgrader.Program.Main
(
    webName: config.WebName,
    baseFolder: config.BaseFolder,
    rubicon: config.Rubicon,
    checkFtp: config.SyncFtp,
    ignoreFtp: !config.SyncFtp,
    upload: config.UploadFtp,
    dontUpload: !config.UploadFtp,
    useBackup: config.UseBackup,
    ftpMaxMb: config.MaxFileSizeMB,
    deleteRedundant: config.DeleteRedundantFiles,
    host: HostnameToKnownIP(config.Host),
    db: string.IsNullOrWhiteSpace(config.Database) ? null : config.Database,
    user: string.IsNullOrWhiteSpace(config.UserName) ? null : config.UserName,
    password: string.IsNullOrWhiteSpace(config.Password) ? null : config.Password,
    dontUpgrade: !config.RunPhpUpgrade,
    adminFolders: config.AdminFolders
)
.ConfigureAwait(false);

static string HostnameToKnownIP(string hostname) => hostname.StartsWith("mcrai2", StringComparison.Ordinal)
    ? "217.16.184.116"
    : hostname.StartsWith("local", StringComparison.Ordinal)
        ? "127.0.0.1"
        : hostname;
