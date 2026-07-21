using System.Collections.Generic;
using SuchByte.MacroDeck.Plugins;
using VoidCore.Tether.OBS;

namespace VoidCore.Tether
{
    public class Main : MacroDeckPlugin
    {
        public override bool CanConfigure => true;

        public override void OpenConfigurator()
        {
            using (var form = new ObsGlobalConfigForm())
            {
                form.ShowDialog();
            }
        }

        public override void Enable()
        {
            this.Actions = new List<PluginAction>
            {
                // HTTP
                new HttpRequestAction(),

                // OBS WebSocket
                new ObsConnectAction(),
                new ObsSceneAction(),
                new ObsStreamingAction(),
                new ObsRecordingAction(),
                new ObsSourceVisibilityAction(),
                new ObsVolumeAction(),
            };

            ObsConnectionManager.Initialize(this);
        }
    }
}