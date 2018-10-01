namespace VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines
{
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines.Arguements;
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Pipelines;


    [PipelineDisplayName(Constants.Pipelines.ComposerTemplateGeneration)]
    public interface ICreateComposerTemplatesPipeline : IPipeline<CreateComposerTemplatesArgument, bool, CommercePipelineExecutionContext>
    {
    }
}
