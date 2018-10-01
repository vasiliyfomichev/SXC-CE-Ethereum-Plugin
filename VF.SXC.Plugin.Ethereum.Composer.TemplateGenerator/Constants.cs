using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator
{
    public class Constants
    {
        public static class Pipelines
        {

            public const string ComposerTemplateGeneration = "VF.SXC.pipeline.ComposerTemplateGeneration";


            public static class Blocks
            {
                public const string CreateComposerTemplatesBlock = "VF.SXC.block.CreateComposerTemplatesBlock";

                public const string SamplePluginConfigureServiceApiBlock = "VF.SXC.block.SamplePluginConfigureServiceApiBlock";
            }

            public static class Fields
            {
                public const string IdentityContractAddressFieldName = "IdentityContractAddress";
                public const string PublishProductsFieldName = "PublishProducts";
                public const string EthBalanceFieldName = "ETHBalance";
            }

            public static class Views
            {
                public const string BlockchainInformationViewName = "BlockchainInformation";
                public const string BlockchainInformationViewDisplayName = "Blockchain Information";
                public const string BlockchainInformationViewId = "Composer-95DACDDD-151E-4527-B068-A27C9275967H";
            }
        }
    }
}
