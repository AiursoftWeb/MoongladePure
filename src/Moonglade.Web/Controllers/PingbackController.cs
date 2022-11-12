using Moonglade.Data.Entities;
using Moonglade.Pingback;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("pingback")]
public class PingbackController : ControllerBase
{
    private readonly ILogger<PingbackController> _logger;
    private readonly IBlogConfig _blogConfig;
    private readonly IMediator _mediator;

    public PingbackController(
        ILogger<PingbackController> logger,
        IBlogConfig blogConfig,
        IMediator mediator)
    {
        _logger = logger;
        _blogConfig = blogConfig;
        _mediator = mediator;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Process()
    {
        if (!_blogConfig.AdvancedSettings.EnablePingbackReceive)
        {
            _logger.LogInformation("Pingback receive is disabled");
            return Forbid();
        }

        return new PingbackResult(PingbackResponse.Success);
    }

    [Authorize]
    [HttpDelete("{pingbackId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid pingbackId)
    {
        await _mediator.Send(new DeletePingbackCommand(pingbackId));
        return NoContent();
    }

    [Authorize]
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await _mediator.Send(new ClearPingbackCommand());
        return NoContent();
    }
}