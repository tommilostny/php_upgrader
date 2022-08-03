namespace PhpUpgrader;

public abstract class ConnectHandler
{
    public abstract void UpgradeConnect(FileWrapper file, PhpUpgraderBase upgrader);
}
