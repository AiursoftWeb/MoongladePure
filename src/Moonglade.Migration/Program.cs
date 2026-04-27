using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MoongladePure.Migration;

internal static class Program
{
    private static int Main(string[] args)
    {
        var options = MigrationOptions.Parse(args, Console.Error);
        if (options is null)
        {
            PrintHelp(Console.Error);
            return 1;
        }

        if (options.ShowHelp)
        {
            PrintHelp(Console.Out);
            return 0;
        }

        if (!File.Exists(options.SourcePath))
        {
            Console.Error.WriteLine($"Source database does not exist: {options.SourcePath}");
            return 2;
        }

        try
        {
            var report = LegacySqliteAnalyzer.Analyze(options.SourcePath);
            LegacySqliteReportWriter.WriteText(report, Console.Out);

            if (!string.IsNullOrWhiteSpace(options.JsonPath))
            {
                LegacySqliteReportWriter.WriteJson(report, options.JsonPath);
                Console.Out.WriteLine();
                Console.Out.WriteLine($"JSON report written to: {options.JsonPath}");
            }

            return report.Errors.Count == 0 ? 0 : 3;
        }
        catch (SqliteException ex)
        {
            Console.Error.WriteLine($"SQLite read failed: {ex.Message}");
            return 4;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"File operation failed: {ex.Message}");
            return 5;
        }
    }

    private static void PrintHelp(TextWriter writer)
    {
        writer.WriteLine("MoongladePure migration preflight for legacy SQLite databases.");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project src/Moonglade.Migration -- --source <legacy.db> [--json <report.json>]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --source <path>   Path to the legacy SQLite database.");
        writer.WriteLine("  --json <path>     Optional JSON report output path.");
        writer.WriteLine("  --help            Show this help.");
    }
}

internal sealed record MigrationOptions(string SourcePath, string? JsonPath, bool ShowHelp)
{
    public static MigrationOptions? Parse(string[] args, TextWriter errorWriter)
    {
        if (args.Length == 0)
        {
            return null;
        }

        string? sourcePath = null;
        string? jsonPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "--help" or "-h")
            {
                return new MigrationOptions(string.Empty, null, true);
            }

            if (arg is "--source" or "-s")
            {
                if (!TryReadValue(args, ref i, arg, errorWriter, out sourcePath))
                {
                    return null;
                }

                continue;
            }

            if (arg == "--json")
            {
                if (!TryReadValue(args, ref i, arg, errorWriter, out jsonPath))
                {
                    return null;
                }

                continue;
            }

            errorWriter.WriteLine($"Unknown option: {arg}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            errorWriter.WriteLine("Missing required option: --source <path>");
            return null;
        }

        return new MigrationOptions(Path.GetFullPath(sourcePath), string.IsNullOrWhiteSpace(jsonPath) ? null : Path.GetFullPath(jsonPath), false);
    }

    private static bool TryReadValue(string[] args, ref int index, string optionName, TextWriter errorWriter, out string? value)
    {
        value = null;
        if (index + 1 >= args.Length)
        {
            errorWriter.WriteLine($"Missing value for option: {optionName}");
            return false;
        }

        var candidate = args[index + 1];
        if (candidate.StartsWith('-'))
        {
            errorWriter.WriteLine($"Missing value for option: {optionName}");
            return false;
        }

        value = candidate;
        index++;
        return true;
    }
}

internal static class LegacySqliteAnalyzer
{
    private static readonly string[] KnownTables =
    [
        "BlogAsset",
        "BlogConfiguration",
        "BlogTheme",
        "Category",
        "Comment",
        "CommentReply",
        "CustomPage",
        "FriendLink",
        "LocalAccount",
        "Menu",
        "Post",
        "PostCategory",
        "PostExtension",
        "PostTag",
        "SubMenu",
        "Tag"
    ];

    private static readonly string[] RequiredSettingKeys =
    [
        "GeneralSettings",
        "ContentSettings",
        "FeedSettings",
        "ImageSettings",
        "AdvancedSettings",
        "CustomStyleSheetSettings"
    ];

