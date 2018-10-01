
namespace VF.SXC.Plugin.Ethereum.Pipelines.Blocks
{
    using Microsoft.Extensions.Logging;
    using Nethereum.Hex.HexTypes;
    using Nethereum.RPC.Eth.DTOs;
    using Nethereum.Web3;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.Composer;
    using Sitecore.Commerce.Plugin.Customers;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator;
    using VF.SXC.Plugin.Ethereum.Policies;
    using VF.SXC.Plugin.Ethereum.Utilities;

    [PipelineDisplayName("Ethereum.CreateCustomerIdentityOnBlockchainOnCreate")]
    public class CreateCustomerIdentityOnBlockchainOnCreate : PipelineBlock<Customer, Customer, CommercePipelineExecutionContext>
    {

        public async override Task<Customer> Run(Customer customer, CommercePipelineExecutionContext context)
        {
            Condition.Requires(context).IsNotNull($"{this.Name}: The argument can not be null");
            Condition.Requires(customer).IsNotNull($"{this.Name}: The argument can not be null");

            // creating identity for accounts with the "ethidentity" tag only
            if (!customer.Tags.Any(t => t.Name.ToLower() == "Blockchain"))
            {
                customer.Tags.Add(new Tag("Blockchain"));
            }

            if (!customer.Tags.Any(t => t.Name.ToLower() == "ethidentity"))
            {
                customer.Tags.Add(new Tag("ethidentity"));
            }

            var customerDetails = customer.GetComponent<CustomerDetailsComponent>();

            var views = new ConcurrentDictionary<string, string>();
            views.TryAdd(Constants.Pipelines.Views.BlockchainInformationViewId, $"{CommerceEntity.IdPrefix<ComposerTemplate>()}{Constants.Pipelines.Views.BlockchainInformationViewName}");


            customer.Components.Add(new ComposerTemplateViewsComponent()
            {
                Views = views
            });




            var composerTemplate = customer.GetComposerView($"{CommerceEntity.IdPrefix<ComposerTemplate>()}{Constants.Pipelines.Views.BlockchainInformationViewName}");

            var blockchainView = customer.GetComponent<ComposerTemplateViewsComponent>().Views.Where(c => c.Value.ToLower().Contains("blockchaininformation")).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(blockchainView.Key))
            {
                return customer;
            }


            var ethPolicy = context.GetPolicy<EthereumClientPolicy>();
            if (ethPolicy == null)
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: missing policy configuration.", context);
                return customer;
            }

            var web3 = new Web3(ethPolicy.NodeUrl);
            // unlock merchant account.
            await Blockchain.UnlockMerchantAccountAsync(web3, context);
            var token = new CancellationTokenSource();
            try
            {
                var gasEstimate = await web3.Eth.DeployContract.EstimateGasAsync(ethPolicy.IdentityContractABI, ethPolicy.IdentityContractByteCode, ethPolicy.MerchantAccountAddress, customer.FirstName, customer.LastName);
                var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(ethPolicy.IdentityContractABI, ethPolicy.IdentityContractByteCode, ethPolicy.MerchantAccountAddress, gasEstimate, token, customer.FirstName, customer.LastName);
                var contractAddress = receipt.ContractAddress;
                Logging.Log(context.GetPolicy<KnownResultCodes>().Information, $"Ethereum: created identity for {customer.Email} at {contractAddress}.", context);

                customerDetails.View.ChildViews.Add(new EntityView()
                {
                    DisplayName = Constants.Pipelines.Views.BlockchainInformationViewDisplayName,
                    EntityId = $"{CommerceEntity.IdPrefix<ComposerTemplate>()}{Constants.Pipelines.Views.BlockchainInformationViewName}",
                    ItemId = Constants.Pipelines.Views.BlockchainInformationViewId,
                    Name = Constants.Pipelines.Views.BlockchainInformationViewName,
                    Properties = new List<ViewProperty>
                {
                    new ViewProperty()
                {
                    IsRequired = false,
                    DisplayName = "Identity Contract Address",
                    Name = Constants.Pipelines.Fields.IdentityContractAddressFieldName,
                    OriginalType = "System.String",
                    RawValue = contractAddress,
                    Value = contractAddress
                },
                    new ViewProperty()
                {
                    IsRequired = false,
                    DisplayName = "Publish Purchased Products",
                    Name = Constants.Pipelines.Fields.PublishProductsFieldName,
                    OriginalType = "System.Boolean",
                    RawValue = true,
                    Value = "true"
                },
                    new ViewProperty()
                {
                    IsRequired = false,
                    DisplayName = "ETH Balance",
                    Name = Constants.Pipelines.Fields.EthBalanceFieldName,
                    OriginalType = "System.String",
                    RawValue = string.Empty,
                    Value = string.Empty,
                    IsReadOnly = true
                }
                }
                });
            }
            catch (Exception ex)
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: unable to send transaction createIdentity. " + ex.Message, context);
                return customer;
            }

            return customer;
        }
    }
}
