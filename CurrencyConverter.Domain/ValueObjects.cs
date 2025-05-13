namespace CurrencyConverter.Domain;

public sealed record ExchangeRate(
    string Date, 
    Dictionary<string, decimal> Rates);

public sealed record ConversionResult(
    string BaseCurrency,
    string TargetCurrency,
    decimal Value);

public sealed record ExchangeRateHistory(
    string StartDate, 
    string EndDate, 
    IReadOnlyCollection<ExchangeRate> Rates,
    PaginationInfo PaginationInfo);

public sealed record PaginationOptions(
    ushort PageNumber, 
    ushort MaxPageSize);

public sealed record PaginationInfo(
    ushort CurrentPageNumber,
    ushort CurrentPageSize,
    ushort PageCountTotal);


    