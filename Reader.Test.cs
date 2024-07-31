using Xunit;

namespace logfiler;

public class ReaderTest
{
    [Fact]
    public async Task ProcessFile_ValidFile_ReturnsLines()
    {
        // Arrange
        var reader = new Reader();
        // Create a temporary file
        var filePath = Path.Combine(Path.GetTempFileName());
        await File.WriteAllTextAsync(filePath, "This is a log line");

        // Act
        var lines = reader.ProcessFile(filePath);

        // Assert
        var linesRead = 0;
        await foreach (var line in lines)
        {
            Assert.NotNull(line);
            linesRead++;
        }
        Assert.Equal(1, linesRead);
    }

}