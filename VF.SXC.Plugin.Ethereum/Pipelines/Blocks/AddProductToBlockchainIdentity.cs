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
    using System.Threading.Tasks;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator;
    using VF.SXC.Plugin.Ethereum.Policies;
    using VF.SXC.Plugin.Ethereum.Utilities;

    [PipelineDisplayName("Ethereum.AddProductToBlockchainIdentity")]
    public class AddProductToBlockchainIdentity : PipelineBlock<Order, Order, CommercePipelineExecutionContext>
    {
        private readonly IGetCustomerPipeline _getCustomerPipeline;
        private readonly IServiceProvider _serviceProvider;
        public AddProductToBlockchainIdentity(IGetCustomerPipeline getCustomerPipeline, IServiceProvider serviceProvider)
        {
            this._getCustomerPipeline = getCustomerPipeline;
            this._serviceProvider = serviceProvider;
        }
        public async override Task<Order> Run(Order order, CommercePipelineExecutionContext context)
        {
            Condition.Requires(context).IsNotNull($"{this.Name}: The argument can not be null");
            Condition.Requires(order).IsNotNull($"{this.Name}: The argument can not be null");

            // Getting the blockchain contract address and ensuring the customer is a blockchain customer and opted in for product publishing
            var contactComponent = order.GetComponent<ContactComponent>();
            if (contactComponent == null)
            {
                return order;
            }
            var getCustomerCommand = new GetCustomerCommand(_getCustomerPipeline, _serviceProvider);
            var customer = await getCustomerCommand.Process(context.CommerceContext, (contactComponent.CustomerId.ToLower().StartsWith("entity") ? contactComponent.CustomerId : CommerceEntity.IdPrefix<Customer>()+contactComponent.CustomerId));
            var customerDetails = customer.GetComponent<CustomerDetailsComponent>();

            if (!(customerDetails.View.ChildViews.Where(v => v.Name.ToLower() == Constants.Pipelines.Views.BlockchainInformationViewName.ToLower()).FirstOrDefault() is EntityView blockchainView))
                return order;

            var contractIdProperty = blockchainView.Properties.FirstOrDefault(p => p.Name == Constants.Pipelines.Fields.IdentityContractAddressFieldName);
            var publishProductsProperty = blockchainView.Properties.FirstOrDefault(p => p.Name == Constants.Pipelines.Fields.PublishProductsFieldName);

            // Only submit products for orders where customer have blockchain accounts and have enabled product publishing
            if (contractIdProperty == null || string.IsNullOrWhiteSpace(contractIdProperty.RawValue?.ToString()) || publishProductsProperty==null || publishProductsProperty.RawValue.ToString()=="false")
            {
                return order;
            }

            var ethPolicy = context.GetPolicy<EthereumClientPolicy>();
            if (ethPolicy == null)
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: missing policy configuration.", context);
                return order;
            }

            var web3 = new Web3(ethPolicy.NodeUrl);
            await Blockchain.UnlockMerchantAccountAsync(web3, context);
            var idContract = web3.Eth.GetContract(ethPolicy.IdentityContractABI, contractIdProperty.RawValue?.ToString());
            var producIds = order.Lines.Select(o => o.GetComponent<CartProductComponent>()?.Id).ToList();

            var hasPurchasedFunction = idContract.GetFunction("contactHasPurchasedProduct");
            var purchasedProductFunction = idContract.GetFunction("addPurchasedProduct");

            foreach (var productId in producIds)
            {
                try
                {
                    // Check if the customer has purchased thisproduct in the past
                    var hasPurchasedProduct = await hasPurchasedFunction.CallAsync<bool>(productId);
                    if (hasPurchasedProduct)
                        continue;
                    var hash = await purchasedProductFunction.SendTransactionAsync(new TransactionInput { Gas = new HexBigInteger(200000), From = ethPolicy.MerchantAccountAddress }, productId);
                }
                catch (Exception ex)
                {
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: unable to send call contactHasPurchasedProduct or transaction addPurchasedProduct." + ex.Message, context);
                    return order;
                }
                Logging.Log(context.GetPolicy<KnownResultCodes>().Information, $"Ethereum: added product {productId} to {contractIdProperty.RawValue?.ToString()} purchase ledger.", context);
            }

            return order;
        }
    }
}
