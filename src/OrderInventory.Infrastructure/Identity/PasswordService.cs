using OrderInventory.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace OrderInventory.Infrastructure.Identity;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _subject = new();

    public string Hash(string password) => _hasher.HashPassword(_subject, password);

    public bool Verify(string hash, string password)
    {
        try
        {
            return _hasher.VerifyHashedPassword(_subject, hash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
