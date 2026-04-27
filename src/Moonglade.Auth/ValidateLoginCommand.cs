using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Utils;

namespace MoongladePure.Auth;

public record ValidateLoginCommand(string Username, string InputPassword) : IRequest<Guid>;

public class ValidateLoginCommandHandler(IRepository<LocalAccountEntity> repo)
    : IRequestHandler<ValidateLoginCommand, Guid>
{
    public async Task<Guid> Handle(ValidateLoginCommand request, CancellationToken ct)
    {
        var username = request.Username.ToLower().Trim();
        var account = await repo.GetAsync(p => p.NormalizedUsername == username || p.Username == username);
        if (account is null) return Guid.Empty;

        var valid = account.PasswordHash == (string.IsNullOrWhiteSpace(account.PasswordSalt)
            ? Helper.HashPassword(request.InputPassword.Trim())
            : Helper.HashPassword2(request.InputPassword.Trim(), account.PasswordSalt));

        // migrate old account to salt
        if (valid && string.IsNullOrWhiteSpace(account.PasswordSalt))
        {
            var salt = Helper.GenerateSalt();
            var newHash = Helper.HashPassword2(request.InputPassword.Trim(), salt);

            account.PasswordSalt = salt;
            account.PasswordHash = newHash;
            account.NormalizedUsername = account.Username.ToLower().Trim();

            await repo.UpdateAsync(account, ct);
        }

        return valid ? account.Id : Guid.Empty;
    }
}
