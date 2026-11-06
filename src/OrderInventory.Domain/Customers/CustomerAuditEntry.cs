namespace OrderInventory.Domain.Customers;

public enum CustomerAuditAction { Created = 1, Updated = 2, Deleted = 3 }

public sealed class CustomerAuditEntry
{
    private CustomerAuditEntry() { }
    public long Id { get; private set; }
    public int CustomerId { get; private set; }
    public int ActorId { get; private set; }
    public CustomerAuditAction Action { get; private set; }
    public string ChangedFields { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }

    public static CustomerAuditEntry Create(int customerId, int actorId, CustomerAuditAction action,
        IReadOnlyList<string> changedFields, DateTimeOffset occurredAt)
    {
        if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));
        if (actorId <= 0) throw new ArgumentOutOfRangeException(nameof(actorId));
        if (!Enum.IsDefined(action)) throw new ArgumentOutOfRangeException(nameof(action));
        var allowed = action switch
        {
            CustomerAuditAction.Deleted => new[] { "isDeleted" },
            _ => ["name", "phone", "email", "address"]
        };
        if (changedFields.Count == 0 || changedFields.Distinct(StringComparer.Ordinal).Count() != changedFields.Count ||
            changedFields.Any(field => !allowed.Contains(field, StringComparer.Ordinal)))
            throw new ArgumentException("Audit entries contain only changed field names.", nameof(changedFields));
        return new()
        {
            CustomerId = customerId, ActorId = actorId, Action = action,
            ChangedFields = string.Join(',', changedFields), OccurredAt = occurredAt
        };
    }
}
