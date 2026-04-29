namespace MoongladePure.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest;

public class DeletePageCommandHandler(IRepository<PageEntity> repo) : IRequestHandler<DeletePageCommand>
{
    public async Task Handle(DeletePageCommand request, CancellationToken ct)
    {
        var page = await repo.GetAsync(p => p.SiteId == SystemIds.DefaultSiteId && p.Id == request.Id);
        if (page is not null) await repo.DeleteAsync(page, ct);
    }
}
