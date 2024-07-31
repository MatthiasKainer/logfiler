using System.Dynamic;
using System.Threading.Channels;

namespace logfiler;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Help();
            return 1;
        }
        
        var silent = args.Contains("--silent");
        args = args.Where(a => a != "--silent").ToArray();
        
        var file = args[0];
        var pattern = args[1];
        var parser = new LogParser(pattern);
        var transform = new Transform(parser).TransformLogLine;
        var reader = new Reader().ProcessFile;
        await using var database = new Database($"{file}.db");
        
        var channel = Channel.CreateUnbounded<ExpandoObject>();
        var writerTask = WriteToDatabaseAsync(channel.Reader, database);

        var currentCount = 0;
        var counterSteps = 100000;
        var timestamp = DateTime.Now;
        await foreach (var line in reader(file))
        {
            if (currentCount % counterSteps == 0)
            {
                var timePerLine = (DateTime.Now - timestamp).TotalMilliseconds / counterSteps;
                if (!silent) Console.WriteLine($"Processed {currentCount} lines - Time per line: {timePerLine} ms");
                timestamp = DateTime.Now;
            }

            if (transform(line) is { } entry)
            {
                if (currentCount == 0)
                {
                    await database.PrepareFor(entry);
                }

                await channel.Writer.WriteAsync(entry);
            }

            currentCount++;
        }

        channel.Writer.Complete();
        await writerTask;
        
        Console.WriteLine($"Processed {currentCount} lines");
        Console.WriteLine("Done");

        return 0;
    }
    
    
    private static async Task WriteToDatabaseAsync(ChannelReader<ExpandoObject> reader, Database database)
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
        Console.WriteLine("Usage: logfiler <file> <pattern> [--peek]");
        Console.WriteLine(" file: path to the log file, also the base name of the sqlite database. Any existing database with the same name will be replaced.");
        Console.WriteLine(" pattern: pattern to parse the log file, for example {timestamp:date:MM/dd/yyyy} {level:graphql-level} {source} {message}");
        Console.WriteLine();
        Console.WriteLine("Pattern Format:");
        Console.WriteLine(" {name:type:format}");
        Console.WriteLine("  name: name of the group");
        Console.WriteLine("  type: type of the group (date, graphql-level, or text) (optional, default text)");
        Console.WriteLine("  format: format of the group (optional)");
        Console.WriteLine();
    }
}