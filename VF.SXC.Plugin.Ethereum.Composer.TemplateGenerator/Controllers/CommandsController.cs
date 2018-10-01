

namespace VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Controllers
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Http.OData;
    using VF.SXC.Plugin.Ethereum.Composer.TemplateGenerator.Commands;
    using Microsoft.AspNetCore.Mvc;

    using Sitecore.Commerce.Core;


    public class CommandsController : CommerceController
    {
        public CommandsController(IServiceProvider serviceProvider, CommerceEnvironment globalEnvironment)
            : base(serviceProvider, globalEnvironment)
        {
        }

        [HttpPut]
        [Route("CreateEthereumComposerTemplatesCommand()")]
        public async Task<IActionResult> CreateEthereumComposerTemplatesCommand([FromBody] ODataActionParameters value)
        {

            var command = this.Command<CreateEthereumComposerTemplatesCommand>();
            var result = await command.Process(this.CurrentContext, "Placeholder");

            return new ObjectResult(command);
        }
    }
}

