
namespace VF.SXC.Plugin.Ethereum.Pipelines.Blocks
{
    using Microsoft.Extensions.Logging;
    using Nethereum.Hex.HexTypes;
    using Nethereum.RPC.Eth.DTOs;
    using Nethereum.Util;
    using Nethereum.Web3;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Composer;
    using Sitecore.Commerce.Plugin.Customers;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator;
    using VF.SXC.Plugin.Ethereum.Policies;
    using VF.SXC.Plugin.Ethereum.Utilities;

    [PipelineDisplayName("Ethereum.UpdateEthBalance")]
    public class UpdateEthBalance : PipelineBlock<Customer, Customer, CommercePipelineExecutionContext>
    {
        private readonly IPersistEntityPipeline _persistEntityPipeline;

        public UpdateEthBalance(IPersistEntityPipeline persistEntityPipeline)
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
        public async override Task<Customer> Run(Customer customer, CommercePipelineExecutionContext context)
        {
            Condition.Requires(context).IsNotNull($"{this.Name}: The argument can not be null");
            Condition.Requires(customer).IsNotNull($"{this.Name}: The argument can not be null");

            //// creating identity for accounts with the "ethidentity" tag only
            //if (!customer.Tags.Any(t => t.Name.ToLower() == "ethidentity"))
            //    return customer;

            var customerDetails = customer.GetComponent<CustomerDetailsComponent>();
            var blockchainView = customer.GetComponent<ComposerTemplateViewsComponent>().Views.Where(c => c.Value.ToLower().Contains("blockchaininformation")).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(blockchainView.Key))
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Information, "Ethereum: updated customer does not have blockchain view.", context);
                return customer;
            }

            var composerView = customer.GetComposerView(blockchainView.Key);
            var ethBalanceProperty = composerView.Properties.FirstOrDefault(p => p.Name == Constants.Pipelines.Fields.EthBalanceFieldName);
            var contractIdProperty = composerView.Properties.FirstOrDefault(p => p.Name == Constants.Pipelines.Fields.IdentityContractAddressFieldName);
            if (ethBalanceProperty == null || contractIdProperty == null || string.IsNullOrWhiteSpace(contractIdProperty.RawValue?.ToString()))
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: the \"ehidentity\" tag assigned, however, either the view is not added properly, or missing contract address.", context);
                return customer;
            }

            var ethPolicy = context.GetPolicy<EthereumClientPolicy>();
            if (ethPolicy == null)
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: missing policy configuration.", context);
                return customer;
            }

            var web3 = new Web3(ethPolicy.NodeUrl);

            var idContract = web3.Eth.GetContract(ethPolicy.IdentityContractABI, contractIdProperty.RawValue?.ToString());
            var balance = await web3.Eth.GetBalance.SendRequestAsync(contractIdProperty.RawValue?.ToString());
            ethBalanceProperty.RawValue = (new UnitConversion()).FromWei(balance.Value).ToString();
            ethBalanceProperty.Value = (new UnitConversion()).FromWei(balance.Value).ToString();

            await this._persistEntityPipeline.Run(new PersistEntityArgument(customer), context);

            Logging.Log(context.GetPolicy<KnownResultCodes>().Information, $"Ethereum: updated ETH Balacnce to {balance} on {contractIdProperty.RawValue?.ToString()}.", context);
            return customer;
        }
    }
}