    public static LegacySqliteReport Analyze(string sourcePath)
    {
        using var connection = OpenReadOnlyConnection(sourcePath);
        var tables = LoadTables(connection);
        var tableSet = tables.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var knownTableReports = BuildKnownTableReports(connection, tableSet);
        var warnings = new List<LegacyIssue>();
        var errors = new List<LegacyIssue>();

        warnings.AddRange(DetectMissingKnownTables(tableSet));
        warnings.AddRange(DetectDuplicateValues(connection, tableSet));
        warnings.AddRange(DetectRequiredValueIssues(connection, tableSet));
        warnings.AddRange(DetectRelationshipIssues(connection, tableSet));
        warnings.AddRange(DetectMissingSettings(connection, tableSet));

        return new LegacySqliteReport(
            sourcePath,
            DateTimeOffset.UtcNow,
            DetectLatestMigration(connection, tableSet),
            tables,
            knownTableReports,
            warnings,
            errors);
    }

    private static SqliteConnection OpenReadOnlyConnection(string sourcePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        return connection;
    }

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

    private static IReadOnlyList<LegacyTableReport> BuildKnownTableReports(SqliteConnection connection, HashSet<string> tableSet)
    {
        var reports = new List<LegacyTableReport>();

        foreach (var tableName in KnownTables)
        {
            if (!tableSet.Contains(tableName))
            {
                reports.Add(new LegacyTableReport(tableName, false, null, []));
                continue;
            }

            reports.Add(new LegacyTableReport(
                tableName,
                true,
                CountRows(connection, tableName),
                LoadColumns(connection, tableName)));
        }

        return reports;
    }

    private static IReadOnlyList<LegacyIssue> DetectMissingKnownTables(HashSet<string> tableSet)
    {
        var warnings = new List<LegacyIssue>();

        foreach (var tableName in KnownTables)
        {
            if (!tableSet.Contains(tableName))
            {
                warnings.Add(new LegacyIssue("MissingTable", $"Expected legacy table is missing: {tableName}", "Warning"));
            }
        }

        return warnings;
    }

    private static IReadOnlyList<LegacyIssue> DetectDuplicateValues(SqliteConnection connection, HashSet<string> tableSet)
    {
        var warnings = new List<LegacyIssue>();

        AddDuplicateValueWarnings(connection, tableSet, warnings, "Category", ["RouteName"], "CategoryRouteNameDuplicate", "Category route name has duplicates");
        AddDuplicateValueWarnings(connection, tableSet, warnings, "Tag", ["NormalizedName"], "TagNormalizedNameDuplicate", "Tag normalized name has duplicates");
        AddDuplicateValueWarnings(connection, tableSet, warnings, "CustomPage", ["Slug"], "CustomPageSlugDuplicate", "Custom page slug has duplicates");
        AddDuplicateValueWarnings(connection, tableSet, warnings, "Post", ["PubDateUtc", "Slug"], "PostRouteDuplicate", "Published post route has duplicates", "IsPublished = 1 AND IsDeleted = 0");

        return warnings;
    }

    private static IReadOnlyList<LegacyIssue> DetectRequiredValueIssues(SqliteConnection connection, HashSet<string> tableSet)
    {
        var warnings = new List<LegacyIssue>();

        AddRequiredValueWarning(connection, tableSet, warnings, "Category", "RouteName", "CategoryRouteNameEmpty", "Category route name is empty");
        AddRequiredValueWarning(connection, tableSet, warnings, "Category", "DisplayName", "CategoryDisplayNameEmpty", "Category display name is empty");
        AddRequiredValueWarning(connection, tableSet, warnings, "Tag", "NormalizedName", "TagNormalizedNameEmpty", "Tag normalized name is empty");
        AddRequiredValueWarning(connection, tableSet, warnings, "Tag", "DisplayName", "TagDisplayNameEmpty", "Tag display name is empty");
        AddRequiredValueWarning(connection, tableSet, warnings, "Post", "Slug", "PostSlugEmpty", "Post slug is empty");
        AddRequiredValueWarning(connection, tableSet, warnings, "Post", "Title", "PostTitleEmpty", "Post title is empty");
        AddRequiredValueWarning(connection, tableSet, warnings, "CustomPage", "Slug", "CustomPageSlugEmpty", "Custom page slug is empty");
        AddRequiredValueWarning(connection, tableSet, warnings, "LocalAccount", "NormalizedUsername", "LocalAccountNormalizedUsernameEmpty", "Local account normalized username is empty");

        return warnings;
    }

