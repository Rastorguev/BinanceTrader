using System.Collections.Generic;
using System.Linq;

namespace BinanceTrader.Core.Entities
{
    public class AccountInfo
    {
        public decimal MakerCommission { get; set; }

        public decimal BuyerCommission { get; set; }

        public decimal TakerCommission { get; set; }

        public decimal SellerCommission { get; set; }

        public bool CanTrade { get; set; }

        public bool CancanWithdraw { get; set; }

        public bool CanDeposit { get; set; }

        public Balances Balances { get; set; }
    }

    public class Balances : List<Balance>
    {
        public Balance GetBalanceFor(string asset)
        {
            return this.FirstOrDefault(b => b.Asset == asset);
        }
    }

    public class Balance
    {
        public string Asset { get; set; }

        public decimal Free { get; set; }

        public decimal Locked { get; set; }
    }
}