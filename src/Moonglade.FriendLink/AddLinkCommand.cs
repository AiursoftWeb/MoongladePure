﻿using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Utils;
using System.ComponentModel.DataAnnotations;

namespace MoongladePure.FriendLink;

public class AddLinkCommand : IRequest, IValidatableObject
{
    [Required]
    [Display(Name = "Title")]
    [MaxLength(64)]
    public string Title { get; set; }

    [Required]
    [Display(Name = "Link")]
    [DataType(DataType.Url)]
    [MaxLength(256)]
    public string LinkUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.IsWellFormedUriString(LinkUrl, UriKind.Absolute))
        {
            yield return new($"{nameof(LinkUrl)} is not a valid url.");
        }
    }
}

public class AddLinkCommandHandler : AsyncRequestHandler<AddLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public AddLinkCommandHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    protected override async Task Handle(AddLinkCommand request, CancellationToken ct)
    {
        var link = new FriendLinkEntity
        {
            Id = Guid.NewGuid(),
            LinkUrl = Helper.SterilizeLink(request.LinkUrl),
            Title = request.Title
        };

        await _repo.AddAsync(link, ct);
    }
}