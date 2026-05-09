namespace MoongladePure.SaaS.Registration;

public sealed record SaaSRegistrationResponse(
    bool Succeeded,
    Guid TenantId,
    Guid UserId,
    Guid SiteId,
    string Host,
    string Error)
{
    public static SaaSRegistrationResponse FromProvisioning(SaaSSiteProvisioningResult result) =>
        new(result.Succeeded, result.TenantId, result.UserId, result.SiteId, result.Host, result.Error);
}
