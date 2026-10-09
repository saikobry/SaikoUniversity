using SaikoUniversity.Application;
using SaikoUniversity.Infrastructure;
using SaikoUniversity.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options.WithTitle("Saiko University API").WithTheme(ScalarTheme.BluePlanet));
}

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    name = "Saiko University",
    api = "api/v1",
    docs = app.Environment.IsDevelopment() ? "/scalar/v1" : null,
    health = "/health"
}));

app.Run();

public partial class Program;
