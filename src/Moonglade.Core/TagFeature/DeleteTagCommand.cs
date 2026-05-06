using MoongladePure.Data;
using MoongladePure.Data.Spec;

namespace MoongladePure.Core.TagFeature;

public record DeleteTagCommand(int Id) : IRequest<OperationCode>;

public class DeleteTagCommandHandler(IRepository<TagEntity> tagRepo, IRepository<PostTagEntity> postTagRepo, ISiteContext siteContext)
    : IRequestHandler<DeleteTagCommand, OperationCode>
{
    public async Task<OperationCode> Handle(DeleteTagCommand request, CancellationToken ct)
    {
        var exists = await tagRepo.AnyAsync(c => c.SiteId == siteContext.SiteId && c.Id == request.Id, ct);
        if (!exists) return OperationCode.ObjectNotFound;

        // 1. Delete Post-Tag Association
        var postTags = await postTagRepo.ListAsync(new PostTagSpec(request.Id, siteContext.SiteId));
        await postTagRepo.DeleteAsync(postTags, ct);

        // 2. Delte Tag itslef
        await tagRepo.DeleteAsync(request.Id, ct);

        return OperationCode.Done;
    }
}
