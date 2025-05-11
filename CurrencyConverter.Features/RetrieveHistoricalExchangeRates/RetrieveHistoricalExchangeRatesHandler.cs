using System.Globalization;
using CurrencyConverter.Domain;
using CurrencyConverter.Features.RetrieveLatestExchangeRates;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace CurrencyConverter.Features.RetrieveHistoricalExchangeRates;

public sealed record RetrieveHistoricalExchangeRatesRequest(
    string BaseCurrency,
    string StartDate,
    string EndDate,
    string? ExchangeRateProviderId,
    ResponsePaginationOptions? PaginationOptions) : IRequest<ErrorOr<RetrieveHistoricalExchangeRatesResponse>>;

public sealed record ResponsePaginationOptions
    (ushort PageNumber, ushort PageCount);

public sealed record RetrieveHistoricalExchangeRatesResponse(
    string ExchangeRateProvider,
    string BaseCurrency,
    string StartDate,
    string EndDate,
    Dictionary<string, Dictionary<string, decimal>> Rates,
    ushort PageNumber,
    int PageCount);

public class RetrieveHistoricalExchangeRatesHandler(ExchangeRateProviderFactory exchangeRateProviderFactory)
    : IRequestHandler<RetrieveHistoricalExchangeRatesRequest, ErrorOr<RetrieveHistoricalExchangeRatesResponse>>
{
    private readonly RetrieveHistoricalExchangeRatesRequestValidator _requestValidator = new();
    
    public async Task<ErrorOr<RetrieveHistoricalExchangeRatesResponse>> 
        Handle(RetrieveHistoricalExchangeRatesRequest request, CancellationToken cancellationToken)
    {
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        if(_requestValidator.Validate(request) is { IsValid : false } validationResult)
            return Error.Validation(validationResult.ToString());

        var exchangeRateProvider = 
            exchangeRateProviderFactory.GetProvider(request.ExchangeRateProviderId);

        PaginatedDateSegment dateSegment;

        if (request.PaginationOptions is not null)
        {
            var errorOr = GetDateSegmentForPage(
                request.StartDate, 
                request.EndDate, 
                request.PaginationOptions);
            
            if(errorOr.IsError)
                return errorOr.FirstError;

            dateSegment = errorOr.Value;
        }
        else
        {
            dateSegment = new PaginatedDateSegment(
                1,
                1,
                request.StartDate, 
                request.EndDate);
        }
        
        
        var exchangeRateHistory = await exchangeRateProvider.GetHistoryAsync(
            request.BaseCurrency, 
            dateSegment.StartDate,
            dateSegment.EndDate,
            cancellationToken);
        
        return new RetrieveHistoricalExchangeRatesResponse(
            exchangeRateProvider.Id,
            request.BaseCurrency,
            request.StartDate,
            request.EndDate,
            Rates: exchangeRateHistory.Rates
                .ToDictionary(rate => rate.Date, rate => rate.Rates),
            dateSegment.PageNumber,
            dateSegment.PageCount);
    }

    private sealed record PaginatedDateSegment(
        ushort PageNumber, ushort PageCount, string StartDate, string EndDate);
    
    private ErrorOr<PaginatedDateSegment> GetDateSegmentForPage(
            string startDate, string endDate, ResponsePaginationOptions paginationOptions)
    {
        var endDateTime = DateTime.Parse(endDate);
        var dateTime = DateTime.Parse(startDate);
        
        var totalDaysInRange = (endDateTime - dateTime).TotalDays + 1;
        
        if(totalDaysInRange % paginationOptions.PageCount != 0)
            return Error.Validation(description:"Total days in range must be divisible by page count.");
        
        var pageSize = (ushort) Math.Max(1, totalDaysInRange / paginationOptions.PageCount);

        //if (pageSize == 0) // means that desired page count is higher than number of days in date range
        //{
        //    return new PaginatedDateSegment(1, 1, startDate, endDate);
        //}
        
        var resultsOnPageCounter = 1;
        ushort currentPageNumber = 1;
        
        var currentSegmentStartDate = string.Empty;
        
        while (dateTime <= endDateTime)
        {
            if (resultsOnPageCounter == 1)
            {
                currentSegmentStartDate = $"{dateTime:yyyy-MM-dd}";
            }
            
            if (resultsOnPageCounter == pageSize)
            {
                if(currentPageNumber == paginationOptions.PageNumber)
                    return new PaginatedDateSegment(
                        currentPageNumber,
                        paginationOptions.PageCount,
                        currentSegmentStartDate, 
                        dateTime.ToString("yyyy-MM-dd"));
                
                resultsOnPageCounter = 0;
                currentPageNumber++;
            }
            
            dateTime = dateTime.AddDays(1);
            resultsOnPageCounter++;
        }
        
        return Error.Failure(
            description:$"Page number {paginationOptions.PageNumber} is out of range for the given date range.");
    }

    private sealed class RetrieveHistoricalExchangeRatesRequestValidator
        : AbstractValidator<RetrieveHistoricalExchangeRatesRequest>
    {
        public RetrieveHistoricalExchangeRatesRequestValidator()
        {
            RuleFor(x => x.BaseCurrency)
                .NotEmpty()
                .Matches("^[A-Za-z]{3}$")
                .WithMessage("Base currency must be a three-letter currency code.");
            
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                .WithMessage("Start date must be in the format YYYY-MM-DD.");
            
            RuleFor(x => x.EndDate)
                .NotEmpty()
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                .WithMessage("End date must be in the format YYYY-MM-DD.");

            RuleFor(x => x.PaginationOptions)
                .Must(po => 
                    po is null || (po.PageNumber > 0 && po.PageCount > 1 && po.PageNumber <= po.PageCount))
                .WithMessage("Pagination options is not valid.");
            
            RuleFor(x => x)
                .Must(x => DateTime.Parse(x.StartDate) <= DateTime.Parse(x.EndDate))
                .WithMessage("Start date must be earlier than end date.");
            
        }
    }
}
