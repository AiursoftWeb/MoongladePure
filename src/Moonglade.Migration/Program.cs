using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MoongladePure.Migration;

internal static class Program
{
    internal static int Main(string[] args)
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

        if (options.Command == MigrationCommand.Validate)
        {
            if (string.IsNullOrWhiteSpace(options.TargetPath))
            {
                Console.Error.WriteLine("Missing required option for validate: --target <path>");
                return 1;
            }

            if (!File.Exists(options.TargetPath))
            {
                Console.Error.WriteLine($"Target database does not exist: {options.TargetPath}");
                return 2;
            }
        }
        else if (!File.Exists(options.SourcePath))
        {
            Console.Error.WriteLine($"Source database does not exist: {options.SourcePath}");
            return 2;
        }

        try
        {
            if (options.Command == MigrationCommand.Validate)
            {
                return RunValidate(options);
            }

            if (options.Command == MigrationCommand.Migrate)
            {
                if (string.IsNullOrWhiteSpace(options.TargetPath))
                {
                    Console.Error.WriteLine("Missing required option for migrate: --target <path>");
                    return 1;
                }

                var result = LegacySqliteMigrator.Migrate(options);
                LegacySqliteMigrationReportWriter.WriteText(result, Console.Out);

                if (!string.IsNullOrWhiteSpace(options.JsonPath))
                {
                    LegacySqliteMigrationReportWriter.WriteJson(result, options.JsonPath);
                    Console.Out.WriteLine();
                    Console.Out.WriteLine($"JSON report written to: {options.JsonPath}");
                }

                return result.Errors.Count == 0 ? 0 : 3;
            }

            return RunPreflight(options);
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

    private static int RunPreflight(MigrationOptions options)
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

    private static int RunValidate(MigrationOptions options)
    {
        var report = TargetSqliteValidator.Validate(options.TargetPath!);
        TargetSqliteValidationReportWriter.WriteText(report, Console.Out);

        if (!string.IsNullOrWhiteSpace(options.JsonPath))
        {
            TargetSqliteValidationReportWriter.WriteJson(report, options.JsonPath);
            Console.Out.WriteLine();
            Console.Out.WriteLine($"JSON report written to: {options.JsonPath}");
        }

        return report.Errors.Count == 0 ? 0 : 3;
    }

    private static void PrintHelp(TextWriter writer)
    {
        writer.WriteLine("MoongladePure migration tool for legacy SQLite databases.");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project src/Moonglade.Migration -- preflight --source <legacy.db> [--json <report.json>]");
        writer.WriteLine("  dotnet run --project src/Moonglade.Migration -- migrate --source <legacy.db> --target <new.db> [--overwrite] [--json <report.json>]");
        writer.WriteLine("  dotnet run --project src/Moonglade.Migration -- validate --target <new.db> [--json <report.json>]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --source <path>   Path to the legacy SQLite database.");
        writer.WriteLine("  --target <path>   Path to the new SQLite database created by migrate.");
        writer.WriteLine("  --json <path>     Optional JSON report output path.");
        writer.WriteLine("  --overwrite       Delete the target database first when it already exists.");
        writer.WriteLine("  --help            Show this help.");
    }
}

internal enum MigrationCommand
{
    Preflight = 0,
    Migrate = 1,
    Validate = 2
}

internal sealed record MigrationOptions(
    MigrationCommand Command,
    string SourcePath,
    string? TargetPath,
    string? JsonPath,
    bool Overwrite,
    bool ShowHelp)
{
    public static MigrationOptions? Parse(string[] args, TextWriter errorWriter)
    {
        if (args.Length == 0)
        {
            return null;
        }

        string? sourcePath = null;
        string? targetPath = null;
        string? jsonPath = null;
        var overwrite = false;
        var command = MigrationCommand.Preflight;
        var startIndex = 0;

        if (args[0] is "preflight" or "migrate" or "validate")
        {
            command = args[0] switch
            {
                "migrate" => MigrationCommand.Migrate,
                "validate" => MigrationCommand.Validate,
                _ => MigrationCommand.Preflight
            };
            startIndex = 1;
        }

        for (var i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "--help" or "-h")
            {
                return new MigrationOptions(command, string.Empty, null, null, false, true);
            }

            if (arg is "--source" or "-s")
            {
                if (!TryReadValue(args, ref i, arg, errorWriter, out sourcePath))
                {
                    return null;
                }

                continue;
            }

            if (arg is "--target" or "-t")
            {
                if (!TryReadValue(args, ref i, arg, errorWriter, out targetPath))
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

            if (arg == "--overwrite")
            {
                overwrite = true;
                continue;
            }

            errorWriter.WriteLine($"Unknown option: {arg}");
            return null;
        }

        if (command != MigrationCommand.Validate && string.IsNullOrWhiteSpace(sourcePath))
        {
            errorWriter.WriteLine("Missing required option: --source <path>");
            return null;
        }

        return new MigrationOptions(
            command,
            string.IsNullOrWhiteSpace(sourcePath) ? string.Empty : Path.GetFullPath(sourcePath),
            string.IsNullOrWhiteSpace(targetPath) ? null : Path.GetFullPath(targetPath),
            string.IsNullOrWhiteSpace(jsonPath) ? null : Path.GetFullPath(jsonPath),
            overwrite,
            false);
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

internal static class TargetSqliteValidator
{
    private static readonly string[] ExpectedTables =
    [
        "AiArtifact",
        "AiJob",
        "Category",
        "Comment",
        "CommentReply",
        "FriendLink",
        "MediaAsset",
        "MediaVariant",
        "Menu",
        "Page",
        "Post",
        "PostCategory",
        "PostContent",
        "PostMetric",
        "PostRoute",
        "PostTag",
        "Site",
        "SiteBinaryAsset",
        "SiteDomain",
        "SiteMembership",
        "SiteSetting",
        "SubMenu",
        "Tag",
        "Tenant",
        "Theme",
        "User"
    ];

    public static TargetSqliteValidationReport Validate(string targetPath)
    {
        using var connection = OpenReadOnlyConnection(targetPath);
        var tableSet = LoadTables(connection).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tableRows = CountExpectedRows(connection, tableSet);
        var warnings = new List<LegacyIssue>();
        var errors = new List<LegacyIssue>();

        AddMissingTableErrors(tableSet, errors);
        AddMinimumRowCountErrors(tableRows, errors);
        AddForeignKeyErrors(connection, errors);
        AddRelationshipErrors(connection, tableSet, errors);
        AddPostShapeErrors(connection, tableSet, errors);

        return new TargetSqliteValidationReport(
            targetPath,
            DateTimeOffset.UtcNow,
            tableRows,
            warnings,
            errors);
    }

    private static SqliteConnection OpenReadOnlyConnection(string targetPath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = targetPath,
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

    private static Dictionary<string, long> CountExpectedRows(SqliteConnection connection, HashSet<string> tableSet)
    {
        var rows = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var tableName in ExpectedTables)
        {
            if (tableSet.Contains(tableName))
            {
                rows[tableName] = CountRows(connection, tableName);
            }
        }

        return rows;
    }

    private static void AddMissingTableErrors(HashSet<string> tableSet, List<LegacyIssue> errors)
    {
        foreach (var tableName in ExpectedTables)
        {
            if (!tableSet.Contains(tableName))
            {
                errors.Add(new LegacyIssue("TargetTableMissing", $"Expected target table is missing: {tableName}", "Error"));
            }
        }
    }

    private static void AddMinimumRowCountErrors(Dictionary<string, long> tableRows, List<LegacyIssue> errors)
    {
        AddMinimumRowCountError(tableRows, errors, "Tenant", 1);
        AddMinimumRowCountError(tableRows, errors, "Site", 1);
        AddMinimumRowCountError(tableRows, errors, "User", 1);
        AddMinimumRowCountError(tableRows, errors, "SiteMembership", 1);
    }

    private static void AddMinimumRowCountError(Dictionary<string, long> tableRows, List<LegacyIssue> errors, string tableName, long minimum)
    {
        if (tableRows.TryGetValue(tableName, out var count) && count < minimum)
        {
            errors.Add(new LegacyIssue("TargetTableTooSmall", $"Target table {tableName} has {count} rows; expected at least {minimum}.", "Error"));
        }
    }

    private static void AddForeignKeyErrors(SqliteConnection connection, List<LegacyIssue> errors)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var table = reader.IsDBNull(0) ? "(unknown)" : reader.GetString(0);
            var rowId = reader.IsDBNull(1) ? "(unknown)" : reader.GetValue(1).ToString();
            var parent = reader.IsDBNull(2) ? "(unknown)" : reader.GetString(2);
            errors.Add(new LegacyIssue("ForeignKeyViolation", $"Foreign key violation in {table} row {rowId}; parent table: {parent}.", "Error"));
        }
    }

    private static void AddRelationshipErrors(SqliteConnection connection, HashSet<string> tableSet, List<LegacyIssue> errors)
    {
        AddOrphanError(connection, tableSet, errors, "PostContent", "PostId", "Post", "Id", "PostContentOrphanPost", "Post content points to a missing post");
        AddOrphanError(connection, tableSet, errors, "PostRoute", "PostId", "Post", "Id", "PostRouteOrphanPost", "Post route points to a missing post");
        AddOrphanError(connection, tableSet, errors, "PostMetric", "PostId", "Post", "Id", "PostMetricOrphanPost", "Post metric points to a missing post");
        AddOrphanError(connection, tableSet, errors, "PostCategory", "PostId", "Post", "Id", "PostCategoryOrphanPost", "Post category points to a missing post");
        AddOrphanError(connection, tableSet, errors, "PostCategory", "CategoryId", "Category", "Id", "PostCategoryOrphanCategory", "Post category points to a missing category");
        AddOrphanError(connection, tableSet, errors, "PostTag", "PostId", "Post", "Id", "PostTagOrphanPost", "Post tag points to a missing post");
        AddOrphanError(connection, tableSet, errors, "PostTag", "TagId", "Tag", "Id", "PostTagOrphanTag", "Post tag points to a missing tag");
        AddOrphanError(connection, tableSet, errors, "Comment", "PostId", "Post", "Id", "CommentOrphanPost", "Comment points to a missing post");
        AddOrphanError(connection, tableSet, errors, "CommentReply", "CommentId", "Comment", "Id", "CommentReplyOrphanComment", "Comment reply points to a missing comment");
        AddOrphanError(connection, tableSet, errors, "SubMenu", "MenuId", "Menu", "Id", "SubMenuOrphanMenu", "Sub menu points to a missing menu");
        AddOrphanError(connection, tableSet, errors, "SiteMembership", "UserId", "User", "Id", "SiteMembershipOrphanUser", "Site membership points to a missing user");
    }

    private static void AddPostShapeErrors(SqliteConnection connection, HashSet<string> tableSet, List<LegacyIssue> errors)
    {
        AddCountError(
            connection,
            tableSet,
            errors,
            ["Post", "PostContent"],
            "PostWithoutContent",
            "Post is missing raw content",
            "SELECT COUNT(*) FROM \"Post\" p LEFT JOIN \"PostContent\" pc ON p.\"Id\" = pc.\"PostId\" WHERE pc.\"PostId\" IS NULL;");

        AddCountError(
            connection,
            tableSet,
            errors,
            ["Post", "PostMetric"],
            "PostWithoutMetric",
            "Post is missing metrics",
            "SELECT COUNT(*) FROM \"Post\" p LEFT JOIN \"PostMetric\" pm ON p.\"Id\" = pm.\"PostId\" WHERE pm.\"PostId\" IS NULL;");

        AddCountError(
            connection,
            tableSet,
            errors,
            ["Post", "PostRoute"],
            "PublishedPostWithoutRoute",
            "Published post is missing a route",
            "SELECT COUNT(*) FROM \"Post\" p LEFT JOIN \"PostRoute\" pr ON p.\"Id\" = pr.\"PostId\" WHERE p.\"PubDateUtc\" IS NOT NULL AND pr.\"PostId\" IS NULL;");
    }

    private static void AddOrphanError(
        SqliteConnection connection,
        HashSet<string> tableSet,
        List<LegacyIssue> errors,
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
            errors.Add(new LegacyIssue(code, $"{message}: {count} rows", "Error"));
        }
    }

