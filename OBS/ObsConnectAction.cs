using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.Plugins;

namespace VoidCore.Tether.OBS
{
    public class ObsConnectAction : PluginAction
    {
        public override string Name => "OBS: Connect";
        public override string Description => "Used to refresh the connection to OBS if it deconnects.";
        public override bool CanConfigure => false;

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            ObsConnectionManager.Reconnect();
        }
    }
}