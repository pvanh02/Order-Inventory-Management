using OrderInventory.Application.Common;
using OrderInventory.Application.Customers;
using OrderInventory.Domain.Customers;

namespace OrderInventory.Application.Abstractions;

public interface ICustomerData
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<Customer?> FindAsync(int id, bool forUpdate, CancellationToken cancellationToken);
    void Add(Customer customer);
    void Add(CustomerAuditEntry entry);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<PagedResult<CustomerDto>> ListAsync(CustomerListQuery query, CancellationToken cancellationToken);
    Task<PagedResult<CustomerAuditDto>> HistoryAsync(int customerId, CustomerHistoryQuery query, CancellationToken cancellationToken);
}
