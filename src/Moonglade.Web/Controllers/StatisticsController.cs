﻿using MoongladePure.Core.StatisticFeature;
using MoongladePure.Web.Attributes;

namespace MoongladePure.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IMediator _mediator;

    private bool DNT => (bool)HttpContext.Items["DNT"]!;

    public StatisticsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{postId:guid}")]
    [DisallowSpiderUA]
    [ProducesResponseType(typeof(Tuple<int, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([NotEmpty] Guid postId)
    {
        var (hits, likes) = await _mediator.Send(new GetStatisticQuery(postId));
        return Ok(new { Hits = hits, Likes = likes });
    }

    [HttpPost]
    [DisallowSpiderUA]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(StatisticsRequest request)
    {
        if (DNT) return NoContent();

        await _mediator.Send(new UpdateStatisticCommand(request.PostId, request.IsLike));
        return NoContent();
    }
}

public record StatisticsRequest([NotEmpty] Guid PostId, bool IsLike);