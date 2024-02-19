namespace MoongladePure.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest;

public class DeletePageCommandHandler(IRepository<PageEntity> repo) : IRequestHandler<DeletePageCommand>
{
    public async Task Handle(DeletePageCommand request, CancellationToken ct) =>
        await repo.DeleteAsync(request.Id, ct);
}