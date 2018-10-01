using Nethereum.Web3;
using Sitecore.Commerce.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VF.SXC.Plugin.Ethereum.Policies;
using Microsoft.Extensions.Logging;
using Nethereum.RPC.Eth.DTOs;
using System.Threading;

namespace VF.SXC.Plugin.Ethereum.Utilities
{
    public class Blockchain
    {
        public static async Task UnlockMerchantAccountAsync(Web3 web3, CommercePipelineExecutionContext context)
        {
            var ethPolicy = context.GetPolicy<EthereumClientPolicy>();
            if (ethPolicy == null)
            {
                context.Logger.LogError("Ethereum: missing policy configuration.");
                return;
            }

            var unlockResult = false;
            try
            {
                unlockResult = await web3.Personal.UnlockAccount.SendRequestAsync(ethPolicy.MerchantAccountAddress, ethPolicy.MerchantAccountPassword, 3600);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Ethereum: error occured while trying to unlock the {ethPolicy.MerchantAccountAddress} account. Make sure the proper contract address and password was specified in the EthereumClientPolicy.", ex);
                return;
            }

            if (!unlockResult)
            {
                
                context.Logger.LogError("Ethereum: unable to unlock the account. Make sure the proper contract address and password was specified in the EthereumClientPolicy.");
                throw new Exception($"Ethereum: Unable to unlock account {ethPolicy.MerchantAccountAddress}.");
            }
        }

        public static async Task<TransactionReceipt> GetReceiptAsync(Web3 web3, string transactionHash)
        {

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            return receipt;
        }
    }
}