    private static void AddCountError(
        SqliteConnection connection,
        HashSet<string> tableSet,
        List<LegacyIssue> errors,
        string[] requiredTables,
        string code,
        string message,
        string sql)
    {
        if (requiredTables.Any(tableName => !tableSet.Contains(tableName)))
        {
            return;
        }

        var count = ExecuteScalarLong(connection, sql);
        if (count > 0)
        {
            errors.Add(new LegacyIssue(code, $"{message}: {count} rows", "Error"));
        }
    }

    private static bool HasColumns(SqliteConnection connection, HashSet<string> tableSet, string tableName, string[] columnNames)
    {
        if (!tableSet.Contains(tableName))
        {
            return false;
        }

        var columns = LoadColumns(connection, tableName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return columnNames.All(columns.Contains);
    }

    private static IReadOnlyList<string> LoadColumns(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";
        using var reader = command.ExecuteReader();
        var columns = new List<string>();

        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
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

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}

internal static class TargetSqliteValidationReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static void WriteText(TargetSqliteValidationReport report, TextWriter writer)
    {
        writer.WriteLine("MoongladePure target SQLite validation report");
        writer.WriteLine($"Target: {report.TargetPath}");
        writer.WriteLine($"Generated UTC: {report.GeneratedAtUtc:O}");
        writer.WriteLine();
        writer.WriteLine("Target rows:");

        foreach (var item in report.TableRows.OrderBy(static item => item.Key))
        {
            writer.WriteLine($"  {item.Key}: {item.Value}");
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

    public static void WriteJson(TargetSqliteValidationReport report, string jsonPath)
    {
        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonOptions));
    }
}

internal sealed record TargetSqliteValidationReport(
    string TargetPath,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyDictionary<string, long> TableRows,
    IReadOnlyList<LegacyIssue> Warnings,
    IReadOnlyList<LegacyIssue> Errors);

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
