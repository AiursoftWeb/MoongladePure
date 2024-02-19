using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Exporting.Exporters;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Exporting;

public record ExportLinkDataCommand : IRequest<ExportResult>;

public class ExportLinkDataCommandHandler(IRepository<FriendLinkEntity> repo)
    : IRequestHandler<ExportLinkDataCommand, ExportResult>
{
    public Task<ExportResult> Handle(ExportLinkDataCommand request, CancellationToken ct)
    {
        var fdExp = new CSVExporter<FriendLinkEntity>(repo, "moonglade-friendlinks", ExportManager.DataDir);
        return fdExp.ExportData(p => p, ct);
    }
}