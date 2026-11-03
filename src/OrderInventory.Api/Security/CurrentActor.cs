using System.Globalization;
using System.Security.Claims;
using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Api.Security;

public static class CurrentActor
{
    public static Actor Get(ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue("sub"), NumberStyles.None, CultureInfo.InvariantCulture, out var id) ||
            id <= 0 || !Enum.TryParse<UserRole>(principal.FindFirstValue("role"), out var role) ||
            !Enum.IsDefined(role))
            throw new UnauthorizedException("A valid access token is required.");
        return new Actor(id, role);
    }
}
