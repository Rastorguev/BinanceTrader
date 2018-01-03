namespace BinanceTrader.Utils
{
    public static class MathUtils
    {
        public static decimal Percents(this decimal value, decimal percents) => value / 100 * percents;

        public static decimal RoundPrice(this decimal value) => decimal.Round(value, 8);
    }


}