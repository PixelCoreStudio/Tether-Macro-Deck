using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace VoidCore.Tether.OBS
{
    public class ObsSceneAction : PluginAction
    {
        public override string Name => "OBS: Scenes switcher";
        public override string Description => "Switches to a specific OBS-Scene.";
        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
            => new ObsSceneConfigurator(this, actionConfigurator);

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            if (string.IsNullOrEmpty(this.Configuration)) return;
            if (!ObsConnectionManager.IsConnected) return;
            try
            {
                var config = JObject.Parse(this.Configuration);
                string sceneName = config["scene"]?.ToString();
                if (string.IsNullOrWhiteSpace(sceneName)) return;

                ObsConnectionManager.Send("SetCurrentProgramScene", new { sceneName });
            }
            catch (Exception) { }
        }
    }
}