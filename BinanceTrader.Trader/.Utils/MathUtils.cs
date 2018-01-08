namespace BinanceTrader.Utils
{
    public static class MathUtils
    {
        public static decimal Percents(this decimal value, decimal percents) => value / 100 * percents;

        public static decimal RoundPrice(this decimal value) => decimal.Round(value, 8);

        public static decimal CalculateProfit(decimal initialAmount, decimal currentAmount)
        {
            var profit = (currentAmount - initialAmount) * 100 / initialAmount;

            return profit;
        }
    }
}