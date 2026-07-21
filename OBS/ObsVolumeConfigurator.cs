using Newtonsoft.Json.Linq;
using ObsWebSocket.Net.Protocol.Enums;
using ObsWebSocket.Net.Protocol.Requests;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;

namespace VoidCore.Tether.OBS
{
    public class ObsVolumeConfigurator : ActionConfigControl
    {
        private readonly ObsVolumeAction _action;

        private System.Windows.Forms.ComboBox comboBoxSource;
        private System.Windows.Forms.TextBox textBoxSource;
        private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.ComboBox comboBoxMode;
        private System.Windows.Forms.NumericUpDown numericStep;
        private System.Windows.Forms.NumericUpDown numericTarget;
        private System.Windows.Forms.Label labelSource, labelMode, labelStep, labelTarget;

        public ObsVolumeConfigurator(ObsVolumeAction action, ActionConfigurator actionConfigurator)
        {
            _action = action;
            InitializeComponents();
            TryLoadInputs();

            if (!string.IsNullOrEmpty(_action.Configuration))
            {
                try
                {
                    var config = JObject.Parse(_action.Configuration);
                    textBoxSource.Text = config["source"]?.ToString() ?? "";
                    string mode = config["mode"]?.ToString() ?? "increase";
                    comboBoxMode.SelectedItem = mode;
                    numericStep.Value = config["step"]?.ToObject<decimal>() ?? 5m;
                    numericTarget.Value = config["target"]?.ToObject<decimal>() ?? -20m;
                }
                catch { }
            }
            if (comboBoxMode.SelectedIndex < 0) comboBoxMode.SelectedIndex = 0;
            UpdateFieldVisibility();
        }

        private void InitializeComponents()
        {
            this.Size = new System.Drawing.Size(400, 230);

            labelSource = new System.Windows.Forms.Label { Text = "Sources:", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(80, 20), ForeColor = System.Drawing.Color.White };
            comboBoxSource = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(110, 17), Size = new System.Drawing.Size(200, 25), DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
            comboBoxSource.SelectedIndexChanged += (s, e) => { if (comboBoxSource.SelectedItem != null) textBoxSource.Text = comboBoxSource.SelectedItem.ToString(); };
            buttonRefresh = new System.Windows.Forms.Button { Text = "Refresh", Location = new System.Drawing.Point(318, 17), Size = new System.Drawing.Size(30, 25) };
            buttonRefresh.Click += (s, e) => TryLoadInputs();
            textBoxSource = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(110, 50), Size = new System.Drawing.Size(270, 25) };
            textBoxSource.PlaceholderText = "Source name manually";

            labelMode = new System.Windows.Forms.Label { Text = "Action:", Location = new System.Drawing.Point(20, 90), Size = new System.Drawing.Size(80, 20), ForeColor = System.Drawing.Color.White };
            comboBoxMode = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(110, 87), Size = new System.Drawing.Size(200, 25), DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
            comboBoxMode.Items.AddRange(new object[] { "increase", "decrease", "set", "mute", "unmute", "togglemute" });
            comboBoxMode.SelectedIndexChanged += (s, e) => UpdateFieldVisibility();

            labelStep = new System.Windows.Forms.Label { Text = "Step (dB):", Location = new System.Drawing.Point(20, 130), Size = new System.Drawing.Size(90, 20), ForeColor = System.Drawing.Color.White };
            numericStep = new System.Windows.Forms.NumericUpDown { Location = new System.Drawing.Point(110, 127), Size = new System.Drawing.Size(100, 25), Minimum = 1, Maximum = 100, Value = 5 };

            labelTarget = new System.Windows.Forms.Label { Text = "Goal (dB):", Location = new System.Drawing.Point(20, 170), Size = new System.Drawing.Size(90, 20), ForeColor = System.Drawing.Color.White };
            numericTarget = new System.Windows.Forms.NumericUpDown { Location = new System.Drawing.Point(110, 167), Size = new System.Drawing.Size(100, 25), Minimum = -100, Maximum = 0, Value = -20 };

            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                labelSource, comboBoxSource, buttonRefresh, textBoxSource,
                labelMode, comboBoxMode,
                labelStep, numericStep,
                labelTarget, numericTarget
            });
        }

        private void UpdateFieldVisibility()
        {
            string mode = comboBoxMode.SelectedItem?.ToString() ?? "increase";
            numericStep.Visible = labelStep.Visible = (mode == "increase" || mode == "decrease");
            numericTarget.Visible = labelTarget.Visible = (mode == "set");
        }

        private async void TryLoadInputs()
        {
            if (!ObsConnectionManager.IsConnected) return;
            try
            {
                var response = await ObsConnectionManager.Instance.Invoke<GetInputListResponse>(RequestType.GetInputList);
                if (response?.Inputs == null) return;
                comboBoxSource.Items.Clear();
                foreach (var input in response.Inputs)
                    comboBoxSource.Items.Add(input.InputName);
            }
            catch { }
        }

        public override bool OnActionSave()
        {
            string source = textBoxSource.Text?.Trim();
            if (string.IsNullOrWhiteSpace(source)) return false;

            string mode = comboBoxMode.SelectedItem?.ToString() ?? "increase";
            var config = new JObject
            {
                ["source"] = source,
                ["mode"] = mode,
                ["step"] = (double)numericStep.Value,
                ["target"] = (double)numericTarget.Value
            };
            _action.ConfigurationSummary = mode == "set"
                ? $"{source}: auf {numericTarget.Value} dB setzen"
                : $"{source}: {mode}";
            _action.Configuration = config.ToString();
            return true;
        }
    }
}