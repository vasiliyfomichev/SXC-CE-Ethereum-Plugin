namespace VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines  
{
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines.Arguements;
    using Microsoft.Extensions.Logging;
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Pipelines;

    public class CreateComposerTemplatesPipeline : CommercePipeline<CreateComposerTemplatesArgument, bool>, ICreateComposerTemplatesPipeline
    {
        public CreateComposerTemplatesPipeline(IPipelineConfiguration<ICreateComposerTemplatesPipeline> configuration, ILoggerFactory loggerFactory)
            : base(configuration, loggerFactory)
        {
        }
    }
}

