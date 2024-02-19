using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Exporting.Exporters;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Exporting;

public record ExportTagsDataCommand : IRequest<ExportResult>;

public class ExportTagsDataCommandHandler(IRepository<TagEntity> repo)
    : IRequestHandler<ExportTagsDataCommand, ExportResult>
{
    public Task<ExportResult> Handle(ExportTagsDataCommand request, CancellationToken ct)
    {
        var tagExp = new CSVExporter<TagEntity>(repo, "moonglade-tags", ExportManager.DataDir);
        return tagExp.ExportData(p => new
        {
            p.Id,
            p.NormalizedName,
            p.DisplayName
        }, ct);
    }
}