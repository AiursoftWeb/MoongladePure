using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Exporting.Exporters;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Exporting;

public record ExportPageDataCommand : IRequest<ExportResult>;

public class ExportPageDataCommandHandler : IRequestHandler<ExportPageDataCommand, ExportResult>
{
    private readonly IRepository<PageEntity> _repo;
    public ExportPageDataCommandHandler(IRepository<PageEntity> repo) => _repo = repo;

    public Task<ExportResult> Handle(ExportPageDataCommand request, CancellationToken ct)
    {
        var pgExp = new ZippedJsonExporter<PageEntity>(_repo, "moonglade-pages", ExportManager.DataDir);
        return pgExp.ExportData(p => new
        {
            p.Id,
            p.Title,
            p.Slug,
            p.MetaDescription,
            p.HtmlContent,
            p.CssContent,
            p.HideSidebar,
            p.IsPublished,
            p.CreateTimeUtc,
            p.UpdateTimeUtc
        }, ct);
    }
}