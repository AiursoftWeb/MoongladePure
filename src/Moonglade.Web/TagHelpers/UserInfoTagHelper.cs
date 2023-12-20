using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Security.Claims;

namespace MoongladePure.Web.TagHelpers;

[HtmlTargetElement("userinfo", TagStructure = TagStructure.NormalOrSelfClosing)]
public class UserInfoTagHelper : TagHelper
{
    public ClaimsPrincipal User { get; set; }

    public static string TagClassBase => "aspnet-tag-moonglade-userinfo";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (User?.Identity is null || !User.Identity.IsAuthenticated)
        {
            base.Process(context, output);
        }
        else
        {
            var name = GetName();
            output.TagName = "div";
            output.Attributes.SetAttribute("class", TagClassBase);
            output.Content.SetContent(name);
        }
    }

    private string GetName()
    {
        string name = null;

        // try non-standard name
        if (User.HasClaim(c => c.Type.ToLower() == "name"))
        {
            name = User.Claims.FirstOrDefault(c => c.Type.ToLower() == "name")?.Value;
        }

        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (User.Identity != null) name = User.Identity.Name;
        // if (string.IsNullOrWhiteSpace(name)) name = "N/A";

        return name;
    }

}