
namespace VF.SXC.Plugin.Ethereum.Pipelines.Blocks
{
    using Microsoft.Extensions.Logging;
    using Nethereum.Hex.HexTypes;
    using Nethereum.RPC.Eth.DTOs;
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

    [PipelineDisplayName("Ethereum.CreateCustomerIdentityOnBlockchain")]
    public class CreateCustomerIdentityOnBlockchainOnUpdate : PipelineBlock<Customer, Customer, CommercePipelineExecutionContext>
    {

        public async override Task<Customer> Run(Customer customer, CommercePipelineExecutionContext context)
        {
            Condition.Requires(context).IsNotNull($"{this.Name}: The argument can not be null");
            Condition.Requires(customer).IsNotNull($"{this.Name}: The argument can not be null");

            var customerDetails = customer.GetComponent<CustomerDetailsComponent>();
            var blockchainView = customer.GetComponent<ComposerTemplateViewsComponent>().Views.Where(c => c.Value.ToLower().Contains("blockchaininformation")).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(blockchainView.Key))
            {
                Logging.Log(context.GetPolicy<KnownResultCodes>().Information, "Ethereum: updated customer does not have blockchain view.", context);
                return customer;
            }

            var composerView = customer.GetComposerView(blockchainView.Key);
            var contractIdProperty = composerView.Properties.FirstOrDefault(p => p.Name == Constants.Pipelines.Fields.IdentityContractAddressFieldName);

            if (contractIdProperty == null)
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
            if (string.IsNullOrWhiteSpace(contractIdProperty.RawValue?.ToString()))
            {
                

                var token = new CancellationTokenSource();
                try
                {
                    var gasEstimate = await web3.Eth.DeployContract.EstimateGasAsync(ethPolicy.IdentityContractABI, ethPolicy.IdentityContractByteCode, ethPolicy.MerchantAccountAddress, customer.FirstName, customer.LastName);
                    var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(ethPolicy.IdentityContractABI, ethPolicy.IdentityContractByteCode, ethPolicy.MerchantAccountAddress, gasEstimate, token, customer.FirstName, customer.LastName);
                    var contractAddress = receipt.ContractAddress;
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Information, $"Ethereum: created identity for {customer.Email} at {contractAddress}.", context);

                    contractIdProperty.Value = contractAddress;
                    contractIdProperty.RawValue = contractAddress;
                }
                catch (Exception ex)
                {
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: unable to send transaction createIdentity. "+ex.Message, context);
                    return customer;
                }

                return customer;
            }
            else
            {
                var idContract = web3.Eth.GetContract(ethPolicy.IdentityContractABI, contractIdProperty.RawValue?.ToString());
                var updateIdentityFunction = idContract.GetFunction("updateIdentity");
                try
                {
                    var gasEstimate = await updateIdentityFunction.EstimateGasAsync(customer.FirstName, customer.LastName);
                    var hash = await updateIdentityFunction.SendTransactionAsync(new TransactionInput { Gas = gasEstimate, From = ethPolicy.MerchantAccountAddress }, customer.FirstName, customer.LastName);
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Information, $"Ethereum: updated identity for {customer.Email} at {contractIdProperty.RawValue?.ToString()}", context);
                }
                catch (Exception ex)
                {
                    Logging.Log(context.GetPolicy<KnownResultCodes>().Error, "Ethereum: unable to send transaction createIdentity." + ex.Message, context);
                    return customer;
                }
            }

            return customer;
        }

        
    }
}
