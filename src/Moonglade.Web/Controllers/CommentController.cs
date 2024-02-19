using Microsoft.AspNetCore.Mvc.ModelBinding;
using MoongladePure.Web.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MoongladePure.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[CommentProviderGate]
public class CommentController(
    IMediator mediator,
    IBlogConfig blogConfig) : ControllerBase
{
    [HttpPost("{postId:guid}")]
    [AllowAnonymous]
    [ServiceFilter(typeof(ValidateCaptcha))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([NotEmpty] Guid postId, CommentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Email) && !Helper.IsValidEmailAddress(request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "Invalid Email address.");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        if (!blogConfig.ContentSettings.EnableComments) return Forbid();

        var ip = (bool)HttpContext.Items["DNT"] ? "N/A" : Helper.GetClientIP(HttpContext);
        var item = await mediator.Send(new CreateCommentCommand(postId, request, ip));

        if (item is null)
        {
            ModelState.AddModelError(nameof(request.Content), "Your comment contains bad bad word.");
            return Conflict(ModelState);
        }

        if (blogConfig.ContentSettings.RequireCommentReview)
        {
            return Created("moonglade://empty", item);
        }

        return Ok();
    }

    [HttpPut("{commentId:guid}/approval/toggle")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approval([NotEmpty] Guid commentId)
    {
        await mediator.Send(new ToggleApprovalCommand(new[] { commentId }));
        return Ok(commentId);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(Guid[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete([FromBody][MinLength(1)] Guid[] commentIds)
    {
        await mediator.Send(new DeleteCommentsCommand(commentIds));
        return Ok(commentIds);
    }

    [HttpPost("{commentId:guid}/reply")]
    [ProducesResponseType(typeof(CommentReply), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reply(
        [NotEmpty] Guid commentId,
        [Required][FromBody] string replyContent,
        [FromServices] LinkGenerator linkGenerator)
    {
        if (!blogConfig.ContentSettings.EnableComments) return Forbid();

        var reply = await mediator.Send(new ReplyCommentCommand(commentId, replyContent));

        return Ok(reply);
    }
}