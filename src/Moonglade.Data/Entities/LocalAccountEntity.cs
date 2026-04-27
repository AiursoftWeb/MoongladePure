namespace MoongladePure.Data.Entities;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

public class LocalAccountEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; } = SystemIds.DefaultTenantId;
    public string Username { get; set; }
    public string NormalizedUsername { get; set; }
    public string Email { get; set; }
    public string NormalizedEmail { get; set; }
    public string PasswordSalt { get; set; }
    public string PasswordHash { get; set; }
    public DateTime? LastLoginTimeUtc { get; set; }
    public string LastLoginIp { get; set; }
    public DateTime CreateTimeUtc { get; set; }

    public virtual TenantEntity Tenant { get; set; }
    public virtual ICollection<SiteMembershipEntity> SiteMemberships { get; set; } = new HashSet<SiteMembershipEntity>();
}
