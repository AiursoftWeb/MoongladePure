using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MoongladePure.SaaS.Hosting;

namespace MoongladePure.SaaS.Registration;

public sealed class SaaSRegistrationEndpoint(
    IOptions<SaaSOptions> options,
    SaaSSiteProvisioningService provisioningService)
{
    private const string PasswordError = "Password must be 8-32 characters and include letters and numbers.";
    private static readonly Regex PasswordPattern = new(
        "^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,32}$",
        RegexOptions.Compiled);

    public IResult ShowForm() =>
        Results.Content(SaaSRegistrationHtml.Form(), "text/html; charset=utf-8");

    public async Task<IResult> RegisterFormAsync(HttpRequest request, CancellationToken ct = default)
    {
        var form = await request.ReadFormAsync(ct);
        var input = new SaaSRegistrationInput(
            form["username"],
            form["password"],
            form["email"],
            form["displayName"],
            form["siteName"]);

        var response = await RegisterAsync(input, ct);
        if (!response.Succeeded)
        {
            return Results.Content(
                SaaSRegistrationHtml.Form(response.Error, input),
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Content(
            SaaSRegistrationHtml.Success(response.Host),
            "text/html; charset=utf-8",
            statusCode: StatusCodes.Status201Created);
    }

    public async Task<IResult> RegisterJsonAsync(SaaSRegistrationInput input, CancellationToken ct = default)
    {
        var response = await RegisterAsync(input, ct);
        return response.Succeeded
            ? Results.Created($"https://{response.Host}/", response)
            : Results.BadRequest(response);
    }

    public async Task<SaaSRegistrationResponse> RegisterAsync(SaaSRegistrationInput input, CancellationToken ct = default)
    {
        var passwordResult = ValidatePassword(input.Password);
        if (passwordResult is not null)
        {
            return new SaaSRegistrationResponse(false, Guid.Empty, Guid.Empty, Guid.Empty, null, passwordResult);
        }

        var password = HashPassword(input.Password.Trim());
        var result = await provisioningService.ProvisionAsync(new SaaSSiteProvisioningRequest(
            input.Username,
            options.Value.SiteSubdomainRoot,
            input.Email,
            input.DisplayName,
            input.SiteName,
            password.Salt,
            password.Hash), ct);

        return SaaSRegistrationResponse.FromProvisioning(result);
    }

    private static string ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        return PasswordPattern.IsMatch(password.Trim()) ? null : PasswordError;
    }

    private static PasswordHash HashPassword(string clearPassword)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(128 / 8);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: clearPassword,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return new PasswordHash(salt, hash);
    }

    private sealed record PasswordHash(string Salt, string Hash);
}
