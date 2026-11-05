using OrderInventory.Application.Catalog;
using OrderInventory.Application.Common;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Inventory;

namespace OrderInventory.Application.Abstractions;

public interface ICatalogData
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<Category?> FindCategoryAsync(int id, bool forUpdate, CancellationToken cancellationToken);
    Task<Product?> FindProductAsync(int id, bool forUpdate, CancellationToken cancellationToken);
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken);
    Task<bool> CategoryHasProductsAsync(int categoryId, CancellationToken cancellationToken);
    Task<int> ReadOnHandAsync(int productId, CancellationToken cancellationToken);
    void Add(Category category);
    void Add(Product product);
    void Add(InventoryBalance balance);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<PagedResult<CategoryDto>> ListCategoriesAsync(CategoryListQuery query, CancellationToken cancellationToken);
    Task<PagedResult<ProductDto>> ListProductsAsync(ProductListQuery query, CancellationToken cancellationToken);
    Task<ProductDto?> ReadProductAsync(int id, CancellationToken cancellationToken);
}
