using MoongladePure.Data.Entities;

namespace MoongladePure.Data.Infrastructure;

public interface ISiteContext
{
    Guid SiteId { get; }
}

public sealed class DefaultSiteContext : ISiteContext
{
    public Guid SiteId => SystemIds.DefaultSiteId;
}
