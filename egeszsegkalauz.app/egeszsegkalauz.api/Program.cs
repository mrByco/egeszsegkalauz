using System.Text.Json.Serialization;
using egeszsegkalauz.api.Auth;
using egeszsegkalauz.api.Extensions;
using egeszsegkalauz.api.Workers;
using egeszsegkalauz.services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Otherwise Swagger also lists text/plain and text/json, and ng-openapi-gen duplicates every function.
builder.Services.AddControllers(options => options.Filters.Add(new ProducesAttribute("application/json")))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAppSwagger();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ServiceExceptionHandler>();

await using var devDatabase = await builder.AddAppDatabaseAsync();
builder.Services.AddAppServices();
builder.Services.AddAppAuthentication();
builder.Services.AddHostedService<DailyImportWorker>();
builder.Services.AddHostedService<SourceSyncWorker>();

var app = builder.Build();

await app.InitializeDatabaseAsync(seedDevData: devDatabase is not null);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAppAuthentication();
app.MapControllers();

// In development `ng serve` hosts the client and proxies /api here.
if (!app.Environment.IsDevelopment())
    app.MapClientApp();

app.Run();
