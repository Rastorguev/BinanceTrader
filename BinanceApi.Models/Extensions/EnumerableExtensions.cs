using System.Collections.Generic;
using System.Linq;

namespace Binance.API.Csharp.Client.Models.Extensions
{
    public static class EnumerableExtensions
    {
        public static decimal Median(this IEnumerable<decimal> source)
        {
            var ordered = source.OrderBy(n => n).ToList();
            var count = ordered.Count();
            var halfIndex = ordered.Count() / 2;

            var median = count % 2 == 0
                ? (ordered.ElementAt(halfIndex) +
                   ordered.ElementAt(halfIndex - 1)) / 2m
                : ordered.ElementAt(halfIndex);

            return median;
        }
    }
}