    private static IReadOnlyList<LegacyIssue> DetectRelationshipIssues(SqliteConnection connection, HashSet<string> tableSet)
    {
        var warnings = new List<LegacyIssue>();

        AddOrphanWarning(connection, tableSet, warnings, "PostCategory", "PostId", "Post", "Id", "PostCategoryOrphanPost", "Post category points to a missing post");
        AddOrphanWarning(connection, tableSet, warnings, "PostCategory", "CategoryId", "Category", "Id", "PostCategoryOrphanCategory", "Post category points to a missing category");
        AddOrphanWarning(connection, tableSet, warnings, "PostTag", "PostId", "Post", "Id", "PostTagOrphanPost", "Post tag points to a missing post");
        AddOrphanWarning(connection, tableSet, warnings, "PostTag", "TagId", "Tag", "Id", "PostTagOrphanTag", "Post tag points to a missing tag");
        AddOrphanWarning(connection, tableSet, warnings, "PostExtension", "PostId", "Post", "Id", "PostExtensionOrphanPost", "Post extension points to a missing post");
        AddOrphanWarning(connection, tableSet, warnings, "Comment", "PostId", "Post", "Id", "CommentOrphanPost", "Comment points to a missing post");
        AddOrphanWarning(connection, tableSet, warnings, "CommentReply", "CommentId", "Comment", "Id", "CommentReplyOrphanComment", "Comment reply points to a missing comment");

        return warnings;
    }

    private static IReadOnlyList<LegacyIssue> DetectMissingSettings(SqliteConnection connection, HashSet<string> tableSet)
    {
        var warnings = new List<LegacyIssue>();
        if (!HasColumns(connection, tableSet, "BlogConfiguration", ["CfgKey"]))
        {
            return warnings;
        }

        foreach (var settingKey in RequiredSettingKeys)
        {
            if (!HasSetting(connection, settingKey))
            {
                warnings.Add(new LegacyIssue("SettingMissing", $"Required setting is missing: {settingKey}", "Warning"));
            }
        }

        return warnings;
    }

    private static string? DetectLatestMigration(SqliteConnection connection, HashSet<string> tableSet)
    {
        if (!HasColumns(connection, tableSet, "__EFMigrationsHistory", ["MigrationId"]))
        {
            return null;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM \"__EFMigrationsHistory\" ORDER BY MigrationId DESC LIMIT 1;";
        return command.ExecuteScalar() as string;
    }

    private static void AddDuplicateValueWarnings(
        SqliteConnection connection,
        HashSet<string> tableSet,
        List<LegacyIssue> warnings,
        string tableName,
        string[] columnNames,
        string code,
        string message,
        string? filter = null)
    {
        if (!HasColumns(connection, tableSet, tableName, columnNames))
        {
            return;
        }

        var keyExpression = string.Join(" || ' | ' || ", columnNames.Select(static columnName => $"IFNULL(TRIM(CAST({QuoteIdentifier(columnName)} AS TEXT)), '')"));
        var groupColumns = string.Join(", ", columnNames.Select(QuoteIdentifier));
        var whereParts = columnNames
            .Select(static columnName => $"IFNULL(TRIM(CAST({QuoteIdentifier(columnName)} AS TEXT)), '') <> ''")
            .ToList();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            whereParts.Add(filter);
        }

        var whereClause = string.Join(" AND ", whereParts);
        var sql = $"SELECT {keyExpression} AS DuplicateKey, COUNT(*) AS DuplicateCount FROM {QuoteIdentifier(tableName)} WHERE {whereClause} GROUP BY {groupColumns} HAVING COUNT(*) > 1;";

        foreach (var duplicate in QueryKeyCounts(connection, sql))
        {
            warnings.Add(new LegacyIssue(code, $"{message}: {duplicate.Key} ({duplicate.Count} rows)", "Warning"));
        }
    }

    private static void AddRequiredValueWarning(
        SqliteConnection connection,
        HashSet<string> tableSet,
        List<LegacyIssue> warnings,
        string tableName,
        string columnName,
        string code,
        string message)
    {
        if (!HasColumns(connection, tableSet, tableName, [columnName]))
        {
            return;
        }

        var sql = $"SELECT COUNT(*) FROM {QuoteIdentifier(tableName)} WHERE {QuoteIdentifier(columnName)} IS NULL OR TRIM(CAST({QuoteIdentifier(columnName)} AS TEXT)) = '';";
        var count = ExecuteScalarLong(connection, sql);
        if (count > 0)
        {
            warnings.Add(new LegacyIssue(code, $"{message}: {count} rows", "Warning"));
        }
    }

    private static void AddOrphanWarning(
        SqliteConnection connection,
        HashSet<string> tableSet,
        List<LegacyIssue> warnings,
        string childTable,
        string childColumn,
        string parentTable,
        string parentColumn,
        string code,
        string message)
    {
        if (!HasColumns(connection, tableSet, childTable, [childColumn]) || !HasColumns(connection, tableSet, parentTable, [parentColumn]))
        {
            return;
        }

        var sql =
            $"SELECT COUNT(*) FROM {QuoteIdentifier(childTable)} c " +
            $"LEFT JOIN {QuoteIdentifier(parentTable)} p ON c.{QuoteIdentifier(childColumn)} = p.{QuoteIdentifier(parentColumn)} " +
            $"WHERE c.{QuoteIdentifier(childColumn)} IS NOT NULL AND p.{QuoteIdentifier(parentColumn)} IS NULL;";
        var count = ExecuteScalarLong(connection, sql);
        if (count > 0)
        {
            warnings.Add(new LegacyIssue(code, $"{message}: {count} rows", "Warning"));
        }
    }

    private static bool HasSetting(SqliteConnection connection, string settingKey)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"BlogConfiguration\" WHERE \"CfgKey\" = $settingKey;";
        command.Parameters.AddWithValue("$settingKey", settingKey);
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    private static bool HasColumns(SqliteConnection connection, HashSet<string> tableSet, string tableName, string[] columnNames)
    {
        if (!tableSet.Contains(tableName))
        {
            return false;
        }

        var columns = LoadColumns(connection, tableName)
            .Select(static column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return columnNames.All(columns.Contains);
    }

    private static IReadOnlyList<LegacyColumnReport> LoadColumns(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";
        using var reader = command.ExecuteReader();
        var columns = new List<LegacyColumnReport>();

        while (reader.Read())
        {
            columns.Add(new LegacyColumnReport(
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3) == 1,
                reader.GetInt32(5) > 0));
        }

        return columns;
    }

    private static long CountRows(SqliteConnection connection, string tableName)
    {
        return ExecuteScalarLong(connection, $"SELECT COUNT(*) FROM {QuoteIdentifier(tableName)};");
    }

    private static long ExecuteScalarLong(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static IReadOnlyList<KeyCount> QueryKeyCounts(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var rows = new List<KeyCount>();

        while (reader.Read())
        {
            rows.Add(new KeyCount(reader.GetString(0), reader.GetInt64(1)));
        }

        return rows;
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    private sealed record KeyCount(string Key, long Count);
}

internal static class LegacySqliteReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static void WriteText(LegacySqliteReport report, TextWriter writer)
    {
        writer.WriteLine("MoongladePure legacy SQLite preflight report");
        writer.WriteLine($"Source: {report.SourcePath}");
        writer.WriteLine($"Generated UTC: {report.GeneratedAtUtc:O}");
        writer.WriteLine($"Latest EF migration: {report.LatestMigrationId ?? "(not found)"}");
        writer.WriteLine();
        writer.WriteLine("Known legacy tables:");

        foreach (var table in report.KnownTables)
        {
            var rowCount = table.RowCount.HasValue ? table.RowCount.Value.ToString() : "missing";
            writer.WriteLine($"  {table.Name}: {rowCount}");
        }

        writer.WriteLine();
        writer.WriteLine($"Warnings: {report.Warnings.Count}");

        foreach (var warning in report.Warnings)
        {
            writer.WriteLine($"  [{warning.Code}] {warning.Message}");
        }

        writer.WriteLine();
        writer.WriteLine($"Errors: {report.Errors.Count}");

        foreach (var error in report.Errors)
        {
            writer.WriteLine($"  [{error.Code}] {error.Message}");
        }
    }

    public static void WriteJson(LegacySqliteReport report, string jsonPath)
    {
        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonOptions));
    }
}

internal sealed record LegacySqliteReport(
    string SourcePath,
    DateTimeOffset GeneratedAtUtc,
    string? LatestMigrationId,
    IReadOnlyList<string> AllTables,
    IReadOnlyList<LegacyTableReport> KnownTables,
    IReadOnlyList<LegacyIssue> Warnings,
    IReadOnlyList<LegacyIssue> Errors);

internal sealed record LegacyTableReport(
    string Name,
    bool Exists,
    long? RowCount,
    IReadOnlyList<LegacyColumnReport> Columns);

internal sealed record LegacyColumnReport(
    string Name,
    string Type,
    bool NotNull,
    bool PrimaryKey);

internal sealed record LegacyIssue(
    string Code,
    string Message,
    string Severity);
