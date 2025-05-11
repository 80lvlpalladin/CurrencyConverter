using System.Reflection;
using CurrencyConverter.Domain;
using CurrencyConverter.Features.ConvertCurrency;
using CurrencyConverter.Features.RetrieveLatestExchangeRates;
using CurrencyConverter.Infrastructure;
using CurrencyConverter.Infrastructure.Clients;
using CurrencyConverter.Infrastructure.ExchangeRateProviders;
using Refit;

namespace CurrencyConverter.Api;

public static class ServiceRegistrant
{
    public static IServiceCollection AddDomainLayer
        (this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSingleton<ExchangeRateProviderFactory>();
    }
    
    public static IServiceCollection AddInfrastructureLayer
        (this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddFrankfurterApiExchangeRateProvider(configuration);
    }
    
    public static IServiceCollection AddFeaturesLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsSection =
            configuration.GetRequiredSection(ConvertCurrencyOptions.SectionName);
        var featuresAssembly = 
            Assembly.GetAssembly(typeof(RetrieveLatestExchangeRatesHandler));
        
        return services
            .AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(featuresAssembly!))
            .Configure<ConvertCurrencyOptions>(optionsSection);
    }

    private static IServiceCollection AddFrankfurterApiExchangeRateProvider
        (this IServiceCollection services, IConfiguration configuration)
    {
        var optionsSection =
            configuration.GetRequiredSection(FrankfurterApiOptions.SectionName);
        
        services
            .AddRefitClient<IFrankfurterApiClient>()
            .ConfigureHttpClient(client => 
                client.BaseAddress = new Uri(optionsSection.Get<FrankfurterApiOptions>()!.BaseUrl));
        
        var historyCacheConnectionString =
            configuration.GetConnectionString(AspireResourceNames.ExchangeRateHistoryCache);
        
        if(historyCacheConnectionString is null) throw new InvalidOperationException(
                "Aspire connection string is not found: " + AspireResourceNames.ExchangeRateHistoryCache);

        return services
            .Configure<FrankfurterApiOptions>(optionsSection)
            .AddSingleton<RedisStorage>(_ => new RedisStorage(historyCacheConnectionString))
            .AddSingleton<IExchangeRateProvider, FrankfurterExchangeRateProvider>();
    }
}