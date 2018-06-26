﻿namespace BinanceTrader
{
    public class TradeSessionConfig
    {
        public decimal InitialQuoteAmount { get; }
        public decimal Fee { get; }
        public decimal MinQuoteAmount { get; }
        public decimal ProfitRatio { get; }
        public decimal MaxIdleHours { get; }

        public TradeSessionConfig(
            decimal initialQuoteAmount,
            decimal fee,
            decimal minQuoteAmount,
            decimal profitRatio,
            decimal maxIdleHours
        )
        {
            InitialQuoteAmount = initialQuoteAmount;
            Fee = fee;
            MinQuoteAmount = minQuoteAmount;
            ProfitRatio = profitRatio;
            MaxIdleHours = maxIdleHours;
        }

        public override bool Equals(object obj) =>
            obj is TradeSessionConfig config &&
            InitialQuoteAmount == config.InitialQuoteAmount &&
            Fee == config.Fee &&
            MinQuoteAmount == config.MinQuoteAmount &&
            ProfitRatio == config.ProfitRatio &&
            MaxIdleHours == config.MaxIdleHours;

        public override int GetHashCode()
        {
            var hashCode = 1639983505;
            hashCode = hashCode * -1521134295 + InitialQuoteAmount.GetHashCode();
            hashCode = hashCode * -1521134295 + Fee.GetHashCode();
            hashCode = hashCode * -1521134295 + MinQuoteAmount.GetHashCode();
            hashCode = hashCode * -1521134295 + ProfitRatio.GetHashCode();
            hashCode = hashCode * -1521134295 + MaxIdleHours.GetHashCode();
            return hashCode;
        }
    }
}