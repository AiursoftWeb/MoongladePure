namespace MoongladePure.Web.ViewComponents;

public class CommentListViewComponent(ILogger<CommentListViewComponent> logger, IMediator mediator)
    : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid postId)
    {
        try
        {
            if (postId == Guid.Empty)
            {
                logger.LogError("postId: {PostId} is not a valid GUID", postId);
                throw new ArgumentOutOfRangeException(nameof(postId));
            }

            var comments = await mediator.Send(new GetApprovedCommentsQuery(postId));
            return View(comments);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error reading comments for post id: {PostId}", postId);
            return Content(e.Message);
        }
    }
}