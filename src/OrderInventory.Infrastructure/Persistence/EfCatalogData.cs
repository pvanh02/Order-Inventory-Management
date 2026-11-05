using System.Data;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Catalog;
using OrderInventory.Application.Common;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Inventory;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class EfCatalogData(OrderInventoryDbContext context) : ICatalogData
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfUnitOfWorkTransaction(await PersistenceErrors.ExecuteAsync(() => context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)));

    public async Task<Category?> FindCategoryAsync(int id, bool forUpdate, CancellationToken cancellationToken)
    {
        if (!forUpdate) return await context.Categories.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        RequireTransaction();
        var alreadyTracked = context.Categories.Local.Any(item => item.Id == id);
        var category = await PersistenceErrors.ExecuteAsync(() => context.Categories
            .FromSqlInterpolated($"SELECT * FROM [Categories] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync(cancellationToken));
        if (category is not null && alreadyTracked) await context.Entry(category).ReloadAsync(cancellationToken);
        return category;
    }

    public async Task<Product?> FindProductAsync(int id, bool forUpdate, CancellationToken cancellationToken)
    {
        if (!forUpdate) return await context.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        RequireTransaction();
        var alreadyTracked = context.Products.Local.Any(item => item.Id == id);
        var product = await PersistenceErrors.ExecuteAsync(() => context.Products
            .FromSqlInterpolated($"SELECT * FROM [Products] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync(cancellationToken));
        if (product is not null && alreadyTracked) await context.Entry(product).ReloadAsync(cancellationToken);
        return product;
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(item => item.Sku == sku, cancellationToken);
    public Task<bool> CategoryHasProductsAsync(int categoryId, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(item => item.CategoryId == categoryId && !item.IsDeleted, cancellationToken);
    public Task<int> ReadOnHandAsync(int productId, CancellationToken cancellationToken) =>
        context.InventoryBalances.Where(item => item.ProductId == productId).Select(item => item.OnHand).SingleAsync(cancellationToken);
    public void Add(Category category) => context.Categories.Add(category);
    public void Add(Product product) => context.Products.Add(product);
    public void Add(InventoryBalance balance) => context.InventoryBalances.Add(balance);
    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await PersistenceErrors.ExecuteAsync(() => context.SaveChangesAsync(cancellationToken), "IX_Products_Sku", "duplicate_sku", "The SKU is already reserved, including deleted products.");

    public async Task<PagedResult<CategoryDto>> ListCategoriesAsync(CategoryListQuery query, CancellationToken cancellationToken)
    {
        var rows = context.Categories.AsNoTracking().Where(item => !item.IsDeleted);
        if (query.Search is not null) rows = rows.Where(item => item.Name.Contains(query.Search));
        var total = await rows.CountAsync(cancellationToken);
        var ordered = query.Sort switch
        {
            "name" => rows.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "-name" => rows.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
            "createdAt" => rows.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            "-createdAt" => rows.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id),
            "-id" => rows.OrderByDescending(item => item.Id),
            _ => rows.OrderBy(item => item.Id)
        };
        var offset = ((long)query.Page - 1) * query.PageSize;
        var items = offset > int.MaxValue ? [] : await ordered.Skip((int)offset).Take(query.PageSize)
            .Select(item => new CategoryDto(item.Id, item.Name, item.Description, item.CreatedAt)).ToListAsync(cancellationToken);
        return new(items, query.Page, query.PageSize, total);
    }

    public async Task<PagedResult<ProductDto>> ListProductsAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        var rows = Products();
        if (query.Search is not null) rows = rows.Where(item => item.Name.Contains(query.Search) || item.Sku.Contains(query.Search));
        if (query.CategoryId.HasValue) rows = rows.Where(item => item.CategoryId == query.CategoryId);
        if (query.MinPrice.HasValue) rows = rows.Where(item => item.UnitPrice >= query.MinPrice);
        if (query.MaxPrice.HasValue) rows = rows.Where(item => item.UnitPrice <= query.MaxPrice);
        if (query.InStock.HasValue) rows = query.InStock.Value ? rows.Where(item => item.OnHand > 0) : rows.Where(item => item.OnHand == 0);
        var total = await rows.CountAsync(cancellationToken);
        var ordered = query.Sort switch
        {
            "name" => rows.OrderBy(item => item.Name).ThenBy(item => item.Id),
            "-name" => rows.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
            "sku" => rows.OrderBy(item => item.Sku).ThenBy(item => item.Id),
            "-sku" => rows.OrderByDescending(item => item.Sku).ThenBy(item => item.Id),
            "unitPrice" => rows.OrderBy(item => item.UnitPrice).ThenBy(item => item.Id),
            "-unitPrice" => rows.OrderByDescending(item => item.UnitPrice).ThenBy(item => item.Id),
            "onHand" => rows.OrderBy(item => item.OnHand).ThenBy(item => item.Id),
            "-onHand" => rows.OrderByDescending(item => item.OnHand).ThenBy(item => item.Id),
            "createdAt" => rows.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            "-createdAt" => rows.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id),
            "-id" => rows.OrderByDescending(item => item.Id),
            _ => rows.OrderBy(item => item.Id)
        };
        var offset = ((long)query.Page - 1) * query.PageSize;
        var items = offset > int.MaxValue ? [] : await Map(ordered.Skip((int)offset).Take(query.PageSize)).ToListAsync(cancellationToken);
        return new(items, query.Page, query.PageSize, total);
    }

    public Task<ProductDto?> ReadProductAsync(int id, CancellationToken cancellationToken) =>
        Map(Products().Where(item => item.Id == id)).SingleOrDefaultAsync(cancellationToken);

    private IQueryable<ProductRow> Products() =>
        from product in context.Products.AsNoTracking()
        join category in context.Categories on product.CategoryId equals category.Id
        join balance in context.InventoryBalances on product.Id equals balance.ProductId
        where !product.IsDeleted && !category.IsDeleted
        select new ProductRow
        {
            Id = product.Id, CategoryId = product.CategoryId, Sku = product.Sku, Name = product.Name,
            Description = product.Description, UnitPrice = product.UnitPrice, OnHand = balance.OnHand, CreatedAt = product.CreatedAt
        };

    private static IQueryable<ProductDto> Map(IQueryable<ProductRow> rows) => rows.Select(item =>
        new ProductDto(item.Id, item.CategoryId, item.Sku, item.Name, item.Description, item.UnitPrice, item.OnHand, item.CreatedAt));

    private sealed class ProductRow
    {
        public int Id { get; init; }
        public int CategoryId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal UnitPrice { get; init; }
        public int OnHand { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    private void RequireTransaction()
    {
        if (context.Database.CurrentTransaction is null) throw new InvalidOperationException("A catalog write requires the owner's transaction.");
    }
}
