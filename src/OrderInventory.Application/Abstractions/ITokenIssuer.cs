using OrderInventory.Application.Identity;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Application.Abstractions;

public interface ITokenIssuer
{
    TokenResult Issue(UserAccount user);
}
