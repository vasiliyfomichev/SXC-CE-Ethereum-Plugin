using Nethereum.ABI.FunctionEncoding.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VF.SXC.Plugin.Ethereum.Models
{
    public class AssetPurchaseEvent
    {
        [Parameter("string", "token", 1, false)]
        public string Token { get; set; }

        [Parameter("address", "purchaser", 2, false)]
        public string Purchaser { get; set; }
        
        [Parameter("string", "assetId", 3, false)]
        public string AssetId { get; set; }
    }
}
