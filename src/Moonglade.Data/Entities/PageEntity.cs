namespace MoongladePure.Data.Entities;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

public class PageEntity
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public string Title { get; set; }
    public string Slug { get; set; }
    public string MetaDescription { get; set; }
    public string HtmlContent { get; set; }
    public string CssContent { get; set; }
    public bool HideSidebar { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public DateTime? UpdateTimeUtc { get; set; }

    public virtual SiteEntity Site { get; set; }
}
