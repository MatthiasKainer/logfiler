
using Xunit;
using Xunit.Abstractions;

namespace logfiler;

public class ProgramTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Main_NoArgs_ShowsHelp()
    {
        // Redirect console output
        var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);
        
        // Act
        var exitCode = await Program.Main([]);

        // Assert
        Assert.Equal(1, exitCode);
        var output = consoleOutput.ToString();
        Assert.Contains("Reads a log file and stores it in a sqlite database", output);
    }

    [Fact]
    public async Task Main_ValidArgs_ProcessesLogFile()
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        var pattern = "{timestamp:date:yyyy-MM-ddThh:mm:ss.ffffffZ} {level:graphql-level} {source} {message}";
        var logContent = """
                         2024-07-24T11:21:27.169996Z I i.q.g.e.QueryProgress fin [id=1245, sql=`SELECT instance_name, instance_rgb, current_user`, principal=admin, cache=true, time=99001]
                         2024-07-24T11:21:27.170000Z I i.q.c.h.p.JsonQueryProcessorState [48] timings [compiler: 0, count: 0, execute: 126701, q=`SELECT instance_name, instance_rgb, current_user`]
                         2024-07-24T11:21:27.170020Z I i.q.c.h.p.JsonQueryProcessor all sent [fd=48, lastRequestBytesSent=596, nCompletedRequests=18, totalBytesSent=92374570]
                         2024-07-24T11:21:27.171601Z I i.q.g.e.QueryProgress exe [id=1246, sql=`(show parameters) where property_path ilike 'cairo.sql.copy.root'`, principal=admin, cache=true]
                         2024-07-24T11:21:27.171707Z I i.q.g.e.QueryProgress fin [id=1246, sql=`(show parameters) where property_path ilike 'cairo.sql.copy.root'`, principal=admin, cache=true, time=107801]
                         2024-07-24T11:21:27.171709Z I i.q.c.h.p.JsonQueryProcessorState [157826] timings [compiler: 0, count: 0, execute: 117801, q=`(show parameters) where property_path ilike 'cairo.sql.copy.root'`]
                         2024-07-24T11:21:27.171720Z U i.q.c.h.p.JsonQueryProcessor all sent [fd=157826, lastRequestBytesSent=776, nCompletedRequests=16, totalBytesSent=110036398]
                         2024-07-24T11:21:27.171935Z W i.q.g.e.QueryProgress exe [id=1247, sql=`tables();`, principal=admin, cache=true]
                         2024-07-24T11:21:27.172152Z E i.q.c.h.p.StaticContentProcessor [157825] incoming [url=/assets/vs/loader.js]
                         """;
        await File.WriteAllTextAsync(logFilePath, logContent);
        var args = new[] { logFilePath, pattern };

        // Redirect console output
        var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = await Program.Main(args);

        // Asser
        Assert.Equal(0, exitCode);
        var output = consoleOutput.ToString();
        Assert.Contains("Processed 9 lines", output);
        Assert.Contains("Done", output);
        
        // Verify database file was created
        var dbPath = $"{logFilePath}.db";
        var size = new FileInfo($"{dbPath}").Length;
        testOutputHelper.WriteLine($"Reading database {dbPath} with size {size}");
        Assert.True(File.Exists($"{dbPath}"));
        // and it has the expected size bigger 0 bytes
        Assert.True(size > 0);
        
        // Verify database content
        await using var db = new Database(dbPath, false);
        var entries = await db.Get();
        Assert.Equal(9, entries.Count);

        dynamic firstEntry = entries[0];
        Assert.Equal(new DateTime(2024, 7, 24, 13, 21, 27, 169, 996), firstEntry.timestamp);
        Assert.Equal((int)LogLevel.Info, firstEntry.level);
        Assert.Equal("i.q.g.e.QueryProgress", firstEntry.source);
        Assert.Equal("fin [id=1245, sql=`SELECT instance_name, instance_rgb, current_user`, principal=admin, cache=true, time=99001]", firstEntry.message);

        dynamic lastEntry = entries[^1];
        Assert.Equal(new DateTime(2024, 7, 24, 13, 21, 27, 172, 152), lastEntry.timestamp);
        Assert.Equal((int)LogLevel.Error, lastEntry.level);
        Assert.Equal("i.q.c.h.p.StaticContentProcessor", lastEntry.source);
        Assert.Equal("[157825] incoming [url=/assets/vs/loader.js]", lastEntry.message);
    }
}