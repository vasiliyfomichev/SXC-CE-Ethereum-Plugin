using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VF.SXC.Plugin.Ethereum.Utilities
{
    public class Logging
    {
        public static void Log (string type, string message, CommercePipelineExecutionContext context)
        {
            context.Logger.LogInformation(message);
            context.CommerceContext.AddMessage(new CommandMessage { Code = type, Text = message });
        }
    }
}
