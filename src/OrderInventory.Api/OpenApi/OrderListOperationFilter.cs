using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using OrderInventory.Api.Controllers;

namespace OrderInventory.Api.OpenApi;

public sealed class OrderListOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(OrdersController) || context.MethodInfo.Name != nameof(OrdersController.List)) return;
        operation.Description = "Admin and Staff can list all orders. Filters and count execute in SQL before paging. Search matches retained SKU/name snapshots, including soft-deleted products/customers. Time bounds are CreatedAt UTC [from,to). Money filters are whole VND; status supports Pending and Completed. Summaries exclude Customer contact data; use detail for line snapshots. Equal sort values use ascending order ID as a tie-breaker.";
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter.Schema is not OpenApiSchema schema) continue;
            switch (parameter.Name?.ToLowerInvariant())
            {
                case "page": schema.Minimum = "1"; parameter.Description = "Default 1."; break;
                case "pagesize": schema.Minimum = "1"; schema.Maximum = "100"; parameter.Description = "Default 20, maximum 100."; break;
                case "sort": parameter.Description = "id, -id, createdAt, -createdAt, total, -total; default -createdAt."; break;
                case "search": schema.MaxLength = 200; break;
                case "mintotal": case "maxtotal": schema.Minimum = "0"; schema.Maximum = "999999999999999999"; schema.MultipleOf = 1; break;
                case "customerid": case "createdby": case "productid": schema.Minimum = "1"; break;
                case "from": parameter.Description = "Inclusive CreatedAt instant; offsets normalized to UTC."; break;
                case "to": parameter.Description = "Exclusive CreatedAt instant; must be after from."; break;
                case "status": parameter.Description = "Pending or Completed (case-insensitive)."; break;
            }
        }
    }
}
