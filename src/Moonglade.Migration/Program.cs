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

            if (!string.IsNullOrWhiteSpace(options.SourcePath) && !File.Exists(options.SourcePath))
            {
                Console.Error.WriteLine($"Source database does not exist: {options.SourcePath}");
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
        var report = string.IsNullOrWhiteSpace(options.SourcePath)
            ? TargetSqliteValidator.Validate(options.TargetPath!)
            : TargetSqliteValidator.Validate(options.TargetPath!, options.SourcePath);
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
        writer.WriteLine("  dotnet run --project src/Moonglade.Migration -- validate --target <new.db> [--source <legacy.db>] [--json <report.json>]");
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
