namespace BinanceTrader
{
    public class TradeSessionConfig
    {
        public decimal InitialQuoteAmount { get; }
        public decimal InitialPrice { get; }
        public decimal Fee { get; }
        public decimal MinQuoteAmount { get; }
        public decimal MinProfitRatio { get; }

        public TradeSessionConfig(
            decimal initialQuoteAmount,
            decimal initialPrice,
            decimal fee,
            decimal minQuoteAmount,
            decimal minProfitRatio)
        {
            InitialQuoteAmount = initialQuoteAmount;
            Fee = fee;
            MinQuoteAmount = minQuoteAmount;
            MinProfitRatio = minProfitRatio;
            InitialPrice = initialPrice;
        }
    }
}