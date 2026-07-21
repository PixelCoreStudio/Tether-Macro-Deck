using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using ObsWebSocket.Net.Protocol.Enums;
using System;

namespace VoidCore.Tether.OBS
{
    public class ObsRecordingAction : PluginAction
    {
        public override string Name => "OBS: Recording options";
        public override string Description => "Start, stop, pause oder toggle the OBS-Recording.";
        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
            => new ObsToggleConfigurator(this, actionConfigurator, "Record", includePause: true);

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

                RequestType requestType = mode switch
                {
                    "start"  => RequestType.StartRecord,
                    "stop"   => RequestType.StopRecord,
                    "pause"  => RequestType.PauseRecord,
                    "resume" => RequestType.ResumeRecord,
                    _        => RequestType.ToggleRecord
                };

                ObsConnectionManager.Instance.Send(requestType);
            }
            catch (Exception) { }
        }
    }
}
