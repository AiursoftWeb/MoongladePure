using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class FriendLinkSpec : BaseSpecification<FriendLinkEntity>
{
    public FriendLinkSpec(Guid id) : base(f => f.Id == id)
    {

    }
}