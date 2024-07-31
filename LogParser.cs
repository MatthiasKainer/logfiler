using System.Dynamic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace logfiler;

public interface ILogParser
{
    ExpandoObject? Parse(string logLine);
}

public partial class LogParser: ILogParser
{
    private readonly Regex _regex;
    private readonly Dictionary<string, Func<string, object>> _groups;

    public LogParser(string pattern)
    {
        // Convert pattern to regex and extract group names
        (_regex, _groups) = CreateRegexFromPattern(pattern);
    }

    public ExpandoObject? Parse(string logLine)
    {
        var match = _regex.Match(logLine);
        if (!match.Success) return null;

        dynamic dynamicObject = new ExpandoObject();
        foreach (var group in _groups)
        {
            ((IDictionary<string, object>)dynamicObject)[group.Key] = group.Value(match.Groups[group.Key].Value);
        }
        
        foreach (var group in _groups)
        {
            if (((IDictionary<string, object?>)dynamicObject)[group.Key] != null) return dynamicObject;
        }

        return null;
    }

    private static (Regex, Dictionary<string, Func<string, object>>) CreateRegexFromPattern(string pattern)
    {
        var groups = new Dictionary<string, Func<string, object>>();
        var regexPattern = PregeneratedRegex().Replace(pattern, match =>
        {
            var name = match.Groups[1].Value;
            var type = match.Groups.Count >= 2 ? match.Groups[2].Value : null;
            var format = match.Groups.Count >= 3 ? match.Groups[3].Value : null;

            switch (type)
            {
                case "date":
                    groups.Add(name, s => DateTime.TryParseExact(s,
                        string.IsNullOrEmpty(format) ? "yyyy-MM-ddTHH:mm:ss.ffffffZ" : format,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime result)
                        ? result
                        : null!);
                    return $@"(?<{name}>.+?)";
                case "graphql-level":
                    groups.Add(name, level => ((int)ToLogLevel(level)));
                    return $@"(?<{name}>.+?)";
                case "int":
                    groups.Add(name, s => int.Parse(s));
                    return $@"(?<{name}>\d+)";
                default:
                    groups.Add(name, s => s);
                    return ($@"(?<{name}>.+?)");
            }
        });

        return (new Regex($"^{regexPattern}$"), groups);
    }
    
    private static LogLevel ToLogLevel(string level)
    {
        return level.ToUpperInvariant() switch
        {
            "I" => LogLevel.Info,
            "W" => LogLevel.Warn,
            "E" => LogLevel.Error,
            _ => LogLevel.Unknown
        };
    }

    [GeneratedRegex(@"\{([\w\.\-_]+)(?::([\w\.\-_]+))?(?::([^}]+))?\}")]
    private static partial Regex PregeneratedRegex();
}