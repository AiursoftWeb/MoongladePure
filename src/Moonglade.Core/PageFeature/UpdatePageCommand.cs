namespace MoongladePure.Core.PageFeature;

public record UpdatePageCommand(Guid Id, EditPageRequest Payload) : IRequest<Guid>;

public class UpdatePageCommandHandler(IRepository<PageEntity> repo) : IRequestHandler<UpdatePageCommand, Guid>
{
    public async Task<Guid> Handle(UpdatePageCommand request, CancellationToken ct)
    {
        var (guid, payload) = request;
        var page = await repo.GetAsync(guid, ct);
        if (page is null)
        {
            throw new InvalidOperationException($"PageEntity with Id '{guid}' not found.");
        }

        page.Title = payload.Title.Trim();
        page.Slug = payload.Slug.ToLower().Trim();
        page.MetaDescription = payload.MetaDescription;
        page.HtmlContent = payload.RawHtmlContent;
        page.CssContent = payload.CssContent;
        page.HideSidebar = payload.HideSidebar;
        page.UpdateTimeUtc = DateTime.UtcNow;
        page.IsPublished = payload.IsPublished;

        await repo.UpdateAsync(page, ct);

        return page.Id;
    }
}