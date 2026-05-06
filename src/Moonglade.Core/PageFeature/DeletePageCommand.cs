namespace MoongladePure.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest;

public class DeletePageCommandHandler(IRepository<PageEntity> repo, ISiteContext siteContext) : IRequestHandler<DeletePageCommand>
{
    public async Task Handle(DeletePageCommand request, CancellationToken ct)
    {
        var page = await repo.GetAsync(p => p.SiteId == siteContext.SiteId && p.Id == request.Id);
        if (page is not null) await repo.DeleteAsync(page, ct);
    }
}
