using System.Dynamic;
using Xunit;

namespace logfiler.Tests;

public class LogParserTests
{
    [Fact] public void Parse_MissingLogLine_TotallyThrows()
    {
        var pattern = "{timestamp:date:yyyy-MM-dd} {level:graphql-level} {source} {message}";
        var parser = new LogParser(pattern);
        Assert.Throws<ArgumentNullException>(() => parser.Parse(null!));
    }

    [Fact]
    public void Parse_ValidLogLine_ReturnsExpandoObject()
    {
        // Arrange
        var pattern = "{timestamp:date:yyyy-MM-dd} {level:graphql-level} {source} {number:int} {message}";
        var logLine = "2023-10-01 I SourceA 5000 This is a log message";
        var parser = new LogParser(pattern);

        // Act
        var result = parser.Parse(logLine);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2023, 10, 01), ((dynamic)result).timestamp);
        Assert.Equal((int)LogLevel.Info, ((dynamic)result).level);
        Assert.Equal("SourceA", ((dynamic)result).source);
        Assert.Equal(5000, ((dynamic)result).number);
        Assert.Equal("This is a log message", ((dynamic)result).message);
    }

    [Fact]
    public void Parse_InvalidLogLine_ReturnsNull()
    {
        // Arrange
        var pattern = "{timestamp:date:yyyy-MM-dd} {level:graphql-level} {source} {message}";
        var logLine = "Invalid log line";
        var parser = new LogParser(pattern);

        // Act
        var result = parser.Parse(logLine);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_LogLineWithMissingGroups_ReturnsNull()
    {
        // Arrange
        var pattern = "{timestamp:date:yyyy-MM-dd} {level:graphql-level} {source} {number:int} {message}";
        var logLine = "2023-10-01 I This is a log message";
        var parser = new LogParser(pattern);

        // Act
        var result = parser.Parse(logLine);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void Parse_LogLineWithWeirdParsing_ReturnsNull()
    {
        // Arrange
        var pattern = "{timestamp:date:yyyy-MM-dd} {number:int} {message}";
        var logLine = "2023-10-01 1g3 I This is a log message";
        var parser = new LogParser(pattern);

        // Act
        var result = parser.Parse(logLine);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void Parse_LogLineWithWeirdDateParsing_ReturnsNull()
    {
        // Arrange
        var pattern = "{timestamp:date:yyyy-MM-dd}";
        var logLine = "This";
        var parser = new LogParser(pattern);

        // Act
        var result = parser.Parse(logLine);

        // Assert
        Assert.Null(result);
    }
}