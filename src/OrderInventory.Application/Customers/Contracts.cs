namespace OrderInventory.Application.Customers;

public sealed record CustomerRequest(string Name, string? Phone = null, string? Email = null, string? Address = null);
public sealed record CustomerDto(int Id, string Name, string? Phone, string? Email, string? Address, DateTimeOffset CreatedAt);
public sealed record CustomerAuditDto(long Id, int CustomerId, int ActorId, string Action, IReadOnlyList<string> ChangedFields, DateTimeOffset OccurredAt);

public sealed record CustomerListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string Sort { get; init; } = "id";
}

public sealed record CustomerHistoryQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
