using Newtonsoft.Json;

namespace BinanceApi.Models.Market
{
    public class SymbolPrice
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}