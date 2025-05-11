using CurrencyConverter.Api;

var builder = DistributedApplication.CreateBuilder(args);

var exchangeRateHistoryCacheResource = builder
    .AddRedis(AspireResourceNames.ExchangeRateHistoryCache)
    .WithLifetime(ContainerLifetime.Session);

builder
    .AddProject<Projects.CurrencyConverter_Api>(AspireResourceNames.CurrencyConverterApi)
    .WithReference(exchangeRateHistoryCacheResource)
    .WaitFor(exchangeRateHistoryCacheResource);

builder.Build().Run();