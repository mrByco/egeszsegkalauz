using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace egeszsegkalauz.api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddAppSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Egészségkalauz API", Version = "v1" });

            options.CustomOperationIds(e =>
                $"{e.ActionDescriptor.RouteValues["controller"]}_{e.ActionDescriptor.RouteValues["action"]}");

            options.SupportNonNullableReferenceTypes();
            options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
        });
        return services;
    }
}

public class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    private readonly NullabilityInfoContext _nullability = new();

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete || concrete.Properties is not { Count: > 0 })
            return;

        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!concrete.Properties.ContainsKey(name))
                continue;

            if (_nullability.Create(property).ReadState == NullabilityState.NotNull)
            {
                concrete.Required ??= new HashSet<string>();
                concrete.Required.Add(name);
            }
        }
    }
}
