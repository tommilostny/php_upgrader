using Xunit;
using Xunit.Abstractions;

namespace PhpUpgrader.Tests
{
    public class RenameBetaUnitTest
    {
        private readonly ITestOutputHelper _output;

        public RenameBetaUnitTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NotPresetShouldRenameBetaVariable()
        {
            //Arrange
            var content = "mysqli_query($beta, $query);";
            var upgrader = new MonaUpgrader();

            //Act
            upgrader.RenameBeta(ref content, "gama");

            //Assert
            var expected = "mysqli_query($gama, $query);";
            _output.WriteLine(expected);
            _output.WriteLine(content);
            Assert.Equal(expected, content);
        }

        [Fact]
        public void PresetShouldRenameBetaVariable()
        {
            //Arrange
            var content = "mysqli_query($beta, $query);";
            var upgrader = new MonaUpgrader { RenameBetaWith = "gama" };

            //Act
            upgrader.RenameBeta(ref content);

            //Assert
            var expected = "mysqli_query($gama, $query);";
            _output.WriteLine(expected);
            _output.WriteLine(content);
            Assert.Equal(expected, content);
        }

        [Fact]
        public void UnsetShouldNotRename()
        {
            //Arrange
            var content = "mysqli_query($beta, $query);";
            var upgrader = new MonaUpgrader();

            //Act
            upgrader.RenameBeta(ref content);

            //Assert
            Assert.Equal("mysqli_query($beta, $query);", content);
        }

        [Fact]
        public void PresetShouldBeOverridenByParameter()
        {
            //Arrange
            var content = "mysqli_query($beta, $query);";
            var upgrader = new MonaUpgrader { RenameBetaWith = "gama" };

            //Act
            upgrader.RenameBeta(ref content, "alfa");

            //Assert
            var expected = "mysqli_query($alfa, $query);";
            _output.WriteLine(expected);
            _output.WriteLine(content);
            Assert.Equal(expected, content);
        }
    }
}
