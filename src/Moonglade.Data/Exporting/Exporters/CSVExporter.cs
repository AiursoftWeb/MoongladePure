using CsvHelper;
using MoongladePure.Data.Infrastructure;
using System.Globalization;
using System.Linq.Expressions;

namespace MoongladePure.Data.Exporting.Exporters;

public class CSVExporter<T>(IRepository<T> repository, string fileNamePrefix, string directory)
    : IExporter<T>
    where T : class
{
    public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct)
    {
        var data = await repository.SelectAsync(selector, ct);
        var result = await ToCSVResult(data, ct);
        return result;
    }

    private async Task<ExportResult> ToCSVResult<TResult>(IEnumerable<TResult> data, CancellationToken ct)
    {
        var tempId = Guid.NewGuid().ToString();
        string exportDirectory = ExportManager.CreateExportDirectory(directory, tempId);

        var distPath = Path.Join(exportDirectory, $"{fileNamePrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv");

        await using var writer = new StreamWriter(distPath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(data, ct);

        return new()
        {
            ExportFormat = ExportFormat.SingleCSVFile,
            FilePath = distPath
        };
    }
}