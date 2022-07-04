namespace PhpUpgrader.Tests.Fixtures;

/// <summary>
/// Třída pro testování MonaUpgraderu.
/// </summary>
internal sealed class MonaUpgraderFixture : MonaUpgrader
{
    /// <summary>
    /// Prázdné vlastnosti BaseFolder, WebName, FindWhat, ReplaceWith.
    /// </summary>
    internal MonaUpgraderFixture() : base(null, null)
    {
    }

    /// <summary>
    /// Konstruktor shodný s původním MonaUpgraderem.
    /// </summary>
    internal MonaUpgraderFixture(string baseFolder, string webName) : base(baseFolder, webName)
    { 
    }
}
