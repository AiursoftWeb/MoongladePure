using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Utils;

namespace MoongladePure.FriendLink;

public class UpdateLinkCommand : AddLinkCommand
{
    public Guid Id { get; set; }
}

public class UpdateLinkCommandHandler : IRequestHandler<UpdateLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public UpdateLinkCommandHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public async Task Handle(UpdateLinkCommand request, CancellationToken ct)
    {
        if (!Uri.IsWellFormedUriString(request.LinkUrl, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{nameof(request.LinkUrl)} is not a valid url.");
        }

        var link = await _repo.GetAsync(request.Id, ct);
        if (link is not null)
        {
            link.Title = request.Title;
            link.LinkUrl = Helper.SterilizeLink(request.LinkUrl);

            await _repo.UpdateAsync(link, ct);
        }
    }
}