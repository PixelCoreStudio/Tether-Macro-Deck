using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using ObsWebSocket.Net.Protocol.Enums;
using System;
using System.Threading.Tasks;
using ObsWebSocket.Net.Protocol.Requests;

namespace VoidCore.Tether.OBS
{
    public class ObsSourceVisibilityAction : PluginAction
    {
        public override string Name => "OBS: Show/hide source";
        public override string Description => "Shows or hides a source in an OBS scene.";
        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
            => new ObsSourceVisibilityConfigurator(this, actionConfigurator);

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            if (string.IsNullOrEmpty(this.Configuration)) return;
            if (!ObsConnectionManager.IsConnected) return;
            try
            {
                var config = JObject.Parse(this.Configuration);
                string sceneName = config["scene"]?.ToString();
                string sourceName = config["source"]?.ToString();
                string mode = config["mode"]?.ToString() ?? "toggle";

                if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(sourceName)) return;

                Task.Run(async () =>
                {
                    try
                    {
                        var idResponse = await ObsConnectionManager.Instance.Invoke<GetSceneItemIdResponse>(
                            RequestType.GetSceneItemId,
                            new { sceneName, sourceName, searchOffset = 0 });

                        if (idResponse == null) return;
                        int sceneItemId = (int)idResponse.SceneItemId;

                        bool newVisible;
                        if (mode == "show")
                        {
                            newVisible = true;
                        }
                        else if (mode == "hide")
                        {
                            newVisible = false;
                        }
                        else
                        {
                            var visResponse = await ObsConnectionManager.Instance.Invoke<GetSceneItemEnabledResponse>(
                                RequestType.GetSceneItemEnabled,
                                new { sceneName, sceneItemId });
                            newVisible = !(visResponse?.SceneItemEnabled ?? true);
                        }

                        ObsConnectionManager.Instance.Send(
                            RequestType.SetSceneItemEnabled,
                            new { sceneName, sceneItemId, sceneItemEnabled = newVisible });
                    }
                    catch { }
                });
            }
            catch (Exception) { }
        }
    }
}
