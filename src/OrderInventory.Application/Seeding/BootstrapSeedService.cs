using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Seeding;

public sealed record BootstrapAccounts(string AdminLogin, string AdminPassword, string StaffLogin, string StaffPassword);

public sealed class BootstrapSeedService(IUserStore users, ISeedHistoryStore history, IPasswordService passwords, TimeProvider clock)
{
    public async Task SeedAsync(BootstrapAccounts accounts, CancellationToken cancellationToken = default)
    {
        await using var transaction = await users.BeginTransactionAsync(cancellationToken);
        if (await history.ExistsAsync("v1", "identity", cancellationToken)) return;
        string adminLogin;
        string staffLogin;
        try
        {
            adminLogin = IdentityValidation.UserName(accounts.AdminLogin);
            staffLogin = IdentityValidation.UserName(accounts.StaffLogin);
            IdentityValidation.Password(accounts.AdminPassword);
            IdentityValidation.Password(accounts.StaffPassword);
        }
        catch (ValidationException)
        {
            throw new InvalidOperationException("Bootstrap account configuration is invalid.");
        }
        if (string.Equals(adminLogin, staffLogin, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bootstrap account configuration is invalid.");
        if (await users.FindByUserNameAsync(adminLogin.ToUpperInvariant(), cancellationToken) is not null ||
            await users.FindByUserNameAsync(staffLogin.ToUpperInvariant(), cancellationToken) is not null)
            throw new InvalidOperationException("Bootstrap names conflict with existing accounts; identity seed was not applied.");
        var now = clock.GetUtcNow();
        users.Add(UserAccount.Create(adminLogin, passwords.Hash(accounts.AdminPassword), UserRole.Admin, now));
        users.Add(UserAccount.Create(staffLogin, passwords.Hash(accounts.StaffPassword), UserRole.Staff, now));
        history.Add("v1", "identity", now);
        await users.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
