using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace MoongladePure.Web.Controllers;

[Route("auth")]
public class AuthController : ControllerBase
{


    [HttpGet("signout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> SignOut(int nounce = 1055)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }

    [AllowAnonymous]
    [HttpGet("accessdenied")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AccessDenied() => Forbid();
}