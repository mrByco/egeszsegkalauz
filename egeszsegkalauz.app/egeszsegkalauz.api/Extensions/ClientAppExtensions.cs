using Microsoft.Net.Http.Headers;

namespace egeszsegkalauz.api.Extensions;

public static class ClientAppExtensions
{
    public static void MapClientApp(this WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                // Angular fingerprints every bundle name, so only index.html can go stale after a deploy.
                context.Context.Response.Headers[HeaderNames.CacheControl] =
                    context.File.Name == "index.html" ? "no-cache" : "public, max-age=31536000, immutable";
            },
        });

        app.Map("/api/{**path}", () => Results.NotFound());
        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            OnPrepareResponse = context => context.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache",
        });
    }
}
