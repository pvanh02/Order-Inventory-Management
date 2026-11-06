using System.ComponentModel.DataAnnotations;

namespace OrderInventory.Domain.Customers;

public static class CustomerRules
{
    public const int MaximumNameLength = 200;
    public const int MaximumPhoneLength = 32;
    public const int MaximumEmailLength = 254;
    public const int MaximumAddressLength = 1000;

    public static string Name(string? name)
    {
        var value = name?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > MaximumNameLength)
            throw new ArgumentException("Supply a name of 1 to 200 characters.", nameof(name));
        return value;
    }

    public static string? Phone(string? phone) => Optional(phone, MaximumPhoneLength, nameof(phone));
    public static string? Address(string? address) => Optional(address, MaximumAddressLength, nameof(address));
    public static string? Email(string? email)
    {
        var value = Optional(email, MaximumEmailLength, nameof(email));
        if (value is not null && !new EmailAddressAttribute().IsValid(value))
            throw new ArgumentException("Supply a valid email address.", nameof(email));
        return value;
    }

    private static string? Optional(string? input, int maximumLength, string field)
    {
        var value = input?.Trim();
        if (value?.Length > maximumLength)
            throw new ArgumentException($"The field must not exceed {maximumLength} characters.", field);
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
