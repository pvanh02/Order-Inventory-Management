namespace OrderInventory.Infrastructure.Configuration;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";
    public string AdminLogin { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string StaffLogin { get; set; } = string.Empty;
    public string StaffPassword { get; set; } = string.Empty;
}
