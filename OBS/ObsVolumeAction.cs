using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using ObsWebSocket.Net.Protocol.Enums;
using ObsWebSocket.Net.Protocol.Requests;
using System;
using System.Threading.Tasks;

namespace VoidCore.Tether.OBS
{
    public class ObsVolumeAction : PluginAction
    {
        public override string Name => "OBS: Volume";
        public override string Description => "Increase, decrease or set the volume from a audio source in OBS (or mutet/unmutet them).";
        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
            => new ObsVolumeConfigurator(this, actionConfigurator);

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            if (string.IsNullOrEmpty(this.Configuration)) return;
            if (!ObsConnectionManager.IsConnected) return;

            try
            {
                var config = JObject.Parse(this.Configuration);
                string inputName = config["source"]?.ToString();
                string mode = config["mode"]?.ToString() ?? "increase";
                double step = config["step"]?.ToObject<double>() ?? 5.0;
                double target = config["target"]?.ToObject<double>() ?? -20.0;

                if (string.IsNullOrWhiteSpace(inputName)) return;

                Task.Run(async () =>
                {
                    try
                    {
                        if (mode == "mute")
                        {
                            ObsConnectionManager.Instance.Send(RequestType.SetInputMute, new { inputName, inputMuted = true });
                            return;
                        }
                        if (mode == "unmute")
                        {
                            ObsConnectionManager.Instance.Send(RequestType.SetInputMute, new { inputName, inputMuted = false });
                            return;
                        }
                        if (mode == "togglemute")
                        {
                            ObsConnectionManager.Instance.Send(RequestType.ToggleInputMute, new { inputName });
                            return;
                        }

                        double newDb;
                        if (mode == "set")
                        {
                            newDb = target;
                        }
                        else
                        {
                            var current = await ObsConnectionManager.Instance.Invoke<GetInputVolumeResponse>(
                                RequestType.GetInputVolume, new { inputName });
                            double currentDb = current?.InputVolumeDb ?? -20.0;
                            newDb = mode == "decrease" ? currentDb - step : currentDb + step;
                        }

                        if (newDb > 0) newDb = 0;
                        if (newDb < -100) newDb = -100;

                        ObsConnectionManager.Instance.Send(RequestType.SetInputVolume, new { inputName, inputVolumeDb = newDb });
                    }
                    catch { }
                });
            }
            catch (Exception) { }
        }
    }
}