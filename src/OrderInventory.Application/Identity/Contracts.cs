using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Identity;

public sealed record UserDto(int Id, string UserName, string Role, bool IsActive, DateTimeOffset CreatedAt);
public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAt);
public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAt, UserDto User, string TokenType = "Bearer");
public sealed record LoginRequest(string UserName, string Password);
public sealed record CreateUserRequest(string UserName, string Password, string Role);
public sealed record ChangeRoleRequest(string Role);
public sealed record SetActiveRequest(bool? IsActive);
public sealed record Actor(int UserId, UserRole Role);

public sealed record UserListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Role { get; init; }
    public bool? IsActive { get; init; }
    public string Sort { get; init; } = "id";
}
