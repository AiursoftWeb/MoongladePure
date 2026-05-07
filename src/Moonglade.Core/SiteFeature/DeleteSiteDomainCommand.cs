using MoongladePure.Data;

namespace MoongladePure.Core.SiteFeature;

public record DeleteSiteDomainCommand(Guid Id) : IRequest<OperationCode>;

public class DeleteSiteDomainCommandHandler(IRepository<SiteDomainEntity> domainRepo)
    : IRequestHandler<DeleteSiteDomainCommand, OperationCode>
{
    public async Task<OperationCode> Handle(DeleteSiteDomainCommand request, CancellationToken ct)
    {
        var domain = await domainRepo.GetAsync(request.Id, ct);
        if (domain is null)
        {
            return OperationCode.ObjectNotFound;
        }

        await domainRepo.DeleteAsync(domain, ct);
        return OperationCode.Done;
    }
}
