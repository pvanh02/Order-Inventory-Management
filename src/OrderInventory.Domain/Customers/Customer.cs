namespace OrderInventory.Domain.Customers;

public sealed class Customer
{
    private Customer() { }
    public int Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    public static Customer Create(string? name, string? phone, string? email, string? address, DateTimeOffset createdAt) => new()
    {
        Name = CustomerRules.Name(name), Phone = CustomerRules.Phone(phone), Email = CustomerRules.Email(email),
        Address = CustomerRules.Address(address), CreatedAt = createdAt
    };

    public IReadOnlyList<string> Update(string? name, string? phone, string? email, string? address)
    {
        if (IsDeleted) throw new InvalidOperationException("A deleted customer cannot be updated.");
        // Validate every field before mutating the entity.
        var cleanName = CustomerRules.Name(name);
        var cleanPhone = CustomerRules.Phone(phone);
        var cleanEmail = CustomerRules.Email(email);
        var cleanAddress = CustomerRules.Address(address);
        List<string> changed = [];
        if (Name != cleanName) changed.Add("name");
        if (Phone != cleanPhone) changed.Add("phone");
        if (Email != cleanEmail) changed.Add("email");
        if (Address != cleanAddress) changed.Add("address");
        (Name, Phone, Email, Address) = (cleanName, cleanPhone, cleanEmail, cleanAddress);
        return changed;
    }

    public void Delete() => IsDeleted = true;
}
