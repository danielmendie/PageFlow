using PageFlow.Blazor.CLI.Commands;

namespace PageFlow.Blazor.Tests.Tests.CLI
{
    [TestFixture]
    public class GenerateRoutesTest
    {
        [Test]
        [Ignore("Only to be run on actual dev env")]
        public void GenerateRoutesCommand_WithValidFile_ShouldNotUpdateFileOnDryRun_Successfully()
        {
            var option = new CommandOptions
            {
                Command = "routes",
                Subcommand = "generate",
                Path = @"path to project",
                Force = false,
                Verbose = true,
                DryRun = true
            };

            // Arrange
            Assert.DoesNotThrowAsync(async () =>
            {
                await GenerateRoutesCommand.RunAsync(option);
            });
        }

        [Test]
        [Ignore("Only to be run on actual dev env")]
        public void GenerateRoutesCommand_WithValidFile_ShouldProcess_Successfully()
        {
            var option = new CommandOptions
            {
                Command = "routes",
                Subcommand = "generate",
                Path = @"path to project",
                Force = true,
                Verbose = true,
                DryRun = false
            };

            // Arrange
            Assert.DoesNotThrowAsync(async () =>
            {
                await GenerateRoutesCommand.RunAsync(option);
            });
        }

    }
}
