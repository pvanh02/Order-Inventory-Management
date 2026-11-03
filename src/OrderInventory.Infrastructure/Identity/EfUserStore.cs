using System.Data;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Identity;
using OrderInventory.Infrastructure.Persistence;

namespace OrderInventory.Infrastructure.Identity;

public sealed class EfUserStore(OrderInventoryDbContext context) : IUserStore
{
    public Task<UserAccount?> FindByIdAsync(int id, CancellationToken cancellationToken) =>
        PersistenceErrors.ExecuteAsync(async () =>
        {
            var tracked = context.UserAccounts.Local.SingleOrDefault(user => user.Id == id);
            if (tracked is null)
                return await context.UserAccounts.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

            // Authentication may have loaded this account before the service's transaction began.
            var entry = context.Entry(tracked);
            await entry.ReloadAsync(cancellationToken);
            return entry.State == EntityState.Detached ? null : tracked;
        });
    public Task<UserAccount?> FindByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        PersistenceErrors.ExecuteAsync(() => context.UserAccounts.SingleOrDefaultAsync(
            user => user.NormalizedUserName == normalizedUserName, cancellationToken));
    public async Task<IReadOnlyList<UserAccount>> ListAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset > int.MaxValue) return [];
        return await PersistenceErrors.ExecuteAsync(() => Sort(Filter(query).AsNoTracking(), query.Sort)
            .Skip((int)offset).Take(query.PageSize).ToListAsync(cancellationToken));
    }
    public Task<int> CountAsync(UserListQuery query, CancellationToken cancellationToken) =>
        PersistenceErrors.ExecuteAsync(() => Filter(query).CountAsync(cancellationToken));
    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken) =>
        PersistenceErrors.ExecuteAsync(() => context.UserAccounts.CountAsync(
            user => user.Role == UserRole.Admin && user.IsActive, cancellationToken));
    public void Add(UserAccount user) => context.UserAccounts.Add(user);
    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await PersistenceErrors.ExecuteAsync(() => context.SaveChangesAsync(cancellationToken));
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfUnitOfWorkTransaction(await PersistenceErrors.ExecuteAsync(() =>
            context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)));

    private IQueryable<UserAccount> Filter(UserListQuery query)
    {
        IQueryable<UserAccount> users = context.UserAccounts;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var normalizedSearch = query.Search.Trim().ToUpperInvariant();
            users = users.Where(user => user.NormalizedUserName.Contains(normalizedSearch));
        }
        if (query.Role is not null)
        {
            var role = Enum.Parse<UserRole>(query.Role, ignoreCase: false);
            users = users.Where(user => user.Role == role);
        }
        if (query.IsActive.HasValue)
            users = users.Where(user => user.IsActive == query.IsActive.Value);
        return users;
    }

    private static IOrderedQueryable<UserAccount> Sort(IQueryable<UserAccount> users, string sort) => sort switch
    {
        "-id" => users.OrderByDescending(user => user.Id),
        "userName" => users.OrderBy(user => user.UserName).ThenBy(user => user.Id),
        "-userName" => users.OrderByDescending(user => user.UserName).ThenBy(user => user.Id),
        "createdAt" => users.OrderBy(user => user.CreatedAt).ThenBy(user => user.Id),
        "-createdAt" => users.OrderByDescending(user => user.CreatedAt).ThenBy(user => user.Id),
        "id" => users.OrderBy(user => user.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sort))
    };
}
