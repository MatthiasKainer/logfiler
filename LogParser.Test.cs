using System.Dynamic;
using Xunit;

namespace logfiler;

public class LogParserTests
{
    [Fact]
    public void Parse_MissingLogLine_TotallyThrows()
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
        Assert.Equal(new DateTime(2023, 10, 01), result["timestamp"]);
        Assert.Equal((int)LogLevel.Info, result["level"]);
        Assert.Equal("SourceA", result["source"]);
        Assert.Equal(5000, result["number"]);
        Assert.Equal("This is a log message", result["message"]);
    }

    [Fact]
    public void Parse_ValidLogLineWithContent_ReturnsExpandoObject()
    {
        // Arrange
        var pattern =
            "{timestamp:date:yyyy-MM-dd} {level:graphql-level} {source} job finished [table={table}, seqTxn={tx:int}, transactions={transactions:int}, rows={rows:int}, time={time}ms, {message}";

        var logLine =
            "2023-10-01 I SourceA job finished [table=orion.2znda4v1cp.av-stage1.data~104594, seqTxn=3458, transactions=1, rows=21127, time=42ms, rate=496895rows/s, physicalWrittenRowsMultiplier=81.99]\n";
        var parser = new LogParser(pattern);

        // Act
        var result = parser.Parse(logLine);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2023, 10, 01), result["timestamp"]);
        Assert.Equal((int)LogLevel.Info, result["level"]);
        Assert.Equal("SourceA", result["source"]);
        Assert.Equal("orion.2znda4v1cp.av-stage1.data~104594", result["table"]);
        Assert.Equal(3458, result["tx"]);
        Assert.Equal(1, result["transactions"]);
        Assert.Equal(21127, result["rows"]);
        Assert.Equal("rate=496895rows/s, physicalWrittenRowsMultiplier=81.99]", result["message"]);
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