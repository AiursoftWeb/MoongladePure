using System.Text.Json;

namespace MoongladePure.Migration;

internal static class LegacySqliteDryRun
{
    public static LegacySqliteDryRunResult Run(MigrationOptions options)
    {
        var directory = Path.Combine(Path.GetTempPath(), "moonglade-migration-dry-run", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var targetPath = Path.Combine(directory, "target.db");
            var migrateOptions = options with
            {
                TargetPath = targetPath,
                Overwrite = false,
                DryRun = false
            };

            var migration = LegacySqliteMigrator.Migrate(migrateOptions);
            var validation = TargetSqliteValidator.Validate(targetPath, options.SourcePath);
            var errors = migration.Errors.Concat(validation.Errors).ToArray();

            return new LegacySqliteDryRunResult(
                options.SourcePath,
                targetPath,
                DateTimeOffset.UtcNow,
                migration,
                validation,
                errors);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}

internal sealed record LegacySqliteDryRunResult(
    string SourcePath,
    string TemporaryTargetPath,
    DateTimeOffset GeneratedAtUtc,
    LegacySqliteMigrationResult Migration,
    TargetSqliteValidationReport Validation,
    IReadOnlyList<LegacyIssue> Errors);

internal static class LegacySqliteDryRunReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static void WriteText(LegacySqliteDryRunResult result, TextWriter writer)
    {
        writer.WriteLine("MoongladePure legacy SQLite dry-run report");
        writer.WriteLine($"Source: {result.SourcePath}");
        writer.WriteLine($"Temporary target: {result.TemporaryTargetPath}");
        writer.WriteLine($"Generated UTC: {result.GeneratedAtUtc:O}");
        writer.WriteLine();
        writer.WriteLine("Migration simulation:");
        LegacySqliteMigrationReportWriter.WriteText(result.Migration, writer);
        writer.WriteLine();
        writer.WriteLine("Validation:");
        TargetSqliteValidationReportWriter.WriteText(result.Validation, writer);
        writer.WriteLine();
        writer.WriteLine($"Dry-run errors: {result.Errors.Count}");
    }

    public static void WriteJson(LegacySqliteDryRunResult result, string jsonPath)
    {
        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(result, JsonOptions));
    }
}
