using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Exporting.Exporters;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Exporting;

public record ExportCategoryDataCommand : IRequest<ExportResult>;

public class ExportCategoryDataCommandHandler(IRepository<CategoryEntity> repo)
    : IRequestHandler<ExportCategoryDataCommand, ExportResult>
{
    public Task<ExportResult> Handle(ExportCategoryDataCommand request, CancellationToken ct)
    {
        var catExp = new CSVExporter<CategoryEntity>(repo, "moonglade-categories", ExportManager.DataDir);
        return catExp.ExportData(p => new
        {
            p.Id,
            p.DisplayName,
            p.RouteName,
            p.Note
        }, ct);
    }
}