using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Identity;
using OrderInventory.Domain.Orders;

namespace OrderInventory.Application.Orders;

public static class OrderLifecyclePolicy
{
    public static bool ShouldComplete(Actor actor, OrderLifecycleState order)
    {
        if (actor.UserId <= 0 || !Enum.IsDefined(actor.Role) || actor.Role == UserRole.Staff && actor.UserId != order.CreatedBy)
            throw new ForbiddenException();
        return order.Status switch
        {
            OrderStatus.Pending => true,
            OrderStatus.Completed => false,
            _ => throw new ConflictException("invalid_order_state", "The order cannot be completed in its current state.")
        };
    }
}
