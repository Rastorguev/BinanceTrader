using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BinanceTrader.Tools
{
    public static class MathUtils
    {
        public static decimal Percents(this decimal value, decimal percents) => value / 100 * percents;

        public static decimal Round(this decimal value) => decimal.Round(value, 8);

        public static decimal Gain(decimal initialAmount, decimal currentAmount)
        {
            var profit = (currentAmount - initialAmount) / initialAmount * 100;

            return profit;
        }

        public static decimal StandardDeviation([NotNull] IEnumerable<decimal> input)
        {
            var list = input.ToList();
            var avg = list.Average();

            return (decimal) Math.Sqrt(list.Average(v => Math.Pow((double) (v - avg), 2)));
        }
    }
}