using MoongladePure.Data;

namespace MoongladePure.Core.SiteFeature;

public record AddSiteDomainCommand(Guid SiteId, string Host, bool IsPrimary = false) : IRequest<OperationCode>;

public class AddSiteDomainCommandHandler(
    IRepository<SiteEntity> siteRepo,
    IRepository<SiteDomainEntity> domainRepo)
    : IRequestHandler<AddSiteDomainCommand, OperationCode>
{
    public async Task<OperationCode> Handle(AddSiteDomainCommand request, CancellationToken ct)
    {
        var host = NormalizeHost(request.Host);
        if (string.IsNullOrWhiteSpace(host))
        {
            return OperationCode.Canceled;
        }

        var siteExists = await siteRepo.AnyAsync(site => site.Id == request.SiteId, ct);
        if (!siteExists)
        {
            return OperationCode.ObjectNotFound;
        }

        var hostExists = await domainRepo.AnyAsync(domain => domain.Host == host, ct);
        if (hostExists)
        {
            return OperationCode.Canceled;
        }

        await domainRepo.AddAsync(new SiteDomainEntity
        {
            Id = Guid.NewGuid(),
            SiteId = request.SiteId,
            Host = host,
            IsPrimary = request.IsPrimary,
            VerificationStatus = SiteDomainVerificationStatus.Verified,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        return OperationCode.Done;
    }

    private static string NormalizeHost(string host) =>
        string.IsNullOrWhiteSpace(host) ? null : host.Trim().ToLowerInvariant();
}
