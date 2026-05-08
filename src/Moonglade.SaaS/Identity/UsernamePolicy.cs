using System.Text.RegularExpressions;

namespace MoongladePure.SaaS.Identity;

public sealed class UsernamePolicy
{
    private const int MinimumLength = 3;
    private const int MaximumLength = 32;
    private static readonly Regex Pattern = new("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$", RegexOptions.Compiled);
    private static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
    {
        "admin",
        "api",
        "app",
        "assets",
        "auth",
        "blog",
        "cdn",
        "docs",
        "localhost",
        "mail",
        "root",
        "smtp",
        "static",
        "status",
        "support",
        "www"
    };

    public UsernameValidationResult Validate(string username)
    {
        var normalized = Normalize(username);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return UsernameValidationResult.Fail("Username is required.");
        }

        if (normalized.Length < MinimumLength || normalized.Length > MaximumLength)
        {
            return UsernameValidationResult.Fail("Username length is invalid.");
        }

        if (!Pattern.IsMatch(normalized))
        {
            return UsernameValidationResult.Fail("Username format is invalid.");
        }

        if (ReservedNames.Contains(normalized))
        {
            return UsernameValidationResult.Fail("Username is reserved.");
        }

        return UsernameValidationResult.Success(normalized);
    }

    public static string Normalize(string username) =>
        string.IsNullOrWhiteSpace(username) ? null : username.Trim().ToLowerInvariant();
}
