namespace OrderInventory.Infrastructure.Persistence;

public sealed class SeedHistory
{
    public int Id { get; set; }
    public string SeedVersion { get; set; } = string.Empty;
    public string SeedUnitKey { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; set; }
}
