﻿using McraiMigrator.TUI;
using Spectre.Console;

var config = UpgraderConfig.Load();
bool? runPhpUpgrader = null;

AnsiConsole.Clear();
AnsiConsole.Write(new FigletText("McRAI PHP Migration Tool").Centered().Color(Color.GreenYellow));
AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule($"(c) Tomáš Milostný, {DateTime.Now.Year}").Centered());
do
{
    var option = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .HighlightStyle(Color.Orange1)
            .PageSize(7)
            .MoreChoicesText("[grey](Další možnosti...)[/]")
            .AddChoices(new[] {
                "[green]Spustit[/]",
                $"Název webu: [yellow]{config.WebName}[/]",
                $"Synchonizovat FTP: {config.SyncFtp.ToYesNo()}",
                $"Nahrát upravené soubory na FTP: {config.UploadFtp.ToYesNo()}",
                $"Rubicon: {config.Rubicon.ToYesNo()}",
                $"Maximální velikost souboru pro FTP synchronizaci: [white]{config.MaxFileSizeMB} MB[/]",
                $"Host: [white]{config.Host}[/]",
                $"Výchozí složka: [white]{config.BaseFolder}[/]",
                $"Spustit PHP upgrade: {config.RunPhpUpgrade.ToYesNo()}",
                $"Použít zálohu: {config.UseBackup.ToYesNo()}",
                "[blue]Reset hodnot[/]",
                "[red]Ukončit[/]",
            }));

    // Act on selected option.
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
    if (option.StartsWith("[blue]Reset hodnot[/]", StringComparison.Ordinal))
    {
        config = new();
        config.Save();
        continue;
    }
    runPhpUpgrader = false;
}
while (runPhpUpgrader is null);

config.Save();

if (runPhpUpgrader is false)
{
    AnsiConsole.Clear();
    return;
}
Console.WriteLine();
await PhpUpgrader.Program.Main
(
    webName: new[] { config.WebName },
    rubicon: config.Rubicon,
    checkFtp: config.SyncFtp,
    ignoreFtp: !config.SyncFtp,
    upload: config.UploadFtp,
    dontUpload: !config.UploadFtp,
    useBackup: config.UseBackup,
    ftpMaxMb: config.MaxFileSizeMB
);
