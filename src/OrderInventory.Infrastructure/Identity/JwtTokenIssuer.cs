using Microsoft.Extensions.Options;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Identity;
using OrderInventory.Infrastructure.Configuration;

namespace OrderInventory.Infrastructure.Identity;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : ITokenIssuer
{
    public TokenResult Issue(UserAccount user)
    {
        var settings = options.Value;
        if (!JwtOptions.IsValid(settings))
            throw new OptionsValidationException(JwtOptions.SectionName, typeof(JwtOptions), ["JWT configuration is invalid."]);
        var now = clock.GetUtcNow();
        var expiry = now.AddMinutes(settings.AccessTokenMinutes);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim("role", user.Role.ToString()),
                new Claim("token_version", user.TokenVersion.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ],
            notBefore: now.UtcDateTime,
            expires: expiry.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(settings.SigningKeyBase64)),
                SecurityAlgorithms.HmacSha256));
        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }
}
