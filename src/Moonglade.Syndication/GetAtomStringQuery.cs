using MediatR;
using Microsoft.AspNetCore.Http;
using MoongladePure.Configuration;
using MoongladePure.Utils;

namespace MoongladePure.Syndication;

public record GetAtomStringQuery : IRequest<string>;

public class GetAtomStringQueryHandler : IRequestHandler<GetAtomStringQuery, string>
{
    private readonly ISyndicationDataSource _sdds;
    private readonly FeedGenerator _feedGenerator;

    public GetAtomStringQueryHandler(IBlogConfig blogConfig, ISyndicationDataSource sdds, IHttpContextAccessor httpContextAccessor)
    {
        _sdds = sdds;

        var acc = httpContextAccessor;
        var baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";

        _feedGenerator = new(
            baseUrl,
            blogConfig.FeedSettings.RssTitle,
            blogConfig.GeneralSettings.Description,
            Helper.FormatCopyright2Html(blogConfig.GeneralSettings.Copyright).Replace("&copy;", "©"),
            $"MoongladePure v{Helper.AppVersion}",
            baseUrl);
    }

    public async Task<string> Handle(GetAtomStringQuery request, CancellationToken ct)
    {
        _feedGenerator.FeedItemCollection = await _sdds.GetFeedDataAsync();
        var xml = await _feedGenerator.WriteAtomAsync();
        return xml;
    }
}