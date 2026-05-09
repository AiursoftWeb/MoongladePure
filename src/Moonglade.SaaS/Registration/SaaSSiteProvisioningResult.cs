namespace MoongladePure.SaaS.Registration;

public sealed record SaaSSiteProvisioningResult(
    bool Succeeded,
    Guid TenantId,
    Guid UserId,
    Guid SiteId,
    string Host,
    string Error)
{
    public static SaaSSiteProvisioningResult Success(Guid tenantId, Guid userId, Guid siteId, string host) =>
        new(true, tenantId, userId, siteId, host, null);

    public static SaaSSiteProvisioningResult Fail(string error) =>
        new(false, Guid.Empty, Guid.Empty, Guid.Empty, null, error);
}
