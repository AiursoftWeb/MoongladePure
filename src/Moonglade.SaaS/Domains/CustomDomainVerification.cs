using System.Security.Cryptography;

namespace MoongladePure.SaaS.Domains;

public static class CustomDomainVerification
{
    public const string TxtRecordPrefix = "_moonglade";
    public const string TxtValuePrefix = "moonglade-site-verification=";

    public static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string BuildTxtRecordName(string host)
    {
        var normalizedHost = NormalizeHost(host);
        return string.IsNullOrWhiteSpace(normalizedHost) ? null : $"{TxtRecordPrefix}.{normalizedHost}";
    }

    public static string BuildTxtRecordValue(string token) =>
        string.IsNullOrWhiteSpace(token) ? null : TxtValuePrefix + token.Trim();

    private static string NormalizeHost(string host) =>
        string.IsNullOrWhiteSpace(host) ? null : host.Trim().TrimEnd('.').ToLowerInvariant();
}
