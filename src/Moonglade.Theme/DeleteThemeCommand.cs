using MediatR;
using MoongladePure.Data;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Theme;

public record DeleteThemeCommand(int Id) : IRequest<OperationCode>;

public class DeleteThemeCommandHandler : IRequestHandler<DeleteThemeCommand, OperationCode>
{
    private readonly IRepository<BlogThemeEntity> _repo;

    public DeleteThemeCommandHandler(IRepository<BlogThemeEntity> repo) => _repo = repo;

    public async Task<OperationCode> Handle(DeleteThemeCommand request, CancellationToken ct)
    {
        var theme = await _repo.GetAsync(request.Id, ct);
        if (null == theme) return OperationCode.ObjectNotFound;
        if (theme.ThemeType == ThemeType.System) return OperationCode.Canceled;

        await _repo.DeleteAsync(request.Id, ct);
        return OperationCode.Done;
    }
}