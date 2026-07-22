using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;

namespace VoidCore.Tether.OBS
{
    public class ObsSceneConfigurator : ActionConfigControl
    {
        private readonly ObsSceneAction _action;

        private System.Windows.Forms.ComboBox comboBoxScene;
        private System.Windows.Forms.TextBox textBoxSceneManual;
        private System.Windows.Forms.Label labelScene;
        private System.Windows.Forms.Label labelHint;
        private System.Windows.Forms.Button buttonRefresh;

        public ObsSceneConfigurator(ObsSceneAction action, ActionConfigurator actionConfigurator)
        {
            _action = action;
            InitializeComponents();
            TryLoadScenes();

            if (!string.IsNullOrEmpty(_action.Configuration))
            {
                try
                {
                    var config = JObject.Parse(_action.Configuration);
                    string scene = config["scene"]?.ToString() ?? "";
                    textBoxSceneManual.Text = scene;
                    if (comboBoxScene.Items.Contains(scene))
                        comboBoxScene.SelectedItem = scene;
                }
                catch { }
            }
        }

        private void InitializeComponents()
        {
            this.Size = new System.Drawing.Size(400, 160);

            labelScene = new System.Windows.Forms.Label { Text = "Scene:", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(80, 20), ForeColor = System.Drawing.Color.White };

            comboBoxScene = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(110, 17), Size = new System.Drawing.Size(200, 25), DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
            comboBoxScene.SelectedIndexChanged += (s, e) => { if (comboBoxScene.SelectedItem != null) textBoxSceneManual.Text = comboBoxScene.SelectedItem.ToString(); };

            buttonRefresh = new System.Windows.Forms.Button { Text = "Refresh", Location = new System.Drawing.Point(318, 17), Size = new System.Drawing.Size(30, 25) };
            buttonRefresh.Click += (s, e) => TryLoadScenes();

            textBoxSceneManual = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(110, 57), Size = new System.Drawing.Size(270, 25) };
            textBoxSceneManual.PlaceholderText = "Enter scene name manually";

            labelHint = new System.Windows.Forms.Label
            {
                Text = ObsConnectionManager.IsConnected ? "OBS Connected." : "OBS not connected. Enter name manually.",
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(360, 40),
                ForeColor = ObsConnectionManager.IsConnected ? System.Drawing.Color.LightGreen : System.Drawing.Color.Orange
            };

            this.Controls.AddRange(new System.Windows.Forms.Control[] { labelScene, comboBoxScene, buttonRefresh, textBoxSceneManual, labelHint });
        }

        private async void TryLoadScenes()
        {
            if (!ObsConnectionManager.IsConnected) return;
            try
            {
                var response = await ObsConnectionManager.InvokeAsync("GetSceneList");
                var scenes = response?["responseData"]?["scenes"] as JArray;
                if (scenes == null) return;

                comboBoxScene.Items.Clear();
                foreach (var scene in scenes)
                {
                    string name = scene["sceneName"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        comboBoxScene.Items.Add(name);
                }

                labelHint.Text = $"{comboBoxScene.Items.Count} Scene(s) loaded.";
                labelHint.ForeColor = System.Drawing.Color.LightGreen;
            }
            catch
            {
                labelHint.Text = "Scenes could not be loaded.";
                labelHint.ForeColor = System.Drawing.Color.Orange;
            }
        }

        public override bool OnActionSave()
        {
            string sceneName = textBoxSceneManual.Text?.Trim();
            if (string.IsNullOrWhiteSpace(sceneName)) return false;

            var config = new JObject { ["scene"] = sceneName };
            _action.ConfigurationSummary = $"Scene: {sceneName}";
            _action.Configuration = config.ToString();
            return true;
        }
    }
}