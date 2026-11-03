using System.Text.RegularExpressions;
using OrderInventory.Application.Common;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Identity;

internal static class IdentityValidation
{
    public static string UserName(string? userName)
    {
        var value = userName?.Trim();
        if (value is null || !Regex.IsMatch(value, "\\A[A-Za-z0-9][A-Za-z0-9_.-]{2,63}\\z", RegexOptions.CultureInvariant))
            throw Error("userName", "Use 3 to 64 ASCII letters, digits, dots, underscores or hyphens, starting with a letter or digit.");
        return value;
    }

    public static void Password(string? password)
    {
        if (password is null || password.Length is < 12 or > 128)
            throw Error("password", "The password must contain 12 to 128 characters.");
    }

    public static UserRole Role(string? role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return UserRole.Admin;
        if (string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase)) return UserRole.Staff;
        throw Error("role", "The role must be Admin or Staff.");
    }

    public static UserListQuery ListQuery(UserListQuery query)
    {
        if (query.Page < 1) throw Error("page", "The page must be at least 1.");
        if (query.PageSize is < 1 or > 100) throw Error("pageSize", "The page size must be between 1 and 100.");
        if (query.Search?.Length > 64) throw Error("search", "Search must not exceed 64 characters.");
        if (query.Sort is not ("id" or "-id" or "userName" or "-userName" or "createdAt" or "-createdAt"))
            throw Error("sort", "The requested sort order is invalid.");
        return query with { Search = query.Search?.Trim(), Role = query.Role is null ? null : Role(query.Role).ToString() };
    }

    public static ValidationException Error(string field, string message) => new(
        new Dictionary<string, string[]> { [field] = [message] });
}
