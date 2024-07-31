using System.Data.SqlClient;
using System.Data.SQLite;
using System.Dynamic;

namespace logfiler;

public class Database: IAsyncDisposable
{
    private readonly SQLiteConnection _connection;

    public Database(string filename = "somethingsomething.db", bool overwrite = true)
    {
        if (overwrite && File.Exists(filename))
        {
            File.Delete(filename);
        }
        
        var connectionString = new SQLiteConnectionStringBuilder
        {
            DataSource = filename,
            Version = 3,
            ForeignKeys = true
        }.ConnectionString;
        _connection = new SQLiteConnection(connectionString, true);
        _connection.Open();
    }
    
    private static string TypeFromValue(object? value)
    {
        return value switch
        {
            DateTime _ => "DATE",
            int _ => "INTEGER",
            _ => "TEXT"
        };
    }

    public async Task PrepareFor(ExpandoObject entry)
    {
        await using var createTable = new SQLiteCommand(_connection);
        var columnsTypes = string.Join(", ", entry.Select(kvp => $"{kvp.Key} {TypeFromValue(kvp.Value)}"));
        createTable.CommandText = $"CREATE TABLE IF NOT EXISTS log_entries ({columnsTypes})";
        createTable.ExecuteNonQuery();
    }

    public async Task StoreInDatabase(ExpandoObject entry)
    {
        await using var command = new SQLiteCommand(_connection);
        var columns = string.Join(", ", entry.Select(kvp => kvp.Key));
        var values = string.Join(", ", entry.Select(kvp => $"@{kvp.Key}"));
        command.CommandText = $"INSERT INTO log_entries ({columns}) VALUES ({values})";
        foreach (var keyValue in entry)
        {
            command.Parameters.AddWithValue($"@{keyValue.Key}", keyValue.Value);
        }

        command.ExecuteNonQuery();
    }


    public async Task<List<ExpandoObject>> Get()
    {
        await using var command = new SQLiteCommand(_connection);
        // read all rows from the log_entries table
        command.CommandText = "SELECT * FROM log_entries";
        await using var reader = command.ExecuteReader();
        var result = new List<ExpandoObject>();
        while (await reader.ReadAsync())
        {
            var row = new ExpandoObject();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                ((IDictionary<string, object>)row)[reader.GetName(i)] = reader.GetValue(i);
            }
            result.Add(row);
        }

        return result;
    }
    
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _connection.DisposeAsync();
    }
}