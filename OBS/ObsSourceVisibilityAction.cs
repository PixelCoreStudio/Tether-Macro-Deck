using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;
using System.Threading.Tasks;

namespace VoidCore.Tether.OBS
{
    public class ObsSourceVisibilityAction : PluginAction
    {
        public override string Name => "OBS: Show/hide source";
        public override string Description => "Shows or hides a source (or a source inside a group) in an OBS scene.";
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
                string scene = config["scene"]?.ToString();
                string source = config["source"]?.ToString();
                string group = config["group"]?.ToString();
                string mode = config["mode"]?.ToString() ?? "toggle";

                if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(source)) return;

                string effectiveScene = !string.IsNullOrWhiteSpace(group) ? group : scene;

                Task.Run(async () =>
                {
                    try
                    {
                        var idResp = await ObsConnectionManager.InvokeAsync("GetSceneItemId",
                            new { sceneName = effectiveScene, sourceName = source, searchOffset = 0 });
                        if (idResp == null) return;

                        int sceneItemId = idResp["responseData"]?["sceneItemId"]?.Value<int>() ?? -1;
                        if (sceneItemId < 0) return;

                        bool newVisible;
                        if (mode == "show") newVisible = true;
                        else if (mode == "hide") newVisible = false;
                        else
                        {
                            var visResp = await ObsConnectionManager.InvokeAsync("GetSceneItemEnabled",
                                new { sceneName = effectiveScene, sceneItemId });
                            newVisible = !(visResp?["responseData"]?["sceneItemEnabled"]?.Value<bool>() ?? true);
                        }

                        ObsConnectionManager.Send("SetSceneItemEnabled",
                            new { sceneName = effectiveScene, sceneItemId, sceneItemEnabled = newVisible });
                    }
                    catch { }
                });
            }
            catch (Exception) { }
        }
    }
}