using System.Collections.Generic;
using BinanceApi.Models.Account;
using Newtonsoft.Json;

namespace BinanceApi.Models.WebSocket
{
    public class AccountUpdatedMessage : IWebSocketMessage
    {
        [JsonProperty("m")]
        public int MakerCommission { get; set; }

        [JsonProperty("t")]
        public int TakerCommission { get; set; }

        [JsonProperty("b")]
        public int BuyerCommission { get; set; }

        [JsonProperty("s")]
        public int SellerCommission { get; set; }

        [JsonProperty("T")]
        public bool CanTrade { get; set; }

        [JsonProperty("W")]
        public bool CanWithdraw { get; set; }

        [JsonProperty("D")]
        public bool CanDeposit { get; set; }

        [JsonProperty("B")]
        [JsonConverter(typeof(ConcreteTypeConverter<IEnumerable<Balance>>))]
        public IEnumerable<IBalance> Balances { get; set; }

        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }
    }

    public class Balance : IBalance
    {
        [JsonProperty("a")]
        public string Asset { get; set; }

        [JsonProperty("f")]
        public decimal Free { get; set; }

        [JsonProperty("l")]
        public decimal Locked { get; set; }
    }
}