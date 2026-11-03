using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Api.Security;
using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class UsersController(UserAdministrationService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> List([FromQuery] UserListQuery query, CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(CurrentActor.Get(User), query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await users.GetAsync(CurrentActor.Get(User), id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(CurrentActor.Get(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPatch("{id:int}/role")]
    public async Task<ActionResult<UserDto>> ChangeRole(int id, ChangeRoleRequest request, CancellationToken cancellationToken) =>
        Ok(await users.ChangeRoleAsync(CurrentActor.Get(User), id, request, cancellationToken));

    [HttpPatch("{id:int}/active")]
    public async Task<ActionResult<UserDto>> SetActive(int id, SetActiveRequest request, CancellationToken cancellationToken) =>
        Ok(await users.SetActiveAsync(CurrentActor.Get(User), id, request, cancellationToken));
}
