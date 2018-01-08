using BinanceTrader.Utils;

namespace BinanceTrader
{
    public interface ITradingAccount
    {
        decimal InitialBaseAmount { get; }
        decimal InitialQuoteAmount { get; }
        decimal InitialPrice { get; }
        decimal CurrentBaseAmount { get; }
        decimal CurrentQuoteAmount { get; }
        decimal LastPrice { get; }

        void Buy(decimal baseAmount, decimal price);
        void Sell(decimal baseAmount, decimal price);
    }

    public class MockTradingAccount : ITradingAccount
    {
        private readonly decimal _fee;

        public MockTradingAccount(decimal initialBaseAmount, decimal initialQuoteAmount, decimal initialPrice,
            decimal fee)
        {
            CurrentBaseAmount = InitialBaseAmount = initialBaseAmount;
            CurrentQuoteAmount = InitialQuoteAmount = initialQuoteAmount;
            LastPrice = InitialPrice = initialPrice;
            _fee = fee;
        }

        public decimal InitialBaseAmount { get; }
        public decimal InitialQuoteAmount { get; }
        public decimal InitialPrice { get; }
        public decimal CurrentBaseAmount { get; private set; }
        public decimal CurrentQuoteAmount { get; private set; }
        public decimal LastPrice { get; private set; }

        public void Buy(decimal baseAmount, decimal price)
        {
            var quoteAmount = baseAmount * price;
            if (quoteAmount > CurrentQuoteAmount)
            {
                throw new InsufficientBalanceException();
            }

            CurrentQuoteAmount -= quoteAmount;
            CurrentBaseAmount += baseAmount - baseAmount.Percents(_fee);
            LastPrice = price;
        }

        public void Sell(decimal baseAmount, decimal price)
        {
            if (CurrentBaseAmount < baseAmount)
            {
                throw new InsufficientBalanceException();
            }

            var quoteAmount = baseAmount * price;
            CurrentBaseAmount -= baseAmount;
            CurrentQuoteAmount += quoteAmount - quoteAmount.Percents(_fee);
            LastPrice = price;
        }
    }
}