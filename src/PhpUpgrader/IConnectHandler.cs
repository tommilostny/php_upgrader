namespace PhpUpgrader;

public interface IConnectHandler
{
    void UpgradeConnect(FileWrapper file, PhpUpgraderBase upgrader);
}
