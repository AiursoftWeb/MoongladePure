using System.Linq.Expressions;

namespace MoongladePure.Data.Exporting.Exporters;

public interface IExporter<T>
{
    Task<ExportResult> ExportData<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct, Expression<Func<T, bool>> filter = null);
}
