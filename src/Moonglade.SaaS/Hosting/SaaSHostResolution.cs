namespace MoongladePure.SaaS.Hosting;

public sealed record SaaSHostResolution(
    SaaSHostKind Kind,
    string Host,
    string Username = null)
{
    public static SaaSHostResolution Unknown(string host) => new(SaaSHostKind.Unknown, host);
}
