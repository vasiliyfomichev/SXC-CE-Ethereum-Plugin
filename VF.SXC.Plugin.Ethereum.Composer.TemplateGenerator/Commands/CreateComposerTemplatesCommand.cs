
namespace VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Commands
{
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Core.Commands;
    using System;
    using System.Threading.Tasks;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines.Arguements;

    public class CreateEthereumComposerTemplatesCommand : CommerceCommand
    {
        private readonly ICreateComposerTemplatesPipeline _pipeline;


        public CreateEthereumComposerTemplatesCommand(ICreateComposerTemplatesPipeline pipeline, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            this._pipeline = pipeline;
        }

        public async Task<bool> Process(CommerceContext commerceContext, object parameter)
        {
            using (var activity = CommandActivity.Start(commerceContext, this))
            {
                var arg = new CreateComposerTemplatesArgument(parameter);
                var result = await this._pipeline.Run(arg, new CommercePipelineExecutionContextOptions(commerceContext));

                return result;
            }
        }
    }
}