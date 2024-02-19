using Edi.Captcha;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MoongladePure.Web.Pages;

public class SignInModel(
    IMediator mediator,
    ILogger<SignInModel> logger,
    ISessionBasedCaptcha captcha)
    : PageModel
{
    [BindProperty]
    [Required]
    [Display(Name = "Username")]
    [MinLength(2), MaxLength(32)]
    [RegularExpression("[a-z0-9]+")]
    public string Username { get; set; }

    [BindProperty]
    [Required]
    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    [MinLength(8), MaxLength(32)]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$")]
    public string Password { get; set; }

    [BindProperty]
    [Required]
    [StringLength(4)]
    public string CaptchaCode { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!captcha.Validate(CaptchaCode, HttpContext.Session))
            {
                ModelState.AddModelError(nameof(CaptchaCode), "Wrong Captcha Code");
            }

            if (ModelState.IsValid)
            {
                var uid = await mediator.Send(new ValidateLoginCommand(Username, Password));
                if (uid != Guid.Empty)
                {
                    var claims = new List<Claim>
                    {
                        new (ClaimTypes.Name, Username),
                        new (ClaimTypes.Role, "Administrator"),
                        new ("uid", uid.ToString())
                    };
                    var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var p = new ClaimsPrincipal(ci);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, p);
                    await mediator.Send(new LogSuccessLoginCommand(uid, Helper.GetClientIP(HttpContext)));


                    logger.LogInformation("Authentication success for local account \"\"{Username}\"\"", Username);

                    return RedirectToPage("/Admin/Post");
                }
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                return Page();
            }

            logger.LogWarning("Authentication failed for local account \"\"{Username}\"\"", Username);

            Response.StatusCode = StatusCodes.Status400BadRequest;
            ModelState.AddModelError(string.Empty, "Bad Request.");
            return Page();
        }
        catch (Exception e)
        {
            logger.LogWarning("Authentication failed for local account \"\"{Username}\"\"", Username);

            ModelState.AddModelError(string.Empty, e.Message);
            return Page();
        }
    }
}