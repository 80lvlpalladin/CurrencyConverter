using CurrencyConverter.Domain;

namespace CurrencyConverter.Infrastructure;

public static class PaginationHelper
{
    public static IEnumerable<(ushort pageNumber, T[] values)> SplitIntoPages<T>
        (IReadOnlyCollection<T> values, int maxPageSize)
    {
        if (values.Count == 0)
            throw new ArgumentOutOfRangeException
                (nameof(values), "Values cannot be an empty collection.");
        
        if(maxPageSize < 1)
            throw new ArgumentOutOfRangeException
                (nameof(maxPageSize), "Page size must be greater than 0.");
        
        ushort pageCounter = 1;

        foreach (var value in values.Chunk(maxPageSize))
        {
            yield return (pageCounter++, value);
        }
    }

    public static T[] GetPage<T>(IReadOnlyCollection<T> values, PaginationOptions paginationOptions)
    {
        if (values.Count == 0)
            return [];
        
        if (paginationOptions.PageNumber == 0)
            throw new ArgumentOutOfRangeException
                (nameof(paginationOptions.PageNumber), "Page number must be greater than 0.");
        
        if (paginationOptions.MaxPageSize == 0)
            throw new ArgumentOutOfRangeException
                (nameof(paginationOptions.MaxPageSize), "Page size must be greater than 0.");

        return values
            .Skip((paginationOptions.PageNumber - 1) * paginationOptions.MaxPageSize)
            .Take(paginationOptions.MaxPageSize)
            .ToArray();
    }

}