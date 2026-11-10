using Microsoft.OpenApi;
using OrderInventory.Api.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderInventory.Api.OpenApi;

public sealed class OrderCompletionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(OrdersController) || context.MethodInfo.Name != nameof(OrdersController.Complete)) return;
        operation.Description = "Admin can complete any order; Staff can complete only orders they created (other orders return 403). Pending becomes Completed without changing stock or movements. The server records CompletedAt and CompletedBy. Repeating completion returns 200 with the original audit fields and no new mutation. Missing orders return 404; incompatible terminal states return 409. No automatic write retries: 503 commit_outcome_unknown requires reconciliation using orderId. No request body is required.";
    }
}
