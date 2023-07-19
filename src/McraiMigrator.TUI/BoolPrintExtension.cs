namespace McraiMigrator.TUI;

internal static class BoolPrintExtension
{
    public static string ToYesNo(this bool value)
    {
        return value ? "[green]ANO[/]" : "[red]NE[/]";
    }
}
