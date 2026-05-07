namespace MoongladePure.Web;

public static class SiteCacheKey
{
    public static string For(Guid siteId, string key) => $"{siteId}:{key}";
}
