using Microsoft.OpenApi;
using OrderInventory.Application.Customers;
using OrderInventory.Domain.Customers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderInventory.Api.OpenApi;

public sealed class CustomerSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema input, SchemaFilterContext context)
    {
        if (context.Type != typeof(CustomerRequest) || input is not OpenApiSchema schema || schema.Properties is null) return;
        schema.Description = "Trim all fields; blank optional values become null. Phone/email need not be unique. PUT replaces all profile fields.";
        schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
        schema.Required.Add("name");
        Text("name", CustomerRules.MaximumNameLength, true);
        Text("phone", CustomerRules.MaximumPhoneLength);
        Text("email", CustomerRules.MaximumEmailLength);
        Text("address", CustomerRules.MaximumAddressLength);
        if (schema.Properties.TryGetValue("email", out var email) && email is OpenApiSchema emailSchema)
            emailSchema.Description = "Optional; nonblank values must be a valid email address.";

        void Text(string name, int maximum, bool required = false)
        {
            if (schema.Properties.TryGetValue(name, out var inputProperty) && inputProperty is OpenApiSchema property)
            {
                property.MaxLength = maximum;
                if (required) property.MinLength = 1;
            }
        }
    }
}
