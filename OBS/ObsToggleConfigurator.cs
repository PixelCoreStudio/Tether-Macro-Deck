using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;

namespace VoidCore.Tether.OBS
{
    /// <summary>
    /// Geteilter Konfigurator für Stream- und Aufnahme-Toggle-Actions.
    /// </summary>
    public class ObsToggleConfigurator : ActionConfigControl
    {
        private readonly PluginAction _action;
        private readonly string _label;
        private System.Windows.Forms.ComboBox comboBoxMode;
        private System.Windows.Forms.Label labelMode;

        public ObsToggleConfigurator(PluginAction action, ActionConfigurator actionConfigurator, string label, bool includePause = false)
        {
            _action = action;
            _label = label;
            InitializeComponents(includePause);

            if (!string.IsNullOrEmpty(_action.Configuration))
            {
                try
                {
                    var config = JObject.Parse(_action.Configuration);
                    string mode = config["mode"]?.ToString() ?? "toggle";
                    comboBoxMode.SelectedItem = mode;
                }
                catch { }
            }

            if (comboBoxMode.SelectedIndex < 0)
                comboBoxMode.SelectedIndex = 0;
        }

        private void InitializeComponents(bool includePause)
        {
            this.Size = new System.Drawing.Size(400, 100);

            labelMode = new System.Windows.Forms.Label
            {
                Text = $"{_label}:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(80, 20),
                ForeColor = System.Drawing.Color.White
            };

            comboBoxMode = new System.Windows.Forms.ComboBox
            {
                Location = new System.Drawing.Point(110, 17),
                Size = new System.Drawing.Size(150, 25),
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            };
            comboBoxMode.Items.AddRange(new object[] { "toggle", "start", "stop" });
            if (includePause)
            {
                comboBoxMode.Items.Add("pause");
                comboBoxMode.Items.Add("resume");
            }

            this.Controls.AddRange(new System.Windows.Forms.Control[] { labelMode, comboBoxMode });
        }

        public override bool OnActionSave()
        {
            string mode = comboBoxMode.SelectedItem?.ToString() ?? "toggle";
            var config = new JObject { ["mode"] = mode };
            _action.ConfigurationSummary = $"{_label}: {mode}";
            _action.Configuration = config.ToString();
            return true;
        }
    }
}
