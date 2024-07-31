namespace logfiler;

using System.IO;

public class Reader
{
    public async IAsyncEnumerable<string?> ProcessFile(string filePath)
    {
        using var reader = new StreamReader(filePath);
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null)
            {
                break;
            }
            yield return line;
        }
    }
}