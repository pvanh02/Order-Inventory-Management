using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Application.Identity;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthenticationService authentication) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResult>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(await authentication.LoginAsync(request, cancellationToken));
    }
}
