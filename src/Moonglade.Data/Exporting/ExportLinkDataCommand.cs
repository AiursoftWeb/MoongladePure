using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Exporting.Exporters;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Exporting;

public record ExportLinkDataCommand : IRequest<ExportResult>;

public class ExportLinkDataCommandHandler : IRequestHandler<ExportLinkDataCommand, ExportResult>
{
    private readonly IRepository<FriendLinkEntity> _repo;
    public ExportLinkDataCommandHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public Task<ExportResult> Handle(ExportLinkDataCommand request, CancellationToken ct)
    {
        var fdExp = new CSVExporter<FriendLinkEntity>(_repo, "moonglade-friendlinks", ExportManager.DataDir);
        return fdExp.ExportData(p => p, ct);
    }
}