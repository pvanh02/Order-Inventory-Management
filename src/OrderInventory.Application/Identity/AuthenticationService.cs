using System.Security.Cryptography;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;

namespace OrderInventory.Application.Identity;

public sealed class AuthenticationService(IUserStore users, IPasswordService passwords, ITokenIssuer tokens)
{
    private readonly string dummyHash = passwords.Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var userName = IdentityValidation.UserName(request.UserName);
        IdentityValidation.Password(request.Password);
        var user = await users.FindByUserNameAsync(userName.ToUpperInvariant(), cancellationToken);
        var validPassword = passwords.Verify(user?.PasswordHash ?? dummyHash, request.Password);
        if (user is null || !validPassword || !user.IsActive)
            throw new UnauthorizedException();

        var token = tokens.Issue(user);
        return new LoginResult(token.AccessToken, token.ExpiresAt, user.ToDto());
    }
}
