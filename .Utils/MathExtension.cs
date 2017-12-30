namespace BinanceTrader.Utils
{
    public static class MathExtension
    {
        public static decimal Percents(this decimal value, decimal perecentage)
        {
            return value / 100 * perecentage;
        }
    }
}