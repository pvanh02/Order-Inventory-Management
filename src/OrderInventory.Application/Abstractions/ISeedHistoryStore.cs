namespace OrderInventory.Application.Abstractions;

public interface ISeedHistoryStore
{
    Task<bool> ExistsAsync(string seedVersion, string seedUnitKey, CancellationToken cancellationToken);
    void Add(string seedVersion, string seedUnitKey, DateTimeOffset completedAt);
}
