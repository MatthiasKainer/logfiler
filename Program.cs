using System.Dynamic;
using System.Reflection;
using System.Threading.Channels;

namespace logfiler;

public class ProgramArgs
{
    [ArgumentParser.Argument("f", "file",
        "path to the log file, also the base name of the sqlite database if db not specified. Any existing database with the same name will be replaced.",
        false)]
    public string File { get; set; } = "";

    [ArgumentParser.Argument("d", "db",
        "path to the sqlite database file. Any existing database with the same name will be replaced.",
        true)]
    public string? DbFile { get; set; } = null;

    [ArgumentParser.Argument("p", "pattern",
        " pattern to parse the log file, for example {timestamp:date:MM/dd/yyyy} {level:graphql-level} {source} {message}",
        false)]
    public string Pattern { get; set; } = "";

    [ArgumentParser.Argument("s", "silent", "Silent mode (no step reporting)")]
    public bool Silent { get; set; } = false;

    [ArgumentParser.Argument("v", "verbose", "Far more logs")]
    public bool Verbose { get; set; } = false;

    [ArgumentParser.Argument("c", "commit-size", "Number of entries to commit to the database at once", true)]
    public int CommitSize { get; set; } = 100000;
}

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Help();
            return 1;
        }

        var programArgs = args.Parse<ProgramArgs>();
        var parser = new LogParser(programArgs.Pattern, programArgs.Verbose);
        var transform = new Transform(parser, programArgs.Verbose).TransformLogLine;
        var reader = new Reader().ProcessFile;
        var database = new Database(programArgs.DbFile ?? $"{programArgs.File}.db");
        var channel = Channel.CreateUnbounded<List<IDictionary<string, object>>>();
        var writerTask = WriteToDatabaseAsync(channel.Reader, database);

        var currentCount = 0;
        var addedLines = 0;
        var counterSteps = programArgs.CommitSize;
        var timestamp = DateTime.Now;
        List<IDictionary<string, object>> entries = new();
        await foreach (var line in reader(programArgs.File))
        {
            if (transform(line) is { } entry)
            {
                entries.Add(entry);
                addedLines++;
            }

            if (currentCount != 0 && currentCount % counterSteps == 0)
            {
                var timePerLine = (DateTime.Now - timestamp).TotalMilliseconds / counterSteps;
                if (!programArgs.Silent)
                    Console.WriteLine(
                        $"Processed {currentCount} lines, added {addedLines} rows - Time per line: {timePerLine} ms");
                await channel.Writer.WriteAsync(entries);
                entries = [];
                timestamp = DateTime.Now;
            }

            currentCount++;
        }

        await channel.Writer.WriteAsync(entries);
        channel.Writer.Complete();
        await writerTask;

        Console.WriteLine($"Processed {currentCount} lines, added {addedLines} rows");
        Console.WriteLine("Done");

        return 0;
    }

    private static async Task WriteToDatabaseAsync(ChannelReader<List<IDictionary<string, object>>> reader,
        Database database)
    {
        await foreach (var entry in reader.ReadAllAsync())
        {
            await database.StoreInDatabase(entry);
        }
    }

    private static void Help()
    {
        Console.WriteLine("""
                              __            _____ __         
                             / /___  ____ _/ __(_) /__  _____
                            / / __ \/ __ `/ /_/ / / _ \/ ___/
                           / / /_/ / /_/ / __/ / /  __/ /    
                          /_/\____/\__, /_/ /_/_/\___/_/     
                                  /____/                     
                          """);
        Console.WriteLine();
        Console.WriteLine("Reads a log file and stores it in a sqlite database");
        Console.WriteLine();
        // create usage from ProgramArgs
        var properties = typeof(ProgramArgs).GetProperties();

        // ensure the attributes are not null
        var attributes = properties
            .Select(p => p.GetCustomAttribute<ArgumentParser.ArgumentAttribute>())
            .NotNull();

        Console.WriteLine($"Usage: logfiler {string.Join(" ",
            attributes.Select(p =>
                $"{(p.Optional ? "[" : "")}--{p.LongName}|-{p.ShortName} value{(p.Optional ? "]" : "")}"
            ))}");
        Console.WriteLine();

        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<ArgumentParser.ArgumentAttribute>();
            if (attr == null)
                continue;
            Console.WriteLine(
                $" --{attr.LongName}|-{attr.ShortName}: {attr.Description} {(attr.Optional ? "(optional)" : "")}");
        }

        Console.WriteLine();
        Console.WriteLine("Pattern Format:");
        Console.WriteLine(" {name:type:format}");
        Console.WriteLine("  name: name of the group");
        Console.WriteLine("  type: type of the group (date, graphql-level, or text) (optional, default text)");
        Console.WriteLine("  format: format of the group (optional)");
        Console.WriteLine();
    }
}