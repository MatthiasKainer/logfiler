﻿using System.Dynamic;
using System.Globalization;

namespace logfiler;

public enum LogLevel
{
    Info,
    Warn,
    Error,
    Unknown
}

public class Transform(ILogParser parser, bool verbose = false)
{
    public IDictionary<string, object>? TransformLogLine(string? logLine)
    {
        if (logLine == null)
            return null;

        if (parser.Parse(logLine) is { } result) return result;
        if (verbose) Console.WriteLine("Skipping invalid log entry: " + logLine);
        return null;
    }
}