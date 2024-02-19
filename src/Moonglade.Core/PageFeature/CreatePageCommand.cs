namespace MoongladePure.Core.PageFeature;

public record CreatePageCommand(EditPageRequest Payload) : IRequest<Guid>;

public class CreatePageCommandHandler(IRepository<PageEntity> repo) : IRequestHandler<CreatePageCommand, Guid>
{
    public async Task<Guid> Handle(CreatePageCommand request, CancellationToken ct)
    {
        var uid = Guid.NewGuid();
        var page = new PageEntity
        {
            Id = uid,
            Title = request.Payload.Title.Trim(),
            Slug = request.Payload.Slug.ToLower().Trim(),
            MetaDescription = request.Payload.MetaDescription,
            CreateTimeUtc = DateTime.UtcNow,
            HtmlContent = request.Payload.RawHtmlContent,
            CssContent = request.Payload.CssContent,
            HideSidebar = request.Payload.HideSidebar,
            IsPublished = request.Payload.IsPublished
        };

        await repo.AddAsync(page, ct);

        return uid;
    }
}