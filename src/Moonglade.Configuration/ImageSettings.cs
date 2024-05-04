using MoongladePure.Utils;
using System.ComponentModel.DataAnnotations;

namespace MoongladePure.Configuration;

public class ImageSettings : IBlogSettings, IValidatableObject
{
    [Display(Name = "Enabled watermark")]
    public bool IsWatermarkEnabled { get; set; }

    [Display(Name = "Keep origin image")]
    public bool KeepOriginImage { get; set; }

    [Required]
    [Display(Name = "Font size")]
    [Range(8, 32)]
    public int WatermarkFontSize { get; set; }

    [Required]
    [Display(Name = "Watermark text")]
    [MaxLength(32)]
    public string WatermarkText { get; set; }

    [Required]
    [Display(Name = "A")]
    [Range(0, 255)]
    public int WatermarkColorA { get; set; } = 128;

    [Required]
    [Display(Name = "R")]
    [Range(0, 255)]
    public int WatermarkColorR { get; set; } = 128;

    [Required]
    [Display(Name = "G")]
    [Range(0, 255)]
    public int WatermarkColorG { get; set; } = 128;

    [Required]
    [Display(Name = "B")]
    [Range(0, 255)]
    public int WatermarkColorB { get; set; } = 128;

    [Required]
    [Display(Name = "Watermark skip pixel threshold")]
    [Range(0, int.MaxValue)]
    public int WatermarkSkipPixel { get; set; } = 40000;

    [Display(Name = "Fit image to device pixel ratio")]
    public bool FitImageToDevicePixelRatio { get; set; }

    [Display(Name = "Enable CDN for images")]
    public bool EnableCdnRedirect { get; set; }

    [DataType(DataType.Url)]
    [MaxLength(128)]
    [Display(Name = "CDN endpoint")]
    public string CdnEndpoint { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EnableCdnRedirect)
        {
            if (string.IsNullOrWhiteSpace(CdnEndpoint))
            {
                EnableCdnRedirect = false;
                yield return new($"{nameof(CdnEndpoint)} must be specified when {nameof(EnableCdnRedirect)} is enabled.");
            }

            // Validate endpoint Url to avoid security risks
            // But it still has risks:
            // e.g. If the endpoint is compromised, the attacker could return any kind of response from a image with a big fuck to a script that can attack users.

            var endpoint = CdnEndpoint;
            var isValidEndpoint = endpoint.IsValidUrl(UrlExtension.UrlScheme.Https);
            if (!isValidEndpoint)
            {
                yield return new("CDN Endpoint is not a valid HTTPS Url.");
            }
        }
    }
}