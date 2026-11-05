using System.Text.RegularExpressions;

namespace OrderInventory.Domain.Catalog;

public static class CatalogRules
{
    public const decimal MaximumPrice = 999999999999999999m;

    public static string Name(string? name, int maximumLength)
    {
        var value = name?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
            throw new ArgumentException("The name is required and exceeds no permitted length.", nameof(name));
        return value;
    }

    public static string? Description(string? description, int maximumLength)
    {
        var value = description?.Trim();
        if (value?.Length > maximumLength) throw new ArgumentException("The description is too long.", nameof(description));
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static string Sku(string? sku)
    {
        var value = sku?.Trim().ToUpperInvariant();
        if (value is null || !Regex.IsMatch(value, "\\A[A-Z0-9][A-Z0-9_.-]{0,63}\\z", RegexOptions.CultureInvariant))
            throw new ArgumentException("Use 1 to 64 ASCII letters, digits, dots, underscores or hyphens, starting with a letter or digit.", nameof(sku));
        return value;
    }

    public static decimal Price(decimal unitPrice)
    {
        if (unitPrice < 1 || unitPrice > MaximumPrice || decimal.Truncate(unitPrice) != unitPrice)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Use a positive whole VND price of at most 18 digits.");
        return unitPrice;
    }
}
