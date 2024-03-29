﻿using System.Text.Json;

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

    public string Host { get; set; } = "127.0.0.1";

    public string Database { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string[] AdminFolders { get; set; } = new[] { "admin" };

    public bool RunPhpUpgrade { get; set; } = true;

    public bool DeleteRedundantFiles { get; set; } = true;

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
