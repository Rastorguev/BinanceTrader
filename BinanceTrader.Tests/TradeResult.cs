using BinanceTrader.Tools;

namespace BinanceTrader
{
    public class TradeResult
    {
        public decimal InitialAmount { get; }
        public decimal TradeAmount { get; }
        public decimal HoldAmount { get; }
        public decimal TradeProfit { get; }
        public decimal HoldProfit { get; }
        public decimal Diff { get; }
        public decimal Afficiency { get; }
        public int CompletedCount { get; }
        public int CanceledCount { get; }

        public TradeResult(
            decimal initialAmount,
            decimal tradeAmount,
            decimal holdAmount,
            int completedCount,
            int canceledCount)
        {
            InitialAmount = initialAmount;
            TradeAmount = tradeAmount;
            HoldAmount = holdAmount;
            CompletedCount = completedCount;
            CanceledCount = canceledCount;
            TradeProfit = MathUtils.Gain(InitialAmount, TradeAmount);
            HoldProfit = MathUtils.Gain(InitialAmount, HoldAmount);
            Diff = MathUtils.Gain(HoldAmount, TradeAmount);
            Afficiency = TradeProfit - HoldProfit;
        }
    }
}