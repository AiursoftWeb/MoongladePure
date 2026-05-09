namespace MoongladePure.SaaS.Registration;

public sealed record SaaSRegistrationInput(
    string Username,
    string Password,
    string Email = null,
    string DisplayName = null,
    string SiteName = null);
