using System.Data;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;
using OrderInventory.Application.Customers;
using OrderInventory.Domain.Customers;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class EfCustomerData(OrderInventoryDbContext context) : ICustomerData
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfUnitOfWorkTransaction(await PersistenceErrors.ExecuteAsync(() => context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)));

    public async Task<Customer?> FindAsync(int id, bool forUpdate, CancellationToken cancellationToken)
    {
        if (!forUpdate) return await context.Customers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        RequireTransaction();
        var alreadyTracked = context.Customers.Local.Any(item => item.Id == id);
        var customer = await PersistenceErrors.ExecuteAsync(() => context.Customers
            .FromSqlInterpolated($"SELECT * FROM [Customers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync(cancellationToken));
        if (customer is not null && alreadyTracked) await context.Entry(customer).ReloadAsync(cancellationToken);
        return customer;
    }

    public void Add(Customer customer) { RequireTransaction(); context.Customers.Add(customer); }
    public void Add(CustomerAuditEntry entry) { RequireTransaction(); context.CustomerAuditEntries.Add(entry); }
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        RequireTransaction();
        await PersistenceErrors.ExecuteAsync(() => context.SaveChangesAsync(cancellationToken));
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(CustomerListQuery query, CancellationToken cancellationToken)
    {
        var rows = context.Customers.AsNoTracking().Where(item => !item.IsDeleted);
        if (query.Search is not null)
            rows = rows.Where(item => item.Name.Contains(query.Search) ||
                item.Phone != null && item.Phone.Contains(query.Search) || item.Email != null && item.Email.Contains(query.Search));
        var total = await rows.CountAsync(cancellationToken);
        var ordered = query.Sort switch
        {
            "name" => rows.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "-name" => rows.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
            "createdAt" => rows.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            "-createdAt" => rows.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id),
            "-id" => rows.OrderByDescending(item => item.Id),
            _ => rows.OrderBy(item => item.Id)
        };
        var offset = ((long)query.Page - 1) * query.PageSize;
        var items = offset > int.MaxValue ? [] : await ordered.Skip((int)offset).Take(query.PageSize)
            .Select(item => new CustomerDto(item.Id, item.Name, item.Phone, item.Email, item.Address, item.CreatedAt)).ToListAsync(cancellationToken);
        return new(items, query.Page, query.PageSize, total);
    }

    public async Task<PagedResult<CustomerAuditDto>> HistoryAsync(int customerId, CustomerHistoryQuery query, CancellationToken cancellationToken)
    {
        var rows = context.CustomerAuditEntries.AsNoTracking().Where(item => item.CustomerId == customerId);
        var total = await rows.CountAsync(cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;
        var items = offset > int.MaxValue ? [] : await rows.OrderByDescending(item => item.OccurredAt).ThenByDescending(item => item.Id)
            .Skip((int)offset).Take(query.PageSize).ToListAsync(cancellationToken);
        return new(items.Select(item => new CustomerAuditDto(item.Id, item.CustomerId, item.ActorId, item.Action.ToString(),
            item.ChangedFields.Split(','), item.OccurredAt)).ToArray(), query.Page, query.PageSize, total);
    }

    private void RequireTransaction()
    {
        if (context.Database.CurrentTransaction is null) throw new InvalidOperationException("A customer write requires the owner's transaction.");
    }
}
