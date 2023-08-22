using JetBrains.Annotations;

namespace BinanceTrader.Tools;

public static class MathUtils
{
    public static decimal Percentage(this decimal value, decimal percents)
    {
        return value / 100 * percents;
    }

    public static decimal Round8(this decimal value)
    {
        return decimal.Round(value, 8);
    }

    public static decimal Round4(this decimal value)
    {
        return decimal.Round(value, 4);
    }

    public static decimal Gain(decimal initialAmount, decimal currentAmount)
    {
        var profit = (currentAmount - initialAmount) / initialAmount * 100;

        return profit;
    }

    public static decimal StandardDeviation([NotNull] this IEnumerable<decimal> input)
    {
        var list = input.ToList();
        var avg = list.Average();

        var dispersion = list.Sum(x => Math.Pow((double)(x - avg), 2)) / list.Count;
        var standardDeviation = (decimal)Math.Sqrt(dispersion);

        return standardDeviation;
    }
}