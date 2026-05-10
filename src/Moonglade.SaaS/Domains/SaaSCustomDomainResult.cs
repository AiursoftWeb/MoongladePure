namespace MoongladePure.SaaS.Domains;

public sealed record SaaSCustomDomainResult(
    bool Succeeded,
    SaaSCustomDomainDigest Domain,
    string Error)
{
    public static SaaSCustomDomainResult Success(SaaSCustomDomainDigest domain) => new(true, domain, null);

    public static SaaSCustomDomainResult Fail(string error) => new(false, null, error);
}
