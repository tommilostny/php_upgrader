using System.Text.Json;

namespace McraiMigrator.TUI;

internal struct UpgraderConfig
{
    public const string ConfigFileName = "McraiMigrator.config.json";

    public UpgraderConfig()
    {
    }

    public string WebName { get; set; } = string.Empty;

    public bool SyncFtp { get; set; } = true;

    public bool UploadFtp { get; set; } = true;

    public bool Rubicon { get; set; } = true;

    public bool UseBackup { get; set; } = true;

    public double MaxFileSizeMB { get; set; } = 500;

    public string BaseFolder { get; set; } = "/McRAI";

    public string Host { get; set; } = "localhost";

    public bool RunPhpUpgrade { get; set; } = true;

    public static UpgraderConfig Load()
    {
        if (File.Exists(ConfigFileName))
        {
            return JsonSerializer.Deserialize<UpgraderConfig>(File.ReadAllText(ConfigFileName))!;
        }
        return new UpgraderConfig();
    }

    public readonly void Save()
    {
        File.WriteAllText(ConfigFileName, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
