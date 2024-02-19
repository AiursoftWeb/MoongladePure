using MoongladePure.Data.Infrastructure;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace MoongladePure.Data.Exporting.Exporters;

public class JsonExporter<T>(IRepository<T> repository) : IExporter<T>
    where T : class
{
    public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct)
    {
        var data = await repository.SelectAsync(selector, ct);
        var json = JsonSerializer.Serialize(data, MoongladeJsonSerializerOptions.Default);

        return new()
        {
            ExportFormat = ExportFormat.SingleJsonFile,
            Content = Encoding.UTF8.GetBytes(json)
        };
    }
}