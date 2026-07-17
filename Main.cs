using System;
using System.Collections.Generic;
using SuchByte.MacroDeck.Plugins;

namespace VoidCore.Tether
{
    public class Main : MacroDeckPlugin
    {
        public override void Enable()
        {
            this.Actions = new List<PluginAction>
            {
                new HttpRequestAction()
            };
        }
    }
}
