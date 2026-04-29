using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Infrastructure;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace MoongladePure.Data.Exporting.Exporters;

public class JsonExporter<T>(IRepository<T> repository) : IExporter<T>
    where T : class
{
    public async Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct, Expression<Func<T, bool>> filter = null)
    {
        var query = repository.AsQueryable();
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var data = await query.Select(selector).ToListAsync(ct);
        var json = JsonSerializer.Serialize(data, MoongladeJsonSerializerOptions.Default);

        return new()
        {
            ExportFormat = ExportFormat.SingleJsonFile,
            Content = Encoding.UTF8.GetBytes(json)
        };
    }
}
