﻿namespace BinanceTrader
{
    public class TradeSessionConfig
    {
        public decimal InitialQuoteAmount { get; }
        public decimal InitialPrice { get; }
        public decimal Fee { get; }
        public decimal MinQuoteAmount { get; }
        public decimal MinProfitRatio { get; }
        public decimal MaxIdleHours { get; set; }

        public TradeSessionConfig(
            decimal initialQuoteAmount,
            decimal initialPrice,
            decimal fee,
            decimal minQuoteAmount,
            decimal minProfitRatio,
            decimal maxIdleHours
        )
        {
            InitialQuoteAmount = initialQuoteAmount;
            Fee = fee;
            MinQuoteAmount = minQuoteAmount;
            MinProfitRatio = minProfitRatio;
            InitialPrice = initialPrice;
            MaxIdleHours = maxIdleHours;
        }

        public override bool Equals(object obj) =>
            obj is TradeSessionConfig config &&
            InitialQuoteAmount == config.InitialQuoteAmount &&
            InitialPrice == config.InitialPrice &&
            Fee == config.Fee &&
            MinQuoteAmount == config.MinQuoteAmount &&
            MinProfitRatio == config.MinProfitRatio &&
            MaxIdleHours == config.MaxIdleHours;

        public override int GetHashCode()
        {
            var hashCode = -504699561;
            hashCode = hashCode * -1521134295 + InitialQuoteAmount.GetHashCode();
            hashCode = hashCode * -1521134295 + InitialPrice.GetHashCode();
            hashCode = hashCode * -1521134295 + Fee.GetHashCode();
            hashCode = hashCode * -1521134295 + MinQuoteAmount.GetHashCode();
            hashCode = hashCode * -1521134295 + MinProfitRatio.GetHashCode();
            hashCode = hashCode * -1521134295 + MaxIdleHours.GetHashCode();
            return hashCode;
        }
    }
}