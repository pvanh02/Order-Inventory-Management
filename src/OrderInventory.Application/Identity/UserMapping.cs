using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Identity;

internal static class UserMapping
{
    public static UserDto ToDto(this UserAccount user) =>
        new(user.Id, user.UserName, user.Role.ToString(), user.IsActive, user.CreatedAt);
}
