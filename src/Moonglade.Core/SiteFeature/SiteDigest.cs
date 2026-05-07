namespace MoongladePure.Core.SiteFeature;

public class SiteDigest
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public SiteStatus Status { get; set; }
    public string DefaultCulture { get; set; }
    public string TimeZoneId { get; set; }
    public IReadOnlyList<SiteDomainDigest> Domains { get; set; } = Array.Empty<SiteDomainDigest>();
}

public class SiteDomainDigest
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Host { get; set; }
    public bool IsPrimary { get; set; }
}
