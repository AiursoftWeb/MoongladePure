﻿using MoongladePure.Caching.Filters;
using MoongladePure.Core.TagFeature;
using System.ComponentModel.DataAnnotations;

namespace MoongladePure.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TagsController(IMediator mediator) : ControllerBase
{
    [HttpGet("names")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Names()
    {
        var tagNames = await mediator.Send(new GetTagNamesQuery());
        return Ok(tagNames);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([Required][FromBody] string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        if (!Tag.ValidateName(name)) return Conflict();

        await mediator.Send(new CreateTagCommand(name.Trim()));
        return Ok();
    }

    [HttpPut("{id:int}")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
    public async Task<IActionResult> Update([Range(1, int.MaxValue)] int id, [Required][FromBody] string name)
    {
        var oc = await mediator.Send(new UpdateTagCommand(id, name));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([Range(0, int.MaxValue)] int id)
    {
        var oc = await mediator.Send(new DeleteTagCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }
}