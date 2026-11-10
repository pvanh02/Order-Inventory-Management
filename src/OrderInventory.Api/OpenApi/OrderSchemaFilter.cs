using Microsoft.OpenApi;
using OrderInventory.Application.Orders;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderInventory.Api.OpenApi;

public sealed class OrderSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema input, SchemaFilterContext context)
    {
        if (input is not OpenApiSchema schema || schema.Properties is null) return;
        if (context.Type == typeof(CancelOrderRequest))
        {
            schema.Description = "Admin can cancel any Pending order; Staff only their own. Returns original line quantities once, including deleted products, in the cancellation transaction. Optional trimmed reason, maximum 512 characters. Repeat returns original audit/reason without mutation; Completed conflicts with 409. Unknown commit requires reconciliation before resubmission.";
            if (schema.Properties["reason"] is OpenApiSchema reason) reason.MaxLength = 512;
        }
        if (context.Type == typeof(CreateOrderRequest))
        {
            schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
            schema.Required.UnionWith(["customerId", "lines"]);
            schema.Description = "Customer is required and must be active. Duplicate product lines are merged. Prices, snapshots, actor and Pending state are server controlled. Amounts are whole VND, at most 18 digits per line and order. No automatic write retries or idempotency guarantee. A 503 commit_outcome_unknown requires reconciliation using orderId before resubmission.";
            if (schema.Properties["customerId"] is OpenApiSchema customer) customer.Minimum = "1";
            if (schema.Properties["lines"] is OpenApiSchema lines) { lines.MinItems = 1; lines.MaxItems = 100; }
        }
        if (context.Type == typeof(OrderLineRequest))
        {
            schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
            schema.Required.UnionWith(["productId", "quantity"]);
            foreach (var name in new[] { "productId", "quantity" })
                if (schema.Properties[name] is OpenApiSchema property) { property.Minimum = "1"; property.Maximum = "2147483647"; }
        }
    }
}
