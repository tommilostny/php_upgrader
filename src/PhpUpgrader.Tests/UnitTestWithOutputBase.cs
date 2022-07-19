namespace PhpUpgrader.Tests;

public abstract class UnitTestWithOutputBase
{
    protected readonly ITestOutputHelper _output;

    public UnitTestWithOutputBase(ITestOutputHelper output)
    {
        _output = output;
    }
}
