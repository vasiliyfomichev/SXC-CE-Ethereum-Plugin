
namespace VF.SXC.Plugin.Ethereum.Pipelines.Blocks
{
    using Microsoft.Extensions.Logging;
    using Nethereum.Hex.HexTypes;
    using Nethereum.RPC.Eth.DTOs;
    using Nethereum.Util;
    using Nethereum.Web3;
    using Newtonsoft.Json;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.Composer;
    using Sitecore.Commerce.Plugin.Customers;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Numerics;
    using System.Threading;
    using System.Threading.Tasks;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator;
    using VF.SXC.Plugin.Ethereum.Models;
    using VF.SXC.Plugin.Ethereum.Policies;
    using VF.SXC.Plugin.Ethereum.Utilities;

    [PipelineDisplayName("Ethereum.UpdateEthBalanceOnEntityView")]
    public class UpdateEthBalanceOnEntityView : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly IPersistEntityPipeline _persistEntityPipeline;

        public UpdateEthBalanceOnEntityView(IPersistEntityPipeline persistEntityPipeline)
        {
            this._persistEntityPipeline = persistEntityPipeline;
        }

        /// <summary>
        /// Updates ETH balance on the customer profile from the identity contract. Customer must have an "ethidentity" tag assigned, which adds the 
        /// BlockchainInformation view providing the contract address.
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async override Task<EntityView> Run(EntityView view, CommercePipelineExecutionContext context)
        {
            Condition.Requires(context).IsNotNull($"{this.Name}: The argument can not be null");
            Condition.Requires(view).IsNotNull($"{this.Name}: The argument can not be null");
            EntityViewArgument request = context.CommerceContext.GetObject<EntityViewArgument>();
           
            if (request.Entity is Customer && request.Entity as Customer != null && request.ViewName.Equals(context.GetPolicy<KnownCustomerViewsPolicy>().Master, StringComparison.OrdinalIgnoreCase))
            {
                var customer = request.Entity as Customer;

                var customerDetails = customer.GetComponent<CustomerDetailsComponent>();
                var blockchainView = customer.GetComponent<ComposerTemplateViewsComponent>().Views.Where(c => c.Value.ToLower().Contains("blockchaininformation")).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(blockchainView.Key))
                {
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Information, "Ethereum: updated customer does not have blockchain view.", context);
                    return view;
                }

                var composerView = customer.GetComposerView(blockchainView.Key);
                var ethBalanceProperty = composerView.Properties.FirstOrDefault(p => p.Name == Constants.Pipelines.Fields.EthBalanceFieldName);
                var contractIdProperty = composerView.Properties.FirstOrDefault(p => p.Name == Constants.Pipelines.Fields.IdentityContractAddressFieldName);
                if (ethBalanceProperty == null || contractIdProperty == null || string.IsNullOrWhiteSpace(contractIdProperty.RawValue?.ToString()))
                {
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: the \"ehidentity\" tag assigned, however, either the view is not added properly, or missing contract address.", context);
                    return view;
                }

                var ethPolicy = context.GetPolicy<EthereumClientPolicy>();
                if (ethPolicy == null)
                {
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: missing policy configuration.", context);
                    return view;
                }

                var web3 = new Web3(ethPolicy.NodeUrl);

                var idContract = web3.Eth.GetContract(ethPolicy.IdentityContractABI, contractIdProperty.RawValue?.ToString());
                var balance = await web3.Eth.GetBalance.SendRequestAsync(contractIdProperty.RawValue?.ToString());

                var usdValue = await GetUsdAmountAsync(decimal.Parse((new UnitConversion()).FromWei(balance.Value).ToString()), context);
                var ethDisplayValue = $"{(new UnitConversion()).FromWei(balance.Value).ToString()} ({usdValue.ToString("C", CultureInfo.CurrentCulture)} USD)";
                ethBalanceProperty.RawValue = ethDisplayValue;
                ethBalanceProperty.Value = ethDisplayValue;

                await this._persistEntityPipeline.Run(new PersistEntityArgument(customer), context);

                Logging.Log(context.GetPolicy<KnownResultCodes>().Information, $"Ethereum: updated ETH Balacnce to {balance} on {contractIdProperty.RawValue?.ToString()}.", context);
                return view;
            }
            else
            {
                return view;
            }            
        }

        public static async Task<decimal> GetUsdAmountAsync(decimal ethAmount, CommercePipelineExecutionContext context)
        {
            try
            {
                var client = new HttpClient();
                var res = client.GetAsync("https://api.coinmarketcap.com/v1/ticker/ethereum/").Result;
                if (res.IsSuccessStatusCode)
                {
                    string result = await res.Content.ReadAsStringAsync();
                    var priceInfo = JsonConvert.DeserializeObject<EthPriceResult[]>(result);

                    var ethPrice = priceInfo.FirstOrDefault().price_usd;
                    return ethPrice * ethAmount;
                    //return 0;
                }
            }
            catch (Exception ex)
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, $"Ethereum: unable to retrieve USD price for ETH balance. {ex.Message}", context);
            }

            return 0;
        }
    }
}
