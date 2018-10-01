using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VF.SXC.Plugin.Ethereum.Components;

namespace VF.SXC.Plugin.Ethereum.Pipelines.Blocks
{
    [PipelineDisplayName("Ethereum.AddDigitalDownloadBlockchainToken")]
    public class AddDigitalDownloadBlockchainToken : PipelineBlock<Cart, Cart, CommercePipelineExecutionContext>
    {
        public override Task<Cart> Run(Cart cart, CommercePipelineExecutionContext context)
        {
            Condition.Requires(cart).IsNotNull("The argument can not be null");
            var arg = context.CommerceContext.GetObjects<CartLineArgument>().First();
            var cartLine = arg.Line;

            cartLine.SetComponent(new DigitalDownloadBlockchainTokenComponent());
            return Task.FromResult(cart);
        }
    }
}
