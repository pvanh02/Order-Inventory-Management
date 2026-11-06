using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Customers;

namespace OrderInventory.Application.Customers;

public sealed class CustomerService(ICustomerData data, TimeProvider clock)
{
    public Task<PagedResult<CustomerDto>> ListAsync(Actor actor, CustomerListQuery query, CancellationToken cancellationToken)
    {
        CustomerValidation.Reader(actor);
        return data.ListAsync(CustomerValidation.Query(query), cancellationToken);
    }

    public async Task<CustomerDto> GetAsync(Actor actor, int id, CancellationToken cancellationToken)
    {
        CustomerValidation.Reader(actor);
        return Map(await ActiveAsync(id, false, cancellationToken));
    }

    public async Task<CustomerDto> CreateAsync(Actor actor, CustomerRequest request, CancellationToken cancellationToken)
    {
        CustomerValidation.Reader(actor);
        var customer = CustomerValidation.Input(request, clock.GetUtcNow());
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        data.Add(customer);
        await data.SaveChangesAsync(cancellationToken);
        data.Add(CustomerAuditEntry.Create(customer.Id, actor.UserId, CustomerAuditAction.Created,
            ["name", "phone", "email", "address"], customer.CreatedAt));
        await data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerDto> UpdateAsync(Actor actor, int id, CustomerRequest request, CancellationToken cancellationToken)
    {
        CustomerValidation.Reader(actor);
        var clean = CustomerValidation.Input(request, clock.GetUtcNow());
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        var customer = await ActiveAsync(id, true, cancellationToken);
        var changed = customer.Update(clean.Name, clean.Phone, clean.Email, clean.Address);
        if (changed.Count == 0) return Map(customer);
        data.Add(CustomerAuditEntry.Create(customer.Id, actor.UserId, CustomerAuditAction.Updated, changed, clock.GetUtcNow()));
        await data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(customer);
    }

    public async Task DeleteAsync(Actor actor, int id, CancellationToken cancellationToken)
    {
        CustomerValidation.Admin(actor);
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        var customer = await data.FindAsync(id, true, cancellationToken) ?? throw Missing();
        if (customer.IsDeleted) return;
        customer.Delete();
        data.Add(CustomerAuditEntry.Create(customer.Id, actor.UserId, CustomerAuditAction.Deleted, ["isDeleted"], clock.GetUtcNow()));
        await data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<PagedResult<CustomerAuditDto>> HistoryAsync(Actor actor, int id, CustomerHistoryQuery query, CancellationToken cancellationToken)
    {
        CustomerValidation.Admin(actor);
        CustomerValidation.Paging(query.Page, query.PageSize);
        _ = await data.FindAsync(id, false, cancellationToken) ?? throw Missing();
        return await data.HistoryAsync(id, query, cancellationToken);
    }

    private async Task<Customer> ActiveAsync(int id, bool forUpdate, CancellationToken cancellationToken)
    {
        var customer = await data.FindAsync(id, forUpdate, cancellationToken);
        return customer is { IsDeleted: false } ? customer : throw Missing();
    }

    private static NotFoundException Missing() => new("The customer was not found.");
    private static CustomerDto Map(Customer customer) => new(customer.Id, customer.Name, customer.Phone, customer.Email, customer.Address, customer.CreatedAt);
}
