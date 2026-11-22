using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
            options.OperationFilter<JsonRequestBodyOperationFilter>();
        });
        return services;
    }
}

public class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    private readonly NullabilityInfoContext nullability = new();

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete || concrete.Properties is not { Count: > 0 })
            return;

        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!concrete.Properties.ContainsKey(name))
                continue;

            if (nullability.Create(property).ReadState == NullabilityState.NotNull)
            {
                concrete.Required ??= new HashSet<string>();
                concrete.Required.Add(name);
            }
        }
    }
}

public class JsonRequestBodyOperationFilter : IOperationFilter
{
    private const string Json = "application/json";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody is not OpenApiRequestBody body || body.Content?.ContainsKey(Json) != true)
            return;

        // ng-openapi-gen would otherwise send the body as the wildcard application/*+json.
        foreach (var contentType in body.Content.Keys.Where(k => k != Json).ToList())
            body.Content.Remove(contentType);

        body.Required = context.ApiDescription.ParameterDescriptions
            .Any(p => p.Source == BindingSource.Body && p.IsRequired);
    }
}
