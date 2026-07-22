using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Logging;
using System.Collections.Generic;

namespace VoidCore.Tether.OBS
{
    public class ObsSourceVisibilityConfigurator : ActionConfigControl
    {
        private readonly ObsSourceVisibilityAction _action;

        private List<SourceEntry> _sourceEntries = new List<SourceEntry>();

        private System.Windows.Forms.ComboBox comboBoxScene;
        private System.Windows.Forms.TextBox textBoxSceneManual;
        private System.Windows.Forms.ComboBox comboBoxSource;
        private System.Windows.Forms.TextBox textBoxSourceManual;
        private System.Windows.Forms.ComboBox comboBoxMode;
        private System.Windows.Forms.Label labelScene, labelSource, labelMode, labelHint;
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

                    if (comboBoxScene.Items.Contains(textBoxSceneManual.Text))
                        comboBoxScene.SelectedItem = textBoxSceneManual.Text;
                }
                catch { }
            }

            if (comboBoxMode.SelectedIndex < 0) comboBoxMode.SelectedIndex = 0;
        }

        private void InitializeComponents()
        {
            this.Size = new System.Drawing.Size(420, 270);

            labelScene = new System.Windows.Forms.Label
            {
                Text = "Scene:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(80, 20),
                ForeColor = System.Drawing.Color.White
            };
            comboBoxScene = new System.Windows.Forms.ComboBox
            {
                Location = new System.Drawing.Point(110, 17),
                Size = new System.Drawing.Size(210, 25),
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            };
            comboBoxScene.SelectedIndexChanged += (s, e) =>
            {
                if (comboBoxScene.SelectedItem == null) return;
                string scene = comboBoxScene.SelectedItem.ToString();
                textBoxSceneManual.Text = scene;
                TryLoadSources(scene);
            };
            buttonRefreshScenes = new System.Windows.Forms.Button
            {
                Text = "\u21ba",
                Location = new System.Drawing.Point(328, 17),
                Size = new System.Drawing.Size(30, 25)
            };
            buttonRefreshScenes.Click += (s, e) => TryLoadScenes();

            textBoxSceneManual = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(110, 50),
                Size = new System.Drawing.Size(280, 25)
            };
            textBoxSceneManual.PlaceholderText = "Scene name (manually if needed)";

            labelSource = new System.Windows.Forms.Label
            {
                Text = "Source:",
                Location = new System.Drawing.Point(20, 90),
                Size = new System.Drawing.Size(80, 20),
                ForeColor = System.Drawing.Color.White
            };
            comboBoxSource = new System.Windows.Forms.ComboBox
            {
                Location = new System.Drawing.Point(110, 87),
                Size = new System.Drawing.Size(210, 25),
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            };
            comboBoxSource.SelectedIndexChanged += (s, e) =>
            {
                int idx = comboBoxSource.SelectedIndex;
                if (idx < 0 || idx >= _sourceEntries.Count) return;
                textBoxSourceManual.Text = _sourceEntries[idx].SourceName;
            };
            buttonRefreshSources = new System.Windows.Forms.Button
            {
                Text = "\u21ba",
                Location = new System.Drawing.Point(328, 87),
                Size = new System.Drawing.Size(30, 25)
            };
            buttonRefreshSources.Click += (s, e) =>
            {
                string scene = textBoxSceneManual.Text.Trim();
                if (!string.IsNullOrWhiteSpace(scene)) TryLoadSources(scene);
            };

            textBoxSourceManual = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(110, 120),
                Size = new System.Drawing.Size(280, 25)
            };
            textBoxSourceManual.PlaceholderText = "Source name (manually if needed)";

            labelMode = new System.Windows.Forms.Label
            {
                Text = "Action:",
                Location = new System.Drawing.Point(20, 160),
                Size = new System.Drawing.Size(80, 20),
                ForeColor = System.Drawing.Color.White
            };
            comboBoxMode = new System.Windows.Forms.ComboBox
            {
                Location = new System.Drawing.Point(110, 157),
                Size = new System.Drawing.Size(150, 25),
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            };
            comboBoxMode.Items.AddRange(new object[] { "toggle", "show", "hide" });

            labelHint = new System.Windows.Forms.Label
            {
                Text = ObsConnectionManager.IsConnected
                    ? "OBS connected – select a scene to load its sources."
                    : "OBS not connected – enter names manually.",
                Location = new System.Drawing.Point(20, 200),
                Size = new System.Drawing.Size(380, 50),
                ForeColor = ObsConnectionManager.IsConnected
                    ? System.Drawing.Color.LightGreen
                    : System.Drawing.Color.Orange
            };

            this.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                labelScene,  comboBoxScene,  buttonRefreshScenes, textBoxSceneManual,
                labelSource, comboBoxSource, buttonRefreshSources, textBoxSourceManual,
                labelMode,   comboBoxMode,
                labelHint
            });
        }

        private async void TryLoadScenes()
        {
            if (!ObsConnectionManager.IsConnected) return;
            try
            {
                var resp = await ObsConnectionManager.InvokeAsync("GetSceneList");
                var scenes = resp?["responseData"]?["scenes"] as Newtonsoft.Json.Linq.JArray;
                if (scenes == null) return;

                comboBoxScene.Items.Clear();
                foreach (var scene in scenes)
                {
                    string name = scene["sceneName"]?.ToString();
                    if (!string.IsNullOrEmpty(name)) comboBoxScene.Items.Add(name);
                }

                labelHint.Text = comboBoxScene.Items.Count + " scene(s) loaded – select one to load sources.";
                labelHint.ForeColor = System.Drawing.Color.LightGreen;
            }
            catch
            {
                labelHint.Text = "Scenes could not be loaded – enter manually.";
                labelHint.ForeColor = System.Drawing.Color.Orange;
            }
        }

        private async void TryLoadSources(string sceneName)
        {
            labelHint.Text = "Loading sources…";
            labelHint.ForeColor = System.Drawing.Color.Gray;

            var result = await ObsRawSceneItemFetcher.GetSourceEntriesAsync(sceneName);
            var entries = result.Entries;

            comboBoxSource.Items.Clear();
            _sourceEntries = entries;

            if (entries.Count == 0)
            {
                labelHint.Text = result.Warnings.Count > 0
                    ? "No sources found. " + string.Join(" ", result.Warnings)
                    : "No sources found – enter name manually.";
                labelHint.ForeColor = System.Drawing.Color.Orange;
                return;
            }

            foreach (var e in entries)
                comboBoxSource.Items.Add(e.DisplayName);

            string savedSource = textBoxSourceManual.Text;
            if (!string.IsNullOrEmpty(savedSource))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].SourceName == savedSource)
                    {
                        comboBoxSource.SelectedIndex = i;
                        break;
                    }
                }
            }

            int groups = 0;
            int children = 0;
            foreach (var e in entries)
            {
                if (e.GroupName != null) children++;
                else if (e.DisplayName.StartsWith("[Group]")) groups++;
            }

            string hint = entries.Count + " source(s) loaded (" + result.TopLevelItemCount + " top-level)";
            if (groups > 0) hint += $", incl. {groups} group(s) with {children} nested item(s)";
            hint += ".";

            if (result.Warnings.Count > 0 && ObsConnectionManager.Plugin != null)
            {
                MacroDeckLogger.Info(ObsConnectionManager.Plugin,
                    $"[Tether] Source diagnostics for scene \"{sceneName}\":\n" + string.Join("\n", result.Warnings));
            }

            if (result.Warnings.Count > 0)
            {
                hint += "  \u26a0 Details in the Macro Deck log.";
                labelHint.Text = hint;
                labelHint.ForeColor = System.Drawing.Color.Orange;
            }
            else
            {
                labelHint.Text = hint;
                labelHint.ForeColor = System.Drawing.Color.LightGreen;
            }
        }

        public override bool OnActionSave()
        {
            string scene = textBoxSceneManual.Text?.Trim();
            string source = textBoxSourceManual.Text?.Trim();
            if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(source)) return false;

            string group = null;
            int idx = comboBoxSource.SelectedIndex;
            if (idx >= 0 && idx < _sourceEntries.Count)
                group = _sourceEntries[idx].GroupName;

            string mode = comboBoxMode.SelectedItem?.ToString() ?? "toggle";
            var config = new JObject
            {
                ["scene"] = scene,
                ["source"] = source,
                ["group"] = group,
                ["mode"] = mode
            };

            _action.ConfigurationSummary = group != null
                ? $"{mode}: {source} (in group {group}) in {scene}"
                : $"{mode}: {source} in {scene}";
            _action.Configuration = config.ToString();
            return true;
        }
    }
}