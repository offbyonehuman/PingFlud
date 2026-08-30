using PingFlud.Application;
using Xunit;

namespace PingFlud.Application.Tests;

public sealed class TargetListImporterTests
{
    [Fact]
    public void ImportIgnoresCommentsAndBlankLines()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-import-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "# authorized lab\n\n  10.0.0.1  \nserver.example\n");
        try
        {
            var targets = new TargetListImporter().Import(path);

            Assert.Equal("10.0.0.1" + Environment.NewLine + "server.example", targets);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ImportRejectsFilesOverFiveMegabytes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-import-{Guid.NewGuid():N}.txt");
        using (var stream = File.Create(path)) stream.SetLength(5L * 1024 * 1024 + 1);
        try
        {
            Assert.Throws<InvalidDataException>(() => new TargetListImporter().Import(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
