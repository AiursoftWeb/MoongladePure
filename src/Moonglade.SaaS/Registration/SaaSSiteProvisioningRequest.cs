namespace MoongladePure.SaaS.Registration;

public sealed record SaaSSiteProvisioningRequest(
    string Username,
    string SiteSubdomainRoot,
    string Email = null,
    string DisplayName = null,
    string SiteName = null,
    string PasswordSalt = null,
    string PasswordHash = null);
