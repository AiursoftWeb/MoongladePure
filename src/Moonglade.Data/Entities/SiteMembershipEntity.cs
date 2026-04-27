namespace MoongladePure.Data.Entities;

public class SiteMembershipEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public Guid UserId { get; set; } = SystemIds.DefaultAdminUserId;
    public SiteRole Role { get; set; } = SiteRole.Admin;
    public string DisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual SiteEntity Site { get; set; }
    public virtual LocalAccountEntity User { get; set; }
}

public enum SiteRole
{
    Owner = 0,
    Admin = 1,
    Editor = 2
}
