namespace VF.SXC.Plugin.Ethereum.Pipelines.Blocks
{
    using Microsoft.Extensions.Logging;
    using Nethereum.Hex.HexTypes;
    using Nethereum.RPC.Eth.DTOs;
    using Nethereum.Web3;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.Carts;
    using Sitecore.Commerce.Plugin.Customers;
    using Sitecore.Commerce.Plugin.Orders;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using VF.SXC.Plugin.Ethereum.Components;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator;
    using VF.SXC.Plugin.Ethereum.Models;
    using VF.SXC.Plugin.Ethereum.Policies;
    using VF.SXC.Plugin.Ethereum.Utilities;

    [PipelineDisplayName("Ethereum.GetBlockchainDownloadToken")]
    public class SetBlockchainDownloadToken : PipelineBlock<Order, Order, CommercePipelineExecutionContext>
    {

        private readonly IGetCustomerPipeline _getCustomerPipeline;
        private readonly IServiceProvider _serviceProvider;
        public SetBlockchainDownloadToken(IGetCustomerPipeline getCustomerPipeline, IServiceProvider serviceProvider)
        {
            this._getCustomerPipeline = getCustomerPipeline;
            this._serviceProvider = serviceProvider;
        }

        public async override Task<Order> Run(Order order, CommercePipelineExecutionContext context)
        {
            Condition.Requires(context).IsNotNull($"{this.Name}: The argument can not be null");
            Condition.Requires(order).IsNotNull($"{this.Name}: The argument can not be null");

        
            var ethPolicy = context.GetPolicy<EthereumClientPolicy>();
            if (ethPolicy == null)
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: missing policy configuration.", context);
                return order;
            }

            // Getting customer id contract
            var contactComponent = order.GetComponent<ContactComponent>();
            if (contactComponent == null || !contactComponent.IsRegistered)
            {
                return order;
            }
            var getCustomerCommand = new GetCustomerCommand(_getCustomerPipeline, _serviceProvider);
            var customerId = (contactComponent.CustomerId.ToLower().StartsWith("entity") ? contactComponent.CustomerId : CommerceEntity.IdPrefix<Customer>() + contactComponent.CustomerId);
            if (string.IsNullOrWhiteSpace(customerId))
                return order;
            var customer = await getCustomerCommand.Process(context.CommerceContext, customerId);
            var customerDetails = customer.GetComponent<CustomerDetailsComponent>();
                        if (!(customerDetails.View.ChildViews.Where(v => v.Name.ToLower() == Constants.Pipelines.Views.BlockchainInformationViewName.ToLower()).FirstOrDefault() is EntityView blockchainView))
                return order;

            var contractIdProperty = blockchainView.Properties.FirstOrDefault(p => p.Name == Constants.Pipelines.Fields.IdentityContractAddressFieldName);
            if (contractIdProperty == null || string.IsNullOrWhiteSpace(contractIdProperty.RawValue?.ToString()))
            {
                return order;
            }

            var web3 = new Web3(ethPolicy.NodeUrl);

            await Blockchain.UnlockMerchantAccountAsync(web3, context);  

            var royaltyContract = web3.Eth.GetContract(ethPolicy.RoyaltyContractABI, ethPolicy.RoyaltyContractAddress);
            var getAssetUrlFunction = royaltyContract.GetFunction("getAssetUrl");
            var assetPurchaseEvent = royaltyContract.GetEvent("AssetPurchased");

            var orderLines = order.Lines;
            foreach (var orderLine in orderLines)
            {
                try
                {
                    var token = new CancellationTokenSource();
                    var value = new HexBigInteger(8568000000000000); // the amount equates about $2

                    var product = orderLine.GetComponent<CartProductComponent>();
                    if (product == null)
                        continue;

                    if (!product.Tags.Any(t => t.Name == "blockchainroyalty"))
                        continue;

                    var producId = orderLine.GetComponent<CartProductComponent>()?.Id;
                    var gasEstimate = await getAssetUrlFunction.EstimateGasAsync(ethPolicy.MerchantAccountAddress, new HexBigInteger(200000), value, producId, contractIdProperty.RawValue.ToString());
                    var receipt = await getAssetUrlFunction.SendTransactionAndWaitForReceiptAsync(ethPolicy.MerchantAccountAddress, gasEstimate, value, token, producId, contractIdProperty.RawValue.ToString());
                    var filterPurchaser =  assetPurchaseEvent.CreateFilterInput(new BlockParameter(receipt.BlockNumber), BlockParameter.CreateLatest());


                    if (((bool)receipt.HasErrors()))
                    {
                        Logging.Log(context.GetPolicy<KnownResultCodes>().Error, $"Ethereum: unable to get receipt for the digital asset token retrieval. Transaction failed with status {receipt.Status}.", context);
                        return order;
                    }
               
                    var logs = await assetPurchaseEvent.GetAllChanges<AssetPurchaseEvent>(filterPurchaser);
                    orderLine.SetComponent(new DigitalDownloadBlockchainTokenComponent
                    {
                        BlockchainDownloadToken = logs.Last().Event.Token
                    });
                }
                catch (Exception ex)
                {
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: unable to send transaction getAssetUrl. "+ex.Message, context);
                    return order;
                }
                Logging.Log(context.GetPolicy<KnownResultCodes>().Information, $"Ethereum: received asset URL token from {ethPolicy.RoyaltyContractAddress}", context);
            }

            return order;
        }
    }
}
