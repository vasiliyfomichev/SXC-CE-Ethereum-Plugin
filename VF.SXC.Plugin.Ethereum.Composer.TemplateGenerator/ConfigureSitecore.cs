
namespace VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator
{
    using System.Reflection;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines.Blocks;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;

    /// <summary>
    /// The configure sitecore class.
    /// </summary>
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>
        /// The configure services.
        /// </summary>
        /// <param name="services">
        /// The services.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            services.Sitecore().Pipelines(config => config

             .AddPipeline<ICreateComposerTemplatesPipeline, CreateComposerTemplatesPipeline>(
                    configure =>
                    {
                        configure.Add<CreateComposerTemplatesBlock>();
                    })

               .ConfigurePipeline<IConfigureServiceApiPipeline>(configure => configure.Add<ConfigureServiceApiBlock>()));

            services.RegisterAllCommands(assembly);
        }
    }
}