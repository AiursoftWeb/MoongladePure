using MoongladePure.Data;
using MoongladePure.Utils;

namespace MoongladePure.Core.TagFeature;

public record UpdateTagCommand(int Id, string Name) : IRequest<OperationCode>;

public class UpdateTagCommandHandler(IRepository<TagEntity> repo, ISiteContext siteContext) : IRequestHandler<UpdateTagCommand, OperationCode>
{
    public async Task<OperationCode> Handle(UpdateTagCommand request, CancellationToken ct)
    {
        var (id, name) = request;
        var tag = await repo.GetAsync(t => t.SiteId == siteContext.SiteId && t.Id == id);
        if (null == tag) return OperationCode.ObjectNotFound;

        tag.DisplayName = name;
        tag.NormalizedName = Tag.NormalizeName(name, Helper.TagNormalizationDictionary);
        await repo.UpdateAsync(tag, ct);

        return OperationCode.Done;
    }
}
