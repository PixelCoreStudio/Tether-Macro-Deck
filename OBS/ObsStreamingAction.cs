using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace VoidCore.Tether.OBS
{
    public class ObsStreamingAction : PluginAction
    {
        public override string Name => "OBS: Control stream";
        public override string Description => "Starts, stops, or toggles the OBS stream.";
        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
            => new ObsToggleConfigurator(this, actionConfigurator, "Stream");

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            if (!ObsConnectionManager.IsConnected) return;
            try
            {
                string mode = "toggle";
                if (!string.IsNullOrEmpty(this.Configuration))
                {
                    var config = JObject.Parse(this.Configuration);
                    mode = config["mode"]?.ToString() ?? "toggle";
                }

                string requestType = mode switch
                {
                    "start" => "StartStream",
                    "stop" => "StopStream",
                    _ => "ToggleStream"
                };
                ObsConnectionManager.Send(requestType);
            }
            catch (Exception) { }
        }
    }
}