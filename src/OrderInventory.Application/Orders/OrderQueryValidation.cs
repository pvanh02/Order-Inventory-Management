using OrderInventory.Domain.Catalog;

namespace OrderInventory.Application.Orders;

public static class OrderQueryValidation
{
    public static OrderListQuery Normalize(OrderListQuery query)
    {
        if (query.Page < 1) throw OrderPricingPolicy.Invalid("page", "Page must be at least 1.");
        if (query.PageSize is < 1 or > 100) throw OrderPricingPolicy.Invalid("pageSize", "Page size must be between 1 and 100.");
        if (!new[] { "id", "-id", "createdAt", "-createdAt", "total", "-total" }.Contains(query.Sort, StringComparer.Ordinal))
            throw OrderPricingPolicy.Invalid("sort", "The sort order is invalid.");
        foreach (var (field, id) in new[] { ("customerId", query.CustomerId), ("createdBy", query.CreatedBy), ("productId", query.ProductId) })
            if (id.HasValue && id <= 0) throw OrderPricingPolicy.Invalid(field, "Use a positive identifier.");
        var status = query.Status?.Trim();
        if (status is not null)
        {
            status = new[] { "Pending", "Completed", "Cancelled" }.FirstOrDefault(value => string.Equals(value, status, StringComparison.OrdinalIgnoreCase))
                ?? throw OrderPricingPolicy.Invalid("status", "Use Pending, Completed or Cancelled.");
        }
        var search = query.Search?.Trim();
        if (search?.Length > 200) throw OrderPricingPolicy.Invalid("search", "Search must not exceed 200 characters.");
        if (query.From.HasValue && query.To.HasValue && query.From >= query.To)
            throw OrderPricingPolicy.Invalid("to", "The interval must have from before to.");
        foreach (var total in new[] { query.MinTotal, query.MaxTotal })
            if (total.HasValue && (total < 0 || total > CatalogRules.MaximumPrice || decimal.Truncate(total.Value) != total))
                throw OrderPricingPolicy.Invalid("total", "Use whole VND bounds between 0 and 18 digits.");
        if (query.MinTotal > query.MaxTotal) throw OrderPricingPolicy.Invalid("total", "Minimum total must not exceed maximum.");
        return query with { Status = status, Search = string.IsNullOrEmpty(search) ? null : search,
            From = query.From?.ToUniversalTime(), To = query.To?.ToUniversalTime() };
    }
}
