
namespace Sitecore.Commerce.Plugin.Sample
{
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.Carts;
    using Sitecore.Commerce.Plugin.Customers;
    using Sitecore.Commerce.Plugin.Orders;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;
    using System.Reflection;
    using VF.SXC.Plugin.Ethereum.Pipelines.Blocks;

    public class ConfigureSitecore : IConfigureSitecore
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            services.Sitecore().Pipelines(config => config
               .ConfigurePipeline<ICreateOrderPipeline>(configure => configure.Add<AddProductToBlockchainIdentity>().Before<IPersistOrderPipeline>())
               .ConfigurePipeline<ICreateOrderPipeline>(configure => configure.Add<UpdateLoyaltyTokensOnBlockchain>().After<AddProductToBlockchainIdentity>())
               .ConfigurePipeline<ICreateOrderPipeline>(configure => configure.Add<SetBlockchainDownloadToken>().After<UpdateLoyaltyTokensOnBlockchain>())

               .ConfigurePipeline<ICreateCustomerPipeline>(configure => configure.Add<CreateCustomerIdentityOnBlockchainOnCreate>().After<CreateCustomerBlock>())
               .ConfigurePipeline<IUpdateCustomerDetailsPipeline>(configure => configure.Add<CreateCustomerIdentityOnBlockchainOnUpdate>().After<UpdateCustomerDetailsBlock>())
               .ConfigurePipeline<IGetCustomerPipeline>(configure => configure.Add<UpdateEthBalance>().After<GetCustomerBlock>())

               .ConfigurePipeline<IGetEntityViewPipeline>(configure => configure.Add<UpdateEthBalanceOnEntityView>().After<GetCustomerDetailsViewBlock>())
               );

            services.RegisterAllCommands(assembly);
        }
    }
}