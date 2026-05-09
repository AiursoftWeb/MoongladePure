namespace MoongladePure.Data.Entities;

public class SiteDomainEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public string Host { get; set; }
    public bool IsPrimary { get; set; }
    public SiteDomainVerificationStatus VerificationStatus { get; set; } = SiteDomainVerificationStatus.Verified;
    public string VerificationToken { get; set; }
    public DateTime? LastVerifiedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string LastVerificationError { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual SiteEntity Site { get; set; }
}

public enum SiteDomainVerificationStatus
{
    Verified = 0,
    PendingVerification = 1,
    Rejected = 2
}
