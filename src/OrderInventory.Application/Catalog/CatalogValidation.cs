using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Application.Inventory;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Catalog;

internal static class CatalogValidation
{
    public static void Reader(Actor actor)
    {
        if (actor.UserId <= 0 || !Enum.IsDefined(actor.Role)) throw new ForbiddenException();
    }

    public static void Admin(Actor actor)
    {
        Reader(actor);
        if (actor.Role != UserRole.Admin) throw new ForbiddenException();
    }

    public static T Input<T>(Func<T> validate)
    {
        try { return validate(); }
        catch (ArgumentException error) { throw Error(error.ParamName ?? "request", error.Message); }
    }

    public static void Paging(int page, int pageSize, string sort, params string[] allowedSorts)
    {
        if (page < 1) throw Error("page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100) throw Error("pageSize", "Page size must be between 1 and 100.");
        if (!allowedSorts.Contains(sort, StringComparer.Ordinal)) throw Error("sort", "The sort order is invalid.");
    }

    public static void Id(int value, string field)
    {
        if (value <= 0) throw Error(field, "The identifier must be positive.");
    }

    public static string? Search(string? search)
    {
        if (search?.Length > 200) throw Error("search", "Search must not exceed 200 characters.");
        return string.IsNullOrWhiteSpace(search) ? null : search.Trim();
    }

    public static CategoryListQuery Query(CategoryListQuery query)
    {
        Paging(query.Page, query.PageSize, query.Sort, "id", "-id", "name", "-name", "createdAt", "-createdAt");
        return query with { Search = Search(query.Search) };
    }

    public static ProductListQuery Query(ProductListQuery query)
    {
        Paging(query.Page, query.PageSize, query.Sort, "id", "-id", "name", "-name", "sku", "-sku", "unitPrice", "-unitPrice", "onHand", "-onHand", "createdAt", "-createdAt");
        if (query.CategoryId.HasValue) Id(query.CategoryId.Value, "categoryId");
        foreach (var price in new[] { query.MinPrice, query.MaxPrice })
            if (price.HasValue && (price < 0 || price > CatalogRules.MaximumPrice || decimal.Truncate(price.Value) != price))
                throw Error("price", "Price filters must be whole VND values within the supported range.");
        if (query.MinPrice > query.MaxPrice) throw Error("price", "The minimum price must not exceed the maximum.");
        return query with { Search = Search(query.Search) };
    }

    public static InventoryListQuery Query(InventoryListQuery query)
    {
        Paging(query.Page, query.PageSize, query.Sort, "id", "-id", "sku", "-sku", "name", "-name", "onHand", "-onHand");
        if (query.ProductId.HasValue) Id(query.ProductId.Value, "productId");
        if (query.CategoryId.HasValue) Id(query.CategoryId.Value, "categoryId");
        return query with { Search = Search(query.Search) };
    }

    public static MovementListQuery Query(MovementListQuery query)
    {
        Paging(query.Page, query.PageSize, query.Sort, "id", "-id", "occurredAt", "-occurredAt");
        if (query.ProductId.HasValue) Id(query.ProductId.Value, "productId");
        if (query.ActorId.HasValue) Id(query.ActorId.Value, "actorId");
        var type = query.Type;
        if (type is not null)
        {
            if (type.Equals("Receipt", StringComparison.OrdinalIgnoreCase)) type = "Receipt";
            else if (type.Equals("Adjustment", StringComparison.OrdinalIgnoreCase)) type = "Adjustment";
            else if (type.Equals("Sale", StringComparison.OrdinalIgnoreCase)) type = "Sale";
            else if (type.Equals("CancellationReturn", StringComparison.OrdinalIgnoreCase)) type = "CancellationReturn";
            else throw Error("type", "Use Receipt, Adjustment, Sale or CancellationReturn.");
        }
        if (query.From.HasValue && query.To.HasValue && query.From >= query.To)
            throw Error("to", "The interval must have from before to.");
        return query with { Type = type, From = query.From?.ToUniversalTime(), To = query.To?.ToUniversalTime() };
    }

    public static string? Reason(string? reason, bool required)
    {
        var value = reason?.Trim();
        if (value?.Length > 512 || required && string.IsNullOrWhiteSpace(value))
            throw Error("reason", "Supply a reason of 1 to 512 characters for adjustments.");
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static ValidationException Error(string field, string message) => new(new Dictionary<string, string[]> { [field] = [message] });
}
