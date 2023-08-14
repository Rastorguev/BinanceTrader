using System.Collections.Generic;
using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Account
{
    public class AccountInfo
    {
        [JsonProperty("makerCommission")]
        public int MakerCommission { get; set; }

        [JsonProperty("takerCommission")]
        public int TakerCommission { get; set; }

        [JsonProperty("buyerCommission")]
        public int BuyerCommission { get; set; }

        [JsonProperty("sellerCommission")]
        public int SellerCommission { get; set; }

        [JsonProperty("canTrade")]
        public bool CanTrade { get; set; }

        [JsonProperty("canWithdraw")]
        public bool CanWithdraw { get; set; }

        [JsonProperty("canDeposit")]
        public bool CanDeposit { get; set; }

        [JsonProperty("balances")]
        [JsonConverter(typeof(ConcreteTypeConverter<IEnumerable<Balance>>))]
        public IEnumerable<IBalance> Balances { get; set; }
    }

    public class Balance : IBalance
    {
        [JsonProperty("asset")]
        public string Asset { get; set; }

        [JsonProperty("free")]
        public decimal Free { get; set; }

        [JsonProperty("locked")]
        public decimal Locked { get; set; }
    }
}