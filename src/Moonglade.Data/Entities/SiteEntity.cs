namespace MoongladePure.Data.Entities;

public class SiteEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; } = SystemIds.DefaultTenantId;
    public string Name { get; set; }
    public string Slug { get; set; }
    public SiteStatus Status { get; set; } = SiteStatus.Active;
    public string DefaultCulture { get; set; } = "en-US";
    public string TimeZoneId { get; set; } = "UTC";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public virtual TenantEntity Tenant { get; set; }
    public virtual ICollection<SiteDomainEntity> Domains { get; set; } = new HashSet<SiteDomainEntity>();
}

public enum SiteStatus
{
    Active = 0,
    Suspended = 1
}
