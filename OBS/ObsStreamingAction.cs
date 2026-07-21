using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using ObsWebSocket.Net.Protocol.Enums;
using System;
using System.Threading.Tasks;

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

                Task.Run(async () =>
                {
                    try
                    {
                        if (mode == "toggle")
                        {
                            ObsConnectionManager.Instance.Send(RequestType.ToggleStream);
                        }
                        else if (mode == "start")
                        {
                            ObsConnectionManager.Instance.Send(RequestType.StartStream);
                        }
                        else
                        {
                            ObsConnectionManager.Instance.Send(RequestType.StopStream);
                        }
                    }
                    catch { }
                });
            }
            catch (Exception) { }
        }
    }
}
