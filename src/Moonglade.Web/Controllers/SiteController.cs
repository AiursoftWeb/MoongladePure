using MoongladePure.Core.SiteFeature;
using MoongladePure.Web.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MoongladePure.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SiteController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SiteDigest>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var sites = await mediator.Send(new ListSitesQuery());
        return Ok(sites);
    }

    [HttpPost("{siteId:guid}/domains")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddDomain([NotEmpty] Guid siteId, AddSiteDomainRequest request)
    {
        var result = await mediator.Send(new AddSiteDomainCommand(siteId, request.Host, request.IsPrimary));
        return result switch
        {
            OperationCode.ObjectNotFound => NotFound(),
            OperationCode.Canceled => Conflict(),
            _ => Created(string.Empty, request)
        };
    }

    [HttpDelete("domains/{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> DeleteDomain([NotEmpty] Guid id)
    {
        var result = await mediator.Send(new DeleteSiteDomainCommand(id));
        if (result == OperationCode.ObjectNotFound)
        {
            return NotFound();
        }

        return NoContent();
    }
}

public class AddSiteDomainRequest
{
    [Required]
    [MaxLength(256)]
    public string Host { get; set; }

    public bool IsPrimary { get; set; }
}
