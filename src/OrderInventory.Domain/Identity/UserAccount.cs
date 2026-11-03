using System.Text.RegularExpressions;

namespace OrderInventory.Domain.Identity;

public sealed class UserAccount
{
    private UserAccount() { }

    public int Id { get; set; }
    public string UserName { get; private set; } = string.Empty;
    public string NormalizedUserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public int TokenVersion { get; private set; } = 1;
    public DateTimeOffset CreatedAt { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    public static UserAccount Create(string userName, string passwordHash, UserRole role, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        var trimmedName = userName.Trim();
        if (!Regex.IsMatch(trimmedName, "\\A[A-Za-z0-9][A-Za-z0-9_.-]{2,63}\\z", RegexOptions.CultureInvariant))
            throw new ArgumentException("The user name is invalid.", nameof(userName));
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role));

        return new UserAccount
        {
            UserName = trimmedName,
            NormalizedUserName = trimmedName.ToUpperInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = createdAt
        };
    }

    public void ChangeRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role));
        if (Role == role)
            return;
        TokenVersion = checked(TokenVersion + 1);
        Role = role;
    }

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
            return;
        TokenVersion = checked(TokenVersion + 1);
        IsActive = isActive;
    }
}
