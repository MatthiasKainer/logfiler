using System.Reflection;

namespace logfiler;

public static class ArgumentParser
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgumentAttribute(string shortName, string longName, string description, bool optional = true) : Attribute
    {
        public string ShortName { get; } = shortName;
        public string LongName { get; } = longName;
        public string Description { get; } = description;
        public bool Optional { get; } = optional;
    }
    
    public static T Parse<T>(this string[] args)
    {
        var type = typeof(T);
        var properties = type.GetProperties();
        var instance = Activator.CreateInstance<T>();

        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<ArgumentAttribute>();
            if (attr == null)
                continue;

            var shortName = $"-{attr.ShortName}";
            var longName = $"--{attr.LongName}";

            var shortIndex = Array.IndexOf(args, shortName);
            var longIndex = Array.IndexOf(args, longName);

            if (shortIndex == -1 && longIndex == -1)
            {
                if (!attr.Optional)
                    throw new MissingFieldException($"{attr.LongName} missing in arguments.");
                continue;
            }

            var index = shortIndex != -1 ? shortIndex : longIndex;
            var value = args[index + 1];

            prop.SetValue(instance, Convert.ChangeType(value, prop.PropertyType));
        }

        return instance;
    }
}