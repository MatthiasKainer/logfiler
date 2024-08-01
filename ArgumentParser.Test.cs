
using System;
using Xunit;
namespace logfiler;

public class ArgumentParserTests
{
    
    class TestArgs
    {
        [ArgumentParser.Argument("f", "file", "The logfile to parse", false)]
        public string File { get; set; } = "";

        [ArgumentParser.Argument("p", "pattern", "The pattern to parse the logfile", false)]
        public string Pattern { get; set; } = "";

        [ArgumentParser.Argument("s", "silent", "Silent mode (no step reporting)")]
        public bool Silent { get; set; } = false;

        [ArgumentParser.Argument("c", "commit-size", "Number of entries to commit to the database at once", true)]
        public int CommitSize { get; set; } = 10000;
    }

    [Fact]
    public void Parse_ValidArguments_ReturnsCorrectValues()
    {
        // Arrange
        var args = new[] { "--file", "log.txt", "--pattern", "{timestamp} {level} {message}", "--silent", "true" };

        // Act
        var result = args.Parse<TestArgs>();

        // Assert
        Assert.Equal("log.txt", result.File);
        Assert.Equal("{timestamp} {level} {message}", result.Pattern);
        Assert.True(result.Silent);
    }

    [Fact]
    public void Parse_MissingOptionalArguments_ReturnsDefaultValues()
    {
        // Arrange
        var args = new[] { "--file", "log.txt", "-p", " " };

        // Act
        var result = args.Parse<TestArgs>();

        // Assert
        Assert.Equal("log.txt", result.File);
        Assert.False(result.Silent);
        Assert.Equal(10000, result.CommitSize);
    }

    [Fact]
    public void Parse_InvalidArgumentNames_IgnoresInvalidArguments()
    {
        // Arrange
        var args = new[] { "--invalid", "value", "--file", "log.txt", "-p", "[]" };

        // Act
        var result = args.Parse<TestArgs>();

        // Assert
        Assert.Equal("log.txt", result.File);
        Assert.False(result.Silent);
    }

    [Fact]
    public void Parse_MixedShortAndLongArgumentNames_ReturnsCorrectValues()
    {
        // Arrange
        var args = new[] { "-f", "log.txt", "--pattern", "{timestamp} {level} {message}", "-s", "true" };

        // Act
        var result = args.Parse<TestArgs>();

        // Assert
        Assert.Equal("log.txt", result.File);
        Assert.Equal("{timestamp} {level} {message}", result.Pattern);
        Assert.True(result.Silent);
    }

    [Fact]
    public void Parse_MissingRequiredArgument_Fails()
    {
        var args = new[] { "-s", "false" };
        
        Assert.Throws<MissingFieldException>(() => args.Parse<TestArgs>());
    }
}