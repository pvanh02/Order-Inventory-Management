using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OrderInventory.Application.Abstractions;

namespace OrderInventory.Api.Security;

public static class CurrentAccountTokenValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null ||
            !int.TryParse(principal.FindFirst("sub")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ||
            !int.TryParse(principal.FindFirst("token_version")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var version))
        {
            context.Fail("Invalid account token.");
            return;
        }
        var store = context.HttpContext.RequestServices.GetRequiredService<IUserStore>();
        var account = await store.FindByIdAsync(id, context.HttpContext.RequestAborted);
        if (account is null || !account.IsActive || account.TokenVersion != version ||
            !string.Equals(account.Role.ToString(), principal.FindFirst("role")?.Value, StringComparison.Ordinal))
            context.Fail("The account token is no longer valid.");
    }
}
