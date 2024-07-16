using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MoongladePure.Web.TagHelpers;

[HtmlTargetElement("metadesc", TagStructure = TagStructure.NormalOrSelfClosing)]
public class MetaDescriptionTagHelper : TagHelper
{
    public string Description { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "meta";
        output.Attributes.SetAttribute("name", "description");
        if (Description.Length > 200)
        {
            output.Attributes.SetAttribute("content", Description.Substring(0, 200).Trim());
        }
        else
        {
            output.Attributes.SetAttribute("content", Description?.Trim() ?? string.Empty);
        }
    }
}