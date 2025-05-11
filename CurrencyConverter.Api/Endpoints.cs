using CurrencyConverter.Api.Extensions;
using CurrencyConverter.Features.ConvertCurrency;
using CurrencyConverter.Features.RetrieveHistoricalExchangeRates;
using CurrencyConverter.Features.RetrieveLatestExchangeRates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Api;

public static class Endpoints
{
    private const string BaseApiUrl = "api/v1/"; //TODO implement versioning

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
        WebResponsePaginationOptions? PaginationOptions);
    
    public sealed record WebResponsePaginationOptions(ushort PageNumber, ushort PageCount);
    
    private static Task<IResult> RetrieveHistoricalExchangeRatesAsync(
        IMediator mediator,
        [FromBody] RetrieveHistoricalExchangeRatesWebRequest webRequest,
        CancellationToken cancellationToken = default)
    {
        var mediatrResponsePaginationOptions = webRequest.PaginationOptions is null ?
            null :
            new ResponsePaginationOptions(
                webRequest.PaginationOptions.PageNumber, 
                webRequest.PaginationOptions.PageCount);
        
        var mediatrRequest = new RetrieveHistoricalExchangeRatesRequest(
            webRequest.BaseCurrency,
            webRequest.StartDate,
            webRequest.EndDate,
            webRequest.ExchangeRateProviderId,
            mediatrResponsePaginationOptions);

        
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


