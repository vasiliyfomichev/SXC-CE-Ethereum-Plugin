

namespace VF.SXC.Plugin.Ethereum.Pipelines.Blocks
{
    using Microsoft.Extensions.Logging;
    using Nethereum.Hex.HexTypes;
    using Nethereum.RPC.Eth.DTOs;
    using Nethereum.Util;
    using Nethereum.Web3;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.Composer;
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

    [PipelineDisplayName("Ethereum.UpdateLoyaltyTokensOnBlockchain")]
    public class UpdateLoyaltyTokensOnBlockchain : PipelineBlock<Order, Order, CommercePipelineExecutionContext>
    {
        private readonly IGetCustomerPipeline _getCustomerPipeline;
        private readonly IServiceProvider _serviceProvider;
        public UpdateLoyaltyTokensOnBlockchain(IGetCustomerPipeline getCustomerPipeline, IServiceProvider serviceProvider)
        {
            this._getCustomerPipeline = getCustomerPipeline;
            this._serviceProvider = serviceProvider;
        }

        public async override Task<Order> Run(Order order, CommercePipelineExecutionContext context)
        {
            Condition.Requires(context).IsNotNull($"{this.Name}: The argument can not be null");
            Condition.Requires(order).IsNotNull($"{this.Name}: The argument can not be null");

            // Getting the blockchain contract address
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

            // Only submit products for orders where customer have blockchain accounts and have enabled product publishing
            if (contractIdProperty == null || string.IsNullOrWhiteSpace(contractIdProperty.RawValue?.ToString()))
            {
                return order;
            }

            var orderSubtotal = order.Totals.SubTotal.Amount;
            var ethPolicy = context.GetPolicy<EthereumClientPolicy>();
            if (ethPolicy == null)
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: missing policy configuration.", context);
                return order;
            }

            var web3 = new Web3(ethPolicy.NodeUrl);

            // Unlock merch account to be able to send state change transactions
            await Blockchain.UnlockMerchantAccountAsync(web3, context);

            var idContract = web3.Eth.GetContract(ethPolicy.LoaltyContractABI, ethPolicy.LoyaltyContractAddress);
            var addLoyaltyPointsFunction = idContract.GetFunction("addLoyaltyPoints");

            

            try
            {
                // TODO: this implementation uses a separate loyalty contract balance, potntial discrepancies over time with bank depletion
                var usdPerEth = await UpdateEthBalanceOnEntityView.GetUsdAmountAsync(1, context);

                var subtotalInWei = UnitConversion.Convert.ToWei((orderSubtotal / usdPerEth), UnitConversion.EthUnit.Ether);
                var loyaltyEstimatedAmount = UnitConversion.Convert.ToWei(((orderSubtotal / usdPerEth)/100), UnitConversion.EthUnit.Ether);
                // validating the royalty contract amount by passing precalculated value (1%)
                var gasEstimate = await addLoyaltyPointsFunction.EstimateGasAsync(ethPolicy.MerchantAccountAddress, new HexBigInteger(200000), new HexBigInteger(loyaltyEstimatedAmount), subtotalInWei, contractIdProperty.RawValue.ToString());
                var hash = await addLoyaltyPointsFunction.SendTransactionAsync(new TransactionInput { Gas = new HexBigInteger(gasEstimate), From = ethPolicy.MerchantAccountAddress, Value= new HexBigInteger(loyaltyEstimatedAmount) }, subtotalInWei, contractIdProperty.RawValue.ToString());
            }
            catch (Exception ex)
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: unable to send transaction addLoyaltyPoints. "+ ex.Message, context);
                return order;
            }

            Logging.Log(context.GetPolicy<KnownResultCodes>().Information, $"Ethereum: added loyalty points for the purchasein the amount of ${orderSubtotal} to {contractIdProperty.RawValue} based on the police defined in {ethPolicy.LoyaltyContractAddress}.", context);

            return order;
        }
    }
}
