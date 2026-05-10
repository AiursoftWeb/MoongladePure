using MoongladePure.Data.Entities;

namespace MoongladePure.SaaS.Domains;

public sealed class SaaSCustomDomainDigest
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Host { get; set; }
    public bool IsPrimary { get; set; }
    public SiteDomainVerificationStatus VerificationStatus { get; set; }
    public string VerificationToken { get; set; }
    public string TxtRecordName { get; set; }
    public string TxtRecordValue { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastVerifiedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string LastVerificationError { get; set; }
}
