namespace MoongladePure.Data.Entities;

public class TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<SiteEntity> Sites { get; set; } = new HashSet<SiteEntity>();
}

public enum TenantStatus
{
    Active = 0,
    Suspended = 1
}
