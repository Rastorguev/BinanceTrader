using System;
using System.Threading.Tasks;
using BinanceTrader.Api;
using BinanceTrader.Entities;
using BinanceTrader.Utils;

namespace BinanceTrader
{
    public class Trader
    {
        private readonly BinanceApi _binanceApi;
        private decimal? _lastOrderPrice;
        private decimal _baseAmount;
        private decimal _quoteAmount;


        public Trader(BinanceApi binanceApi)
        {
            _binanceApi = binanceApi;
        }

        public async void Trade(string baseCurrency, string quoteCurrency, decimal quoteAmount,
            decimal tolerance = 0.1m, decimal fee = 0.05m)
        {
            _quoteAmount = quoteAmount;

            while (true)
            {
                var curencyPair = string.Format($"{baseCurrency}{quoteCurrency}");
                var price = (await GetCurrencyPrice(curencyPair)).Price;

                if (_lastOrderPrice == null)
                {
                    Buy(price, fee);

                    Log(baseCurrency, quoteCurrency);
                }

                else if (_quoteAmount > 0)
                {
                    if (price + _lastOrderPrice.Value.Percents(tolerance) < _lastOrderPrice.Value)
                    {
                        Buy(price, fee);

                        Log(baseCurrency, quoteCurrency);
                    }
                }

                else if (_baseAmount > 0)
                {
                    if (price > _lastOrderPrice.Value + _lastOrderPrice.Value.Percents(tolerance))
                    {
                        Sell(price, fee);

                        Log(baseCurrency, quoteCurrency);
                    }
                }
            }
        }

        private void Sell(decimal price, decimal fee)
        {
            _quoteAmount = SubstractFee(_baseAmount * price, fee);
            _baseAmount = 0;
            _lastOrderPrice = price;
        }

        private void Buy(decimal price, decimal fee)
        {
            _baseAmount = SubstractFee(_quoteAmount / price, fee);
            _quoteAmount = 0;
            _lastOrderPrice = price;
        }

        private void Log(string baseCurrency, string quoteCurrency)
        {
            Console.WriteLine($"Price:\t\t {_lastOrderPrice:0.######} {quoteCurrency}");
            Console.WriteLine($"{baseCurrency} Amount:\t {_baseAmount:0.######}");
            Console.WriteLine($"{quoteCurrency} Amount:\t {_quoteAmount:0.######}");
            Console.WriteLine();
        }

        private async Task<CurrencyPrice> GetCurrencyPrice(string cyrrencySymbol)
        {

            var prices = await _binanceApi.GetPrices();
            var priceTicker = new PriceTicker(prices);
            var price = priceTicker.GetPrice(cyrrencySymbol);
            return price;
        }

        private decimal SubstractFee(decimal value, decimal fee)
        {
            return value - value.Percents(fee);
        }
    }
}