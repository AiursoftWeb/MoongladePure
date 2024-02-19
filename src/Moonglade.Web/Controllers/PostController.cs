using MoongladePure.Caching.Filters;
using MoongladePure.Core.PostFeature;
using MoongladePure.Web.Attributes;
using NUglify;
using System.ComponentModel.DataAnnotations;

namespace MoongladePure.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PostController(
    IMediator mediator,
    ILogger<PostController> logger) : ControllerBase
{
    [HttpPost("createoredit")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[]
    {
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription |
        BlogCacheType.PagingCount
    })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrEdit(PostEditModel model, [FromServices] LinkGenerator linkGenerator)
    {
        try
        {
            if (!ModelState.IsValid) return Conflict(ModelState.CombineErrorMessages());

            if (!string.IsNullOrWhiteSpace(model.InlineCss))
            {
                var uglifyTest = Uglify.Css(model.InlineCss);
                if (uglifyTest.HasErrors)
                {
                    foreach (var err in uglifyTest.Errors)
                    {
                        ModelState.AddModelError(model.InlineCss, err?.ToString() ?? string.Empty);
                    }
                    return BadRequest(ModelState.CombineErrorMessages());
                }

                model.InlineCss = uglifyTest.Code;
            }

            if (model.ChangePublishDate &&
                model.PublishDate.HasValue &&
                model.PublishDate.GetValueOrDefault().Year >= 1975)
            {
                model.PublishDate =model.PublishDate.Value;
            }

            var postEntity = model.PostId == Guid.Empty ?
                await mediator.Send(new CreatePostCommand(model)) :
                await mediator.Send(new UpdatePostCommand(model.PostId, model));

            return Ok(new { PostId = postEntity.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Creating New Post");
            return Conflict(ex.Message);
        }
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[]
    {
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription |
        BlogCacheType.PagingCount
    })]
    [HttpPost("{postId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restore([NotEmpty] Guid postId)
    {
        await mediator.Send(new RestorePostCommand(postId));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[]
    {
        BlogCacheType.SiteMap |
        BlogCacheType.Subscription |
        BlogCacheType.PagingCount
    })]
    [HttpDelete("{postId:guid}/recycle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await mediator.Send(new DeletePostCommand(postId, true));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription | BlogCacheType.SiteMap })]
    [HttpDelete("{postId:guid}/destroy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteFromRecycleBin([NotEmpty] Guid postId)
    {
        await mediator.Send(new DeletePostCommand(postId));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription | BlogCacheType.SiteMap })]
    [HttpDelete("recyclebin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EmptyRecycleBin()
    {
        await mediator.Send(new PurgeRecycledCommand());
        return NoContent();
    }

    [HttpPost("keep-alive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult KeepAlive([MaxLength(16)] string nonce)
    {
        return Ok(new
        {
            ServerTime = DateTime.UtcNow,
            Nonce = nonce
        });
    }
}