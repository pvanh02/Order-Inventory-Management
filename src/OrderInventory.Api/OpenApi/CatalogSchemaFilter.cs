using System.Globalization;
using Microsoft.OpenApi;
using OrderInventory.Application.Catalog;
using OrderInventory.Application.Inventory;
using OrderInventory.Domain.Catalog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderInventory.Api.OpenApi;

public sealed class CatalogSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema input, SchemaFilterContext context)
    {
        if (input is not OpenApiSchema schema || schema.Properties is null) return;
        if (context.Type == typeof(CategoryRequest))
        {
            Text("name", 120, true);
            Text("description", 1000);
        }
        if (context.Type == typeof(CreateProductRequest) || context.Type == typeof(UpdateProductRequest))
        {
            Text("name", 200, true);
            Text("description", 2000);
            Text("sku", 64, context.Type == typeof(CreateProductRequest));
            Number("categoryId", "1", "2147483647", true);
            Number("unitPrice", "1", CatalogRules.MaximumPrice.ToString(CultureInfo.InvariantCulture), true);
            if (Property("unitPrice") is { } price) { price.MultipleOf = 1; price.Description = "Positive whole VND; fractions are rejected without rounding."; }
            if (Property("sku") is { } sku) sku.Description = "Trimmed and uppercased; reserved after soft delete and immutable after creation.";
        }
        if (context.Type == typeof(ReceiptRequest) || context.Type == typeof(AdjustmentRequest))
        {
            Number("productId", "1", "2147483647", true);
            Text("reason", 512, context.Type == typeof(AdjustmentRequest));
            if (context.Type == typeof(ReceiptRequest)) Number("quantity", "1", "2147483647", true);
            else
            {
                Number("delta", "-2147483647", "2147483647", true);
                if (Property("delta") is { } delta) delta.Description = "Nonzero signed difference; zero is rejected. Stock must remain nonnegative.";
            }
        }

        OpenApiSchema? Property(string name) => schema.Properties.TryGetValue(name, out var property) ? property as OpenApiSchema : null;
        void Required(string name) { schema.Required ??= new HashSet<string>(StringComparer.Ordinal); schema.Required.Add(name); }
        void Text(string name, int maximum, bool required = false)
        {
            if (Property(name) is { } property) { property.MaxLength = maximum; if (required) property.MinLength = 1; }
            if (required) Required(name);
        }
        void Number(string name, string minimum, string maximum, bool required)
        {
            if (Property(name) is { } property) { property.Minimum = minimum; property.Maximum = maximum; }
            if (required) Required(name);
        }
    }
}
