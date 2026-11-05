using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Inventory;

namespace OrderInventory.Application.Catalog;

public sealed class CatalogService(ICatalogData data, TimeProvider clock)
{
    public Task<PagedResult<CategoryDto>> ListCategoriesAsync(Actor actor, CategoryListQuery query, CancellationToken cancellationToken)
    {
        CatalogValidation.Reader(actor);
        return data.ListCategoriesAsync(CatalogValidation.Query(query), cancellationToken);
    }

    public async Task<CategoryDto> GetCategoryAsync(Actor actor, int id, CancellationToken cancellationToken)
    {
        CatalogValidation.Reader(actor);
        var category = await CategoryAsync(id, false, cancellationToken);
        return Map(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(Actor actor, CategoryRequest request, CancellationToken cancellationToken)
    {
        CatalogValidation.Admin(actor);
        var category = CatalogValidation.Input(() => Category.Create(request.Name, request.Description, clock.GetUtcNow()));
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        data.Add(category);
        await data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Actor actor, int id, CategoryRequest request, CancellationToken cancellationToken)
    {
        CatalogValidation.Admin(actor);
        var cleaned = CatalogValidation.Input(() => Category.Create(request.Name, request.Description, clock.GetUtcNow()));
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        var category = await CategoryAsync(id, true, cancellationToken);
        category.Update(cleaned.Name, cleaned.Description);
        await data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(category);
    }

    public async Task DeleteCategoryAsync(Actor actor, int id, CancellationToken cancellationToken)
    {
        CatalogValidation.Admin(actor);
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        var category = await data.FindCategoryAsync(id, true, cancellationToken) ?? throw new NotFoundException("The category was not found.");
        if (category.IsDeleted) return;
        if (await data.CategoryHasProductsAsync(id, cancellationToken))
            throw new ConflictException("category_not_empty", "A category containing active products cannot be deleted.");
        category.Delete();
        await data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<PagedResult<ProductDto>> ListProductsAsync(Actor actor, ProductListQuery query, CancellationToken cancellationToken)
    {
        CatalogValidation.Reader(actor);
        return data.ListProductsAsync(CatalogValidation.Query(query), cancellationToken);
    }

    public async Task<ProductDto> GetProductAsync(Actor actor, int id, CancellationToken cancellationToken)
    {
        CatalogValidation.Reader(actor);
        return await data.ReadProductAsync(id, cancellationToken) ?? throw new NotFoundException("The product was not found.");
    }

    public async Task<ProductDto> CreateProductAsync(Actor actor, CreateProductRequest request, CancellationToken cancellationToken)
    {
        CatalogValidation.Admin(actor);
        var product = CatalogValidation.Input(() => Product.Create(request.CategoryId, request.Sku, request.Name, request.UnitPrice, request.Description, clock.GetUtcNow()));
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        await CategoryAsync(product.CategoryId, true, cancellationToken);
        if (await data.SkuExistsAsync(product.Sku, cancellationToken))
            throw new ConflictException("duplicate_sku", "The SKU is already reserved, including deleted products.");
        data.Add(product);
        await data.SaveChangesAsync(cancellationToken);
        data.Add(InventoryBalance.Create(product.Id));
        await data.SaveChangesAsync(cancellationToken);
        var result = await data.ReadProductAsync(product.Id, cancellationToken) ?? throw new InvalidOperationException("The created product is missing.");
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<ProductDto> UpdateProductAsync(Actor actor, int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        CatalogValidation.Admin(actor);
        var cleaned = CatalogValidation.Input(() => Product.Create(request.CategoryId, request.Sku ?? "VALIDATION", request.Name, request.UnitPrice, request.Description, clock.GetUtcNow()));
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        await CategoryAsync(cleaned.CategoryId, true, cancellationToken);
        var product = await data.FindProductAsync(id, true, cancellationToken);
        if (product is null || product.IsDeleted) throw new NotFoundException("The product was not found.");
        if (request.Sku is not null && cleaned.Sku != product.Sku)
            throw new ConflictException("sku_immutable", "The SKU cannot be changed after product creation.");
        product.Update(cleaned.CategoryId, cleaned.Name, cleaned.UnitPrice, cleaned.Description);
        await data.SaveChangesAsync(cancellationToken);
        var result = await data.ReadProductAsync(id, cancellationToken) ?? throw new InvalidOperationException("The updated product is missing.");
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task DeleteProductAsync(Actor actor, int id, CancellationToken cancellationToken)
    {
        CatalogValidation.Admin(actor);
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        var product = await data.FindProductAsync(id, true, cancellationToken) ?? throw new NotFoundException("The product was not found.");
        if (product.IsDeleted) return;
        if (await data.ReadOnHandAsync(id, cancellationToken) != 0)
            throw new ConflictException("stock_remaining", "A product with stock remaining cannot be deleted.");
        product.Delete();
        await data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Category> CategoryAsync(int id, bool forUpdate, CancellationToken cancellationToken)
    {
        var category = await data.FindCategoryAsync(id, forUpdate, cancellationToken);
        return category is not null && !category.IsDeleted ? category : throw new NotFoundException("The category was not found.");
    }

    private static CategoryDto Map(Category category) => new(category.Id, category.Name, category.Description, category.CreatedAt);
}
