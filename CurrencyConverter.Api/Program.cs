using CurrencyConverter.Api;
using CurrencyConverter.Api.Extensions;
using Scalar.AspNetCore;

var currentEnvironment = new CurrentEnvironment();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = currentEnvironment.EnvironmentName,
});

builder
    .AddConfiguration(currentEnvironment)
    .AddConcurrencyRateLimiter()
    .AddTelemetry();

builder.Services
    .AddSingleton(currentEnvironment)
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddInfrastructureLayer(builder.Configuration)
    .AddDomainLayer(builder.Configuration)
    .AddFeaturesLayer(builder.Configuration);
    
if(currentEnvironment.IsDevelopmentOrTesting())    
    builder.Services.AddOpenApi();

var app = builder.Build();

if (currentEnvironment.IsDevelopmentOrTesting())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapEndpoints();

app.UseHttpsRedirection();

app.Run();