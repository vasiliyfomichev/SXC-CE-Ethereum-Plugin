using Sitecore.Commerce.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VF.SXC.Plugin.Ethereum.Components
{
    public class DigitalDownloadBlockchainTokenComponent : Component
    {
        public DigitalDownloadBlockchainTokenComponent()
        {
            Name = "BlockchainDownloadToken";
        }
        public string BlockchainDownloadToken { get; set; }
    }
}
