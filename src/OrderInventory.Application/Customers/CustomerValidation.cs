using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Customers;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Customers;

internal static class CustomerValidation
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

    public static Customer Input(CustomerRequest request, DateTimeOffset now)
    {
        try { return Customer.Create(request.Name, request.Phone, request.Email, request.Address, now); }
        catch (ArgumentException error) { throw Error(error.ParamName ?? "request", error.Message); }
    }

    public static CustomerListQuery Query(CustomerListQuery query)
    {
        Paging(query.Page, query.PageSize);
        if (!new[] { "id", "-id", "name", "-name", "createdAt", "-createdAt" }.Contains(query.Sort, StringComparer.Ordinal))
            throw Error("sort", "The sort order is invalid.");
        var search = query.Search?.Trim();
        if (search?.Length > 200) throw Error("search", "Search must not exceed 200 characters.");
        return query with { Search = string.IsNullOrEmpty(search) ? null : search };
    }

    public static void Paging(int page, int pageSize)
    {
        if (page < 1) throw Error("page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100) throw Error("pageSize", "Page size must be between 1 and 100.");
    }

    private static ValidationException Error(string field, string message) => new(new Dictionary<string, string[]> { [field] = [message] });
}
