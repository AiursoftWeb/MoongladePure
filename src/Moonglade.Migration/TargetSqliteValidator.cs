using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MoongladePure.Migration;

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
        return Validate(targetPath, null);
    }

    public static TargetSqliteValidationReport Validate(string targetPath, string? sourcePath)
    {
        using var connection = OpenReadOnlyConnection(targetPath);
        var tableSet = LoadTables(connection).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tableRows = CountExpectedRows(connection, tableSet);
        var sourceRows = string.IsNullOrWhiteSpace(sourcePath) ? null : CountLegacyRows(sourcePath);
        var comparisons = BuildSourceTargetComparisons(sourceRows, tableRows);
        var warnings = new List<LegacyIssue>();
        var errors = new List<LegacyIssue>();

        AddMissingTableErrors(tableSet, errors);
        AddMinimumRowCountErrors(tableRows, errors);
        AddForeignKeyErrors(connection, errors);
        AddRelationshipErrors(connection, tableSet, errors);
        AddPostShapeErrors(connection, tableSet, errors);
        AddLegacyPostRouteErrors(connection, tableSet, sourcePath, errors);
        AddSiteSettingJsonErrors(connection, tableSet, errors);
        AddSourceTargetCountErrors(comparisons, errors);

        return new TargetSqliteValidationReport(
            sourcePath,
            targetPath,
            DateTimeOffset.UtcNow,
            sourceRows ?? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase),
            tableRows,
            comparisons,
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

    private static Dictionary<string, long> CountLegacyRows(string sourcePath)
    {
        using var connection = OpenReadOnlyConnection(sourcePath);
        var tableSet = LoadTables(connection).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var tableName in new[]
                 {
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
                 })
        {
            if (tableSet.Contains(tableName))
            {
                rows[tableName] = CountRows(connection, tableName);
            }
        }

        if (tableSet.Contains("Post") && HasColumns(connection, tableSet, "Post", ["PubDateUtc"]))
        {
            rows["PublishedPostWithRoute"] = ExecuteScalarLong(connection, "SELECT COUNT(*) FROM \"Post\" WHERE \"PubDateUtc\" IS NOT NULL;");
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

    private static void AddLegacyPostRouteErrors(SqliteConnection targetConnection, HashSet<string> targetTableSet, string? sourcePath, List<LegacyIssue> errors)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !HasColumns(targetConnection, targetTableSet, "PostRoute", ["RouteDate", "Slug"]))
        {
            return;
        }

        using var sourceConnection = OpenReadOnlyConnection(sourcePath);
        var sourceTableSet = LoadTables(sourceConnection).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!HasColumns(sourceConnection, sourceTableSet, "Post", ["PubDateUtc", "Slug"]))
        {
            return;
        }

        var targetRoutes = LoadRoutes(targetConnection, "PostRoute", "RouteDate", "Slug");
        foreach (var legacyRoute in LoadRoutes(sourceConnection, "Post", "PubDateUtc", "Slug"))
        {
            if (!targetRoutes.Contains(legacyRoute))
            {
                errors.Add(new LegacyIssue(
                    "LegacyPostRouteMissing",
                    $"Legacy post route is missing in target: {legacyRoute.RouteDate}/{legacyRoute.Slug}",
                    "Error"));
            }
        }
    }

    private static void AddSiteSettingJsonErrors(SqliteConnection connection, HashSet<string> tableSet, List<LegacyIssue> errors)
    {
        if (!HasColumns(connection, tableSet, "SiteSetting", ["CfgKey", "CfgValue"]))
        {
            return;
        }

        errors.AddRange(LegacySqliteAnalyzer.DetectInvalidJsonRows(
            connection,
            "SiteSetting",
            "CfgKey",
            "CfgValue",
            "SiteSettingJsonInvalid",
            "Site setting JSON is invalid",
            "Error"));
    }

    private static HashSet<PostRouteKey> LoadRoutes(SqliteConnection connection, string tableName, string routeDateColumn, string slugColumn)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {QuoteIdentifier(routeDateColumn)}, {QuoteIdentifier(slugColumn)} " +
            $"FROM {QuoteIdentifier(tableName)} " +
            $"WHERE {QuoteIdentifier(routeDateColumn)} IS NOT NULL " +
            $"AND {QuoteIdentifier(slugColumn)} IS NOT NULL " +
            $"AND TRIM(CAST({QuoteIdentifier(slugColumn)} AS TEXT)) <> '';";
        using var reader = command.ExecuteReader();
        var routes = new HashSet<PostRouteKey>();

        while (reader.Read())
        {
            var routeDateText = Convert.ToString(reader.GetValue(0));
            var slug = Convert.ToString(reader.GetValue(1));
            if (TryNormalizeDate(routeDateText, out var routeDate) && !string.IsNullOrWhiteSpace(slug))
            {
                routes.Add(new PostRouteKey(routeDate, slug));
            }
        }

        return routes;
    }

    private static bool TryNormalizeDate(string? value, out string routeDate)
    {
        routeDate = string.Empty;
        if (!DateTime.TryParse(value, out var parsed))
        {
            return false;
        }

        routeDate = parsed.Date.ToString("yyyy-MM-dd");
        return true;
    }

    private static IReadOnlyList<TargetRowComparison> BuildSourceTargetComparisons(Dictionary<string, long>? sourceRows, Dictionary<string, long> targetRows)
    {
        if (sourceRows is null)
        {
            return [];
        }

        var comparisons = new List<TargetRowComparison>();
        AddComparison(sourceRows, targetRows, comparisons, "LocalAccount", "User");
        AddComparison(sourceRows, targetRows, comparisons, "Category", "Category");
        AddComparison(sourceRows, targetRows, comparisons, "Tag", "Tag");
        AddComparison(sourceRows, targetRows, comparisons, "BlogConfiguration", "SiteSetting");
        AddComparison(sourceRows, targetRows, comparisons, "BlogTheme", "Theme");
        AddComparison(sourceRows, targetRows, comparisons, "BlogAsset", "SiteBinaryAsset");
        AddComparison(sourceRows, targetRows, comparisons, "FriendLink", "FriendLink");
        AddComparison(sourceRows, targetRows, comparisons, "Menu", "Menu");
        AddComparison(sourceRows, targetRows, comparisons, "SubMenu", "SubMenu");
        AddComparison(sourceRows, targetRows, comparisons, "CustomPage", "Page");
        AddComparison(sourceRows, targetRows, comparisons, "Post", "Post");
        AddComparison(sourceRows, targetRows, comparisons, "Post", "PostContent");
        AddComparison(sourceRows, targetRows, comparisons, "PublishedPostWithRoute", "PostRoute");
        AddComparison(sourceRows, targetRows, comparisons, "PostExtension", "PostMetric");
        AddComparison(sourceRows, targetRows, comparisons, "PostCategory", "PostCategory");
        AddComparison(sourceRows, targetRows, comparisons, "PostTag", "PostTag");
        AddComparison(sourceRows, targetRows, comparisons, "Comment", "Comment");
        AddComparison(sourceRows, targetRows, comparisons, "CommentReply", "CommentReply");
        return comparisons;
    }

    private static void AddComparison(
        Dictionary<string, long> sourceRows,
        Dictionary<string, long> targetRows,
        List<TargetRowComparison> comparisons,
        string sourceName,
        string targetName)
    {
        if (!sourceRows.TryGetValue(sourceName, out var sourceCount) || !targetRows.TryGetValue(targetName, out var targetCount))
        {
            return;
        }

        comparisons.Add(new TargetRowComparison(sourceName, targetName, sourceCount, targetCount, sourceCount == targetCount));
    }

    private static void AddSourceTargetCountErrors(IReadOnlyList<TargetRowComparison> comparisons, List<LegacyIssue> errors)
    {
        foreach (var comparison in comparisons.Where(static comparison => !comparison.Matches))
        {
            errors.Add(new LegacyIssue(
                "SourceTargetCountMismatch",
                $"Source {comparison.SourceName} has {comparison.SourceCount} rows, but target {comparison.TargetName} has {comparison.TargetCount} rows.",
                "Error"));
        }
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
        if (!string.IsNullOrWhiteSpace(report.SourcePath))
        {
            writer.WriteLine($"Source: {report.SourcePath}");
        }

        writer.WriteLine($"Target: {report.TargetPath}");
        writer.WriteLine($"Generated UTC: {report.GeneratedAtUtc:O}");

        if (report.SourceRows.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Source rows:");

            foreach (var item in report.SourceRows.OrderBy(static item => item.Key))
            {
                writer.WriteLine($"  {item.Key}: {item.Value}");
            }
        }

        if (report.SourceTargetComparisons.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Source-target comparisons:");

            foreach (var comparison in report.SourceTargetComparisons)
            {
                var status = comparison.Matches ? "OK" : "Mismatch";
                writer.WriteLine($"  {comparison.SourceName} -> {comparison.TargetName}: {comparison.SourceCount} -> {comparison.TargetCount} ({status})");
            }
        }

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
    string? SourcePath,
    string TargetPath,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyDictionary<string, long> SourceRows,
    IReadOnlyDictionary<string, long> TableRows,
    IReadOnlyList<TargetRowComparison> SourceTargetComparisons,
    IReadOnlyList<LegacyIssue> Warnings,
    IReadOnlyList<LegacyIssue> Errors);

internal sealed record TargetRowComparison(
    string SourceName,
    string TargetName,
    long SourceCount,
    long TargetCount,
    bool Matches);

internal sealed record PostRouteKey(string RouteDate, string Slug);
