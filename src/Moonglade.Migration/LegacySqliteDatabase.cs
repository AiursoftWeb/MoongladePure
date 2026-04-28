using Microsoft.Data.Sqlite;

namespace MoongladePure.Migration;

internal sealed class LegacySqliteDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Dictionary<string, HashSet<string>> _columns = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _tables;

    private LegacySqliteDatabase(SqliteConnection connection)
    {
        _connection = connection;
        _tables = LoadTables(connection).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static LegacySqliteDatabase OpenReadOnly(string sourcePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        return new LegacySqliteDatabase(connection);
    }

    public bool HasTable(string tableName) => _tables.Contains(tableName);

    public bool HasColumn(string tableName, string columnName)
    {
        if (!HasTable(tableName))
        {
            return false;
        }

        if (!_columns.TryGetValue(tableName, out var columns))
        {
            columns = LoadColumns(tableName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            _columns[tableName] = columns;
        }

        return columns.Contains(columnName);
    }

    public IReadOnlyList<LegacyRow> ReadRows(string tableName)
    {
        if (!HasTable(tableName))
        {
            return [];
        }

        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {QuoteIdentifier(tableName)};";
        using var reader = command.ExecuteReader();
        var rows = new List<LegacyRow>();

        while (reader.Read())
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(new LegacyRow(values));
        }

        return rows;
    }

    public void Dispose() => _connection.Dispose();

    private static IReadOnlyList<string> LoadTables(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";
        using var reader = command.ExecuteReader();
        var tables = new List<string>();

        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private IReadOnlyList<string> LoadColumns(string tableName)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";
        using var reader = command.ExecuteReader();
        var columns = new List<string>();

        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}

internal sealed class LegacyRow(Dictionary<string, object?> values)
{
    public bool HasValue(string columnName)
    {
        return values.TryGetValue(columnName, out var value) && value is not null && value != DBNull.Value;
    }

    public string? GetString(params string[] columnNames)
    {
        foreach (var columnName in columnNames)
        {
            if (!values.TryGetValue(columnName, out var value) || value is null || value == DBNull.Value)
            {
                continue;
            }

            return Convert.ToString(value);
        }

        return null;
    }

    public Guid? GetGuid(params string[] columnNames)
    {
        var value = GetString(columnNames);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    public int? GetInt32(params string[] columnNames)
    {
        foreach (var columnName in columnNames)
        {
            if (!values.TryGetValue(columnName, out var value) || value is null || value == DBNull.Value)
            {
                continue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (int.TryParse(Convert.ToString(value), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    public bool GetBool(bool defaultValue, params string[] columnNames)
    {
        foreach (var columnName in columnNames)
        {
            if (!values.TryGetValue(columnName, out var value) || value is null || value == DBNull.Value)
            {
                continue;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            var text = Convert.ToString(value);
            if (bool.TryParse(text, out var parsedBool))
            {
                return parsedBool;
            }

            if (int.TryParse(text, out var parsedInt))
            {
                return parsedInt != 0;
            }
        }

        return defaultValue;
    }

    public DateTime? GetDateTime(params string[] columnNames)
    {
        foreach (var columnName in columnNames)
        {
            if (!values.TryGetValue(columnName, out var value) || value is null || value == DBNull.Value)
            {
                continue;
            }

            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            if (DateTime.TryParse(Convert.ToString(value), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
