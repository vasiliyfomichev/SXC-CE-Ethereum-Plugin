namespace VF.SXC.Plugin.Ethereum.Policies
{
    using Sitecore.Commerce.Core;

    /// <inheritdoc />
    /// <summary>
    /// Defines a policy
    /// </summary>
    /// <seealso cref="T:Sitecore.Commerce.Core.Policy" />
    public class EthereumClientPolicy : Policy
    {
        public string MerchantAccountAddress { get; set; }

        public string MerchantAccountPassword { get; set; }

        public string NodeUrl { get; set; }

        #region Identity Contract 

        public string IdentityContractABI { get; set; }

        public string IdentityContractByteCode { get; set; }

        #endregion

        public string LoyaltyContractAddress { get; set; }

        public string LoaltyContractABI { get; set; }

        public string RoyaltyContractAddress { get; set; }

        public string RoyaltyContractABI { get; set; }
    }
}
