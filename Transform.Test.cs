namespace logfiler;

using System.Dynamic;
using Moq;
using Xunit;

public class TransformTests
{
    [Fact]
    public void TransformLogLine_ValidLogLine_ReturnsExpandoObject()
    {
        // Arrange
        var mockParser = new Mock<ILogParser>();
        var logLine = "2023-10-01 I SourceA This is a log message";
        dynamic expectedResult = new ExpandoObject();
        mockParser.Setup(p => p.Parse(logLine)).Returns(expectedResult);
        var transform = new Transform(mockParser.Object);

        // Act
        var result = transform.TransformLogLine(logLine);

        // Assert
        Assert.NotNull(result);
        Assert.Same(expectedResult, result);
    }

    [Fact]
    public void TransformLogLine_NullLogLine_ReturnsNull()
    {
        // Arrange
        var mockParser = new Mock<ILogParser>();
        var transform = new Transform(mockParser.Object);

        // Act
        var result = transform.TransformLogLine(null);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void TransformLogLine_ParserFails_ReturnsNull()
    {
        // Arrange
        var mockParser = new Mock<ILogParser>();
        var logLine = "2023-10-01 I SourceA This is a log message";
        var transform = new Transform(mockParser.Object);
        mockParser.Setup(p => p.Parse(logLine)).Returns((ExpandoObject?)null);

        // Act
        var result = transform.TransformLogLine(logLine);

        // Assert
        Assert.Null(result);
    }

}