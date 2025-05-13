using CurrencyConverter.Api.Application.Extensions;
using CurrencyConverter.Domain;
using CurrencyConverter.Features.ConvertCurrency;
using CurrencyConverter.Features.RetrieveHistoricalExchangeRates;
using CurrencyConverter.Features.RetrieveLatestExchangeRates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Api;

public static class Endpoints
{
    private const string BaseApiUrl = "api/v1/";

    /// <summary>
    /// Configures and maps API endpoints for the application to the provided route builder.
    /// </summary>
    /// <param name="routeBuilder">
    /// The <see cref="IEndpointRouteBuilder"/> to which the endpoints will be added.
    /// </param>
    /// <returns>
    /// The <see cref="IEndpointRouteBuilder"/> with the configured API endpoints.
    /// </returns>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        var routeGroupBuilder = routeBuilder.MapGroup(BaseApiUrl);

        routeGroupBuilder.MapGet(
            "exchange-rates/latest/{baseCurrency}/{exchangeRateProviderId?}",
            RetrieveLatestExchangeRatesAsync);
        
        routeGroupBuilder.MapPost("convert", ConvertCurrencyAsync);
        
        routeGroupBuilder.MapPost("exchange-rates/history", RetrieveHistoricalExchangeRatesAsync);


        return routeBuilder;
    }


    public sealed record RetrieveHistoricalExchangeRatesWebRequest(
        string BaseCurrency,
        string StartDate,
        string EndDate,
        string? ExchangeRateProviderId,
        ushort? PageNumber, 
        ushort? MaxPageSize);
    
    
    private static Task<IResult> RetrieveHistoricalExchangeRatesAsync(
        IMediator mediator,
        [FromBody] RetrieveHistoricalExchangeRatesWebRequest webRequest,
        CancellationToken cancellationToken = default)
    {
        PaginationOptions? paginationOptions = null;
        
        if(webRequest is { PageNumber: not null, MaxPageSize: not null })
        {
            paginationOptions = new PaginationOptions
                (webRequest.PageNumber.Value, webRequest.MaxPageSize.Value);
        }
        else if(webRequest.PageNumber is null ^ webRequest.MaxPageSize is null)
        {
            return Task.FromResult(Results.BadRequest(
                $"{webRequest.PageNumber} and {webRequest.MaxPageSize} are both required"));
        }
        
        var mediatrRequest = new RetrieveHistoricalExchangeRatesRequest(
            webRequest.BaseCurrency,
            webRequest.StartDate,
            webRequest.EndDate,
            webRequest.ExchangeRateProviderId,
            paginationOptions);

        
        return mediator.SendAndReturnResultAsync(mediatrRequest, cancellationToken);
    }

    private static Task<IResult> RetrieveLatestExchangeRatesAsync(
        IMediator mediator,
        string baseCurrency,
        string? exchangeRateProviderId,
        CancellationToken cancellationToken = default)
    {
        var request = new RetrieveLatestExchangeRatesRequest(baseCurrency, exchangeRateProviderId);
        
        return mediator.SendAndReturnResultAsync(request, cancellationToken);
    }

    private sealed record ConvertCurrencyWebRequest(
        string BaseCurrency,
        string TargetCurrency,
        decimal Amount,
        string? ExchangeRateProviderId);
    
    private static Task<IResult> ConvertCurrencyAsync(
        IMediator mediator,
        [FromBody] ConvertCurrencyWebRequest webRequest,
        CancellationToken cancellationToken = default)
    {
        var mediatrRequest = new ConvertCurrencyRequest(
            webRequest.BaseCurrency, 
            webRequest.TargetCurrency, 
            webRequest.Amount, 
            webRequest.ExchangeRateProviderId);
        
        return mediator.SendAndReturnResultAsync(mediatrRequest, cancellationToken);
    }

}


