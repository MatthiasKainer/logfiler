using System.Data.SqlClient;
using System.Data.SQLite;
using System.Dynamic;

namespace logfiler;

public class Database: IAsyncDisposable
{
    private readonly SQLiteConnection _connection;
    private SQLiteCommand _command;
    private Dictionary<string, SQLiteParameter> _parameters = new();

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
            ForeignKeys = true,
            JournalMode = SQLiteJournalModeEnum.Off,
            SyncMode = SynchronizationModes.Normal,
        };
        connectionString.Add("cache", "shared"); 
        connectionString.Add("journal_size_limit", "6144000"); 
        _connection = new SQLiteConnection(connectionString.ConnectionString, true);
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
        
        await createTable.ExecuteNonQueryAsync();
        
        _command = new SQLiteCommand(_connection);
        var columns = string.Join(", ", entry.Select(kvp => kvp.Key));
        var values = string.Join(", ", entry.Select(kvp => $"@{kvp.Key}"));
        _command.CommandText = $"INSERT INTO log_entries ({columns}) VALUES ({values})";

        _parameters = new Dictionary<string, SQLiteParameter>();
        foreach (var keyValue in entry)
        {   
            var param = _command.CreateParameter();
            param.ParameterName = $"@{keyValue.Key}";
            _command.Parameters.Add(param);
            _parameters.Add(keyValue.Key, param);
        }
    }

    public async Task StoreInDatabase(List<ExpandoObject> entry)
    {
        await using var transaction = _connection.BeginTransaction();
        foreach (var obj in entry)
        {
            foreach (var keyValue in obj)
            {
                _parameters[keyValue.Key].Value = keyValue.Value;
            }
            _command.ExecuteNonQuery();
        }
        
        await transaction.CommitAsync();
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