namespace MoongladePure.SaaS.Hosting;

public sealed class SaaSOptions
{
    public string[] PortalHosts { get; set; } = [];
    public string SiteSubdomainRoot { get; set; }
}
