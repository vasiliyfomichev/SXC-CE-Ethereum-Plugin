
namespace VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines.Blocks
{
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;
    using Sitecore.Commerce.Plugin.Composer;
    using Sitecore.Commerce.Plugin.ManagedLists;
    using Sitecore.Commerce.Plugin.Views;
    using Sitecore.Commerce.EntityViews;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Pipelines.Arguements;
    using System;

    [PipelineDisplayName(Constants.Pipelines.Blocks.CreateComposerTemplatesBlock)]
    public class CreateComposerTemplatesBlock : PipelineBlock<CreateComposerTemplatesArgument, bool, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commerceCommander;
        public CreateComposerTemplatesBlock(CommerceCommander commerceCommander)
        {
            _commerceCommander = commerceCommander;
        }


        #region Template Creation Methods

        public override async Task<bool> Run(CreateComposerTemplatesArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{this.Name}: The argument can not be null");

            CreateBlockchainInformationTemplate(context);

            return await Task.FromResult(true);
        }

        private async void CreateBlockchainInformationTemplate(CommercePipelineExecutionContext context)
        {

            string templateId = $"{CommerceEntity.IdPrefix<ComposerTemplate>()}{"BlockchainInformation"}";

            try
            {
                // delete the existing template (this does not delete data from products)
                var deleteTemplate = await _commerceCommander.DeleteEntity(context.CommerceContext, templateId);

                var composerTemplate = new ComposerTemplate(templateId);
                composerTemplate.GetComponent<ListMembershipsComponent>().Memberships.Add(CommerceEntity.ListName<ComposerTemplate>());
                composerTemplate.Tags.Add(new Tag("Blockchain"));
                composerTemplate.Name = "BlockchainInformation";
                composerTemplate.DisplayName = "Blockchain Information";

                var composerTemplateViewComponent = composerTemplate.GetComponent<EntityViewComponent>();
                composerTemplateViewComponent.Id = "95DACDDD-151E-4527-A068-A27C9275967J";
                var composerTemplateView = new EntityView
                {
                    Name = Constants.Pipelines.Views.BlockchainInformationViewName,
                    DisplayName = Constants.Pipelines.Views.BlockchainInformationViewDisplayName,
                    DisplayRank = 0,
                    ItemId = Constants.Pipelines.Views.BlockchainInformationViewId,
                    EntityId = composerTemplate.Id
                };

                composerTemplateView.Properties.Add(new ViewProperty()
                {
                    IsRequired = false,
                    DisplayName = "Identity Contract Address",
                    Name = Constants.Pipelines.Fields.IdentityContractAddressFieldName,
                    OriginalType = "System.String",
                    RawValue = "",
                    Value = ""
                });

                composerTemplateView.Properties.Add(new ViewProperty()
                {
                    IsRequired = false,
                    DisplayName = "Publish Purchased Products",
                    Name = Constants.Pipelines.Fields.PublishProductsFieldName,
                    OriginalType = "System.Boolean",
                    RawValue = false,
                    Value = "false"
                });

                composerTemplateView.Properties.Add(new ViewProperty()
                {
                    IsRequired = false,
                    DisplayName = "ETH Balance",
                    Name = Constants.Pipelines.Fields.EthBalanceFieldName,
                    OriginalType = "System.String",
                    RawValue = string.Empty,
                    Value = string.Empty,
                    IsReadOnly = true
                });


                composerTemplateViewComponent.View.ChildViews.Add(composerTemplateView);
                var persistResult = await this._commerceCommander.PersistEntity(context.CommerceContext, composerTemplate);
            }
            catch (Exception ex)
            {
                context.Logger.LogError(string.Format("{0}. Unable to generate Composer tamplate '{1}'. {2}", Constants.Pipelines.Blocks.CreateComposerTemplatesBlock, templateId, ex.Message), Array.Empty<object>());
            }
        }

        #endregion

    }
}
