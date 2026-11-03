using OrderInventory.Application.Identity;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Abstractions;

public interface IUserStore
{
    Task<UserAccount?> FindByIdAsync(int id, CancellationToken cancellationToken);
    Task<UserAccount?> FindByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserAccount>> ListAsync(UserListQuery query, CancellationToken cancellationToken);
    Task<int> CountAsync(UserListQuery query, CancellationToken cancellationToken);
    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken);
    void Add(UserAccount user);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
