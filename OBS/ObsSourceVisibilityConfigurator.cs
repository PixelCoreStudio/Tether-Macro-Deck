using Newtonsoft.Json.Linq;
using ObsWebSocket.Net.Protocol.Enums;
using ObsWebSocket.Net.Protocol.Requests;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;

namespace VoidCore.Tether.OBS
{
    public class ObsSourceVisibilityConfigurator : ActionConfigControl
    {
        private readonly ObsSourceVisibilityAction _action;

        private System.Windows.Forms.ComboBox comboBoxScene;
        private System.Windows.Forms.TextBox textBoxSceneManual;
        private System.Windows.Forms.ComboBox comboBoxSource;
        private System.Windows.Forms.TextBox textBoxSourceManual;
        private System.Windows.Forms.ComboBox comboBoxMode;
        private System.Windows.Forms.Label labelScene, labelSource, labelMode;
        private System.Windows.Forms.Button buttonRefreshScenes, buttonRefreshSources;

        public ObsSourceVisibilityConfigurator(ObsSourceVisibilityAction action, ActionConfigurator actionConfigurator)
        {
            _action = action;
            InitializeComponents();
            TryLoadScenes();

            if (!string.IsNullOrEmpty(_action.Configuration))
            {
                try
                {
                    var config = JObject.Parse(_action.Configuration);
                    textBoxSceneManual.Text = config["scene"]?.ToString() ?? "";
                    textBoxSourceManual.Text = config["source"]?.ToString() ?? "";
                    string mode = config["mode"]?.ToString() ?? "toggle";
                    comboBoxMode.SelectedItem = mode;
                }
                catch { }
            }
            if (comboBoxMode.SelectedIndex < 0) comboBoxMode.SelectedIndex = 0;
        }

        private void InitializeComponents()
        {
            this.Size = new System.Drawing.Size(400, 250);

            labelScene = new System.Windows.Forms.Label { Text = "Scene:", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(80, 20), ForeColor = System.Drawing.Color.White };
            comboBoxScene = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(110, 17), Size = new System.Drawing.Size(200, 25), DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
            comboBoxScene.SelectedIndexChanged += (s, e) => { if (comboBoxScene.SelectedItem != null) { textBoxSceneManual.Text = comboBoxScene.SelectedItem.ToString(); TryLoadSources(comboBoxScene.SelectedItem.ToString()); } };
            buttonRefreshScenes = new System.Windows.Forms.Button { Text = "Refresh", Location = new System.Drawing.Point(318, 17), Size = new System.Drawing.Size(30, 25) };
            buttonRefreshScenes.Click += (s, e) => TryLoadScenes();
            textBoxSceneManual = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(110, 50), Size = new System.Drawing.Size(270, 25) };
            textBoxSceneManual.PlaceholderText = "Scene name manually";

            labelSource = new System.Windows.Forms.Label { Text = "Source:", Location = new System.Drawing.Point(20, 90), Size = new System.Drawing.Size(80, 20), ForeColor = System.Drawing.Color.White };
            comboBoxSource = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(110, 87), Size = new System.Drawing.Size(200, 25), DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
            comboBoxSource.SelectedIndexChanged += (s, e) => { if (comboBoxSource.SelectedItem != null) textBoxSourceManual.Text = comboBoxSource.SelectedItem.ToString(); };
            buttonRefreshSources = new System.Windows.Forms.Button { Text = "Refresh", Location = new System.Drawing.Point(318, 87), Size = new System.Drawing.Size(30, 25) };
            buttonRefreshSources.Click += (s, e) => { string scene = textBoxSceneManual.Text.Trim(); if (!string.IsNullOrWhiteSpace(scene)) TryLoadSources(scene); };
            textBoxSourceManual = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(110, 120), Size = new System.Drawing.Size(270, 25) };
            textBoxSourceManual.PlaceholderText = "Source name manually";

            labelMode = new System.Windows.Forms.Label { Text = "Action:", Location = new System.Drawing.Point(20, 160), Size = new System.Drawing.Size(80, 20), ForeColor = System.Drawing.Color.White };
            comboBoxMode = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(110, 157), Size = new System.Drawing.Size(150, 25), DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
            comboBoxMode.Items.AddRange(new object[] { "toggle", "show", "hide" });

            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                labelScene, comboBoxScene, buttonRefreshScenes, textBoxSceneManual,
                labelSource, comboBoxSource, buttonRefreshSources, textBoxSourceManual,
                labelMode, comboBoxMode
            });
        }

        private async void TryLoadScenes()
        {
            if (!ObsConnectionManager.IsConnected) return;
            try
            {
                var response = await ObsConnectionManager.Instance.Invoke<GetSceneListResponse>(RequestType.GetSceneList);
                if (response?.Scenes == null) return;
                comboBoxScene.Items.Clear();
                foreach (var scene in response.Scenes)
                    comboBoxScene.Items.Add(scene.SceneName);
            }
            catch { }
        }

        private async void TryLoadSources(string sceneName)
        {
            if (!ObsConnectionManager.IsConnected) return;
            try
            {
                var response = await ObsConnectionManager.Instance.Invoke<GetSceneItemListResponse>(
                    RequestType.GetSceneItemList, new { sceneName });
                if (response?.SceneItems == null) return;
                comboBoxSource.Items.Clear();
                foreach (var item in response.SceneItems)
                    comboBoxSource.Items.Add(item.SourceName);
            }
            catch { }
        }

        public override bool OnActionSave()
        {
            string scene = textBoxSceneManual.Text?.Trim();
            string source = textBoxSourceManual.Text?.Trim();
            if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(source)) return false;

            string mode = comboBoxMode.SelectedItem?.ToString() ?? "toggle";
            var config = new JObject { ["scene"] = scene, ["source"] = source, ["mode"] = mode };
            _action.ConfigurationSummary = $"{mode}: {source} in {scene}";
            _action.Configuration = config.ToString();
            return true;
        }
    }
}
