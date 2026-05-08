namespace MoongladePure.SaaS.Identity;

public sealed record UsernameValidationResult(bool Succeeded, string NormalizedUsername, string Error)
{
    public static UsernameValidationResult Success(string normalizedUsername) =>
        new(true, normalizedUsername, null);

    public static UsernameValidationResult Fail(string error) =>
        new(false, null, error);
}
