using System.Collections.Generic;
using System.Linq;

namespace BinanceTrader.Entities
{
    public class PriceTicker
    {
        public PriceTicker(List<CurrencyPrice> prices)
        {
            Prices = prices;
        }

        public List<CurrencyPrice> Prices { get; }

        public CurrencyPrice GetPrice(string symbol)
        {
            return Prices.First(p => p.Symbol == symbol);
        }
    }

    public class CurrencyPrice
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
    }
}