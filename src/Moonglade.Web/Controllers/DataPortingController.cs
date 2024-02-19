using MoongladePure.Data.Exporting;

namespace MoongladePure.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DataPortingController(IMediator mediator) : ControllerBase
{
    [HttpGet("export/{type}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportDownload(ExportType type, CancellationToken ct)
    {
        var exportResult = type switch
        {
            ExportType.Tags => await mediator.Send(new ExportTagsDataCommand(), ct),
            ExportType.Categories => await mediator.Send(new ExportCategoryDataCommand(), ct),
            ExportType.FriendLinks => await mediator.Send(new ExportLinkDataCommand(), ct),
            ExportType.Pages => await mediator.Send(new ExportPageDataCommand(), ct),
            ExportType.Posts => await mediator.Send(new ExportPostDataCommand(), ct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        switch (exportResult.ExportFormat)
        {
            case ExportFormat.SingleJsonFile:
                return new FileContentResult(exportResult.Content, exportResult.ContentType)
                {
                    FileDownloadName = $"moonglade-{type.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json"
                };

            case ExportFormat.SingleCSVFile:
                Response.Headers.Append("Content-Disposition", $"attachment;filename={Path.GetFileName(exportResult.FilePath)}");
                return PhysicalFile(exportResult.FilePath!, exportResult.ContentType, Path.GetFileName(exportResult.FilePath));

            case ExportFormat.ZippedJsonFiles:
                return PhysicalFile(exportResult.FilePath, exportResult.ContentType, Path.GetFileName(exportResult.FilePath));

            default:
                return BadRequest(ModelState.CombineErrorMessages());
        }
    }
}