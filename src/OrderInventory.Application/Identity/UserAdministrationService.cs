using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Identity;

public sealed class UserAdministrationService(IUserStore users, IPasswordService passwords, TimeProvider clock)
{
    public async Task<PagedResult<UserDto>> ListAsync(Actor actor, UserListQuery query, CancellationToken cancellationToken)
    {
        RequireAdmin(actor);
        var validatedQuery = IdentityValidation.ListQuery(query);
        var items = await users.ListAsync(validatedQuery, cancellationToken);
        var totalCount = await users.CountAsync(validatedQuery, cancellationToken);
        return new(items.Select(user => user.ToDto()).ToArray(), validatedQuery.Page, validatedQuery.PageSize, totalCount);
    }

    public async Task<UserDto> GetAsync(Actor actor, int id, CancellationToken cancellationToken)
    {
        RequireAdmin(actor);
        return (await FindUserAsync(id, cancellationToken)).ToDto();
    }

    public async Task<UserDto> CreateAsync(Actor actor, CreateUserRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin(actor);
        var userName = IdentityValidation.UserName(request.UserName);
        IdentityValidation.Password(request.Password);
        var role = IdentityValidation.Role(request.Role);

        await using var transaction = await users.BeginTransactionAsync(cancellationToken);
        if (await users.FindByUserNameAsync(userName.ToUpperInvariant(), cancellationToken) is not null)
            throw new ConflictException("duplicate_username", "The user name is already in use.");

        var user = UserAccount.Create(userName, passwords.Hash(request.Password), role, clock.GetUtcNow());
        users.Add(user);
        await users.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user.ToDto();
    }

    public async Task<UserDto> ChangeRoleAsync(Actor actor, int id, ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin(actor);
        var role = IdentityValidation.Role(request.Role);

        await using var transaction = await users.BeginTransactionAsync(cancellationToken);
        var user = await FindUserAsync(id, cancellationToken);
        if (user.Role == role) return user.ToDto();
        if (user.IsActive && user.Role == UserRole.Admin)
            await EnsureAnotherActiveAdminAsync(cancellationToken);

        user.ChangeRole(role);
        await users.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user.ToDto();
    }

    public async Task<UserDto> SetActiveAsync(Actor actor, int id, SetActiveRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin(actor);
        if (request.IsActive is not { } isActive)
            throw IdentityValidation.Error("isActive", "An explicit active state is required.");

        await using var transaction = await users.BeginTransactionAsync(cancellationToken);
        var user = await FindUserAsync(id, cancellationToken);
        if (user.IsActive == isActive) return user.ToDto();
        if (user.IsActive && user.Role == UserRole.Admin)
            await EnsureAnotherActiveAdminAsync(cancellationToken);

        user.SetActive(isActive);
        await users.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user.ToDto();
    }

    private static void RequireAdmin(Actor actor)
    {
        if (actor.Role != UserRole.Admin) throw new ForbiddenException();
    }

    private async Task<UserAccount> FindUserAsync(int id, CancellationToken cancellationToken) =>
        await users.FindByIdAsync(id, cancellationToken) ?? throw new NotFoundException();

    private async Task EnsureAnotherActiveAdminAsync(CancellationToken cancellationToken)
    {
        if (await users.CountActiveAdminsAsync(cancellationToken) <= 1)
            throw new ConflictException("last_active_admin", "The last active administrator cannot be demoted or disabled.");
    }
}
