using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using System.Drawing;
using System.Windows.Forms;

namespace VoidCore.Tether.OBS
{
    public class ObsConnectConfigurator : ActionConfigControl
    {
        private readonly ObsConnectAction _action;

        private System.Windows.Forms.TextBox textBoxHost;
        private System.Windows.Forms.NumericUpDown numericPort;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Label labelHost;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.Button buttonTestConnect;
        private System.Windows.Forms.Label labelStatus;

        public ObsConnectConfigurator(ObsConnectAction action, ActionConfigurator actionConfigurator)
        {
            _action = action;
            InitializeComponents();

            if (!string.IsNullOrEmpty(_action.Configuration))
            {
                try
                {
                    var config = JObject.Parse(_action.Configuration);
                    textBoxHost.Text = config["host"]?.ToString() ?? "localhost";
                    numericPort.Value = config["port"]?.ToObject<int>() ?? 4455;
                    textBoxPassword.Text = config["password"]?.ToString() ?? "";
                }
                catch { }
            }
            else
            {
                textBoxHost.Text = "localhost";
                numericPort.Value = 4455;
            }
        }

        private void InitializeComponents()
        {
            this.Size = new Size(400, 230);

            labelHost = new System.Windows.Forms.Label { Text = "Host:", Location = new System.Drawing.Point(20, 20), Size = new Size(80, 20), ForeColor = System.Drawing.Color.White };
            textBoxHost = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(110, 17), Size = new Size(270, 25) };

            labelPort = new System.Windows.Forms.Label { Text = "Port:", Location = new System.Drawing.Point(20, 60), Size = new Size(80, 20), ForeColor = System.Drawing.Color.White };
            numericPort = new System.Windows.Forms.NumericUpDown { Location = new System.Drawing.Point(110, 57), Size = new Size(100, 25), Minimum = 1, Maximum = 65535, Value = 4455 };

            labelPassword = new System.Windows.Forms.Label { Text = "Password:", Location = new System.Drawing.Point(20, 100), Size = new Size(80, 20), ForeColor = System.Drawing.Color.White };
            textBoxPassword = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(110, 97), Size = new Size(270, 25), UseSystemPasswordChar = true };

            buttonTestConnect = new System.Windows.Forms.Button
            {
                Text = "Test Connection",
                Location = new System.Drawing.Point(110, 137),
                Size = new Size(150, 30)
            };
            buttonTestConnect.Click += (s, e) => TestConnection();

            labelStatus = new System.Windows.Forms.Label
            {
                Text = ObsConnectionManager.IsConnected ? "✓ Connected" : "Not connected",
                Location = new System.Drawing.Point(110, 175),
                Size = new Size(270, 20),
                ForeColor = ObsConnectionManager.IsConnected ? System.Drawing.Color.LightGreen : System.Drawing.Color.Gray
            };

            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                labelHost, textBoxHost,
                labelPort, numericPort,
                labelPassword, textBoxPassword,
                buttonTestConnect, labelStatus
            });
        }

        private void TestConnection()
        {
            try
            {
                ObsConnectionManager.Disconnect();
                ObsConnectionManager.Connect(textBoxHost.Text, (int)numericPort.Value, textBoxPassword.Text);
                labelStatus.Text = "✓ Connected";
                labelStatus.ForeColor = System.Drawing.Color.LightGreen;
            }
            catch
            {
                labelStatus.Text = "✗ Connection Failed";
                labelStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        public override bool OnActionSave()
        {
            var config = new JObject
            {
                ["host"] = textBoxHost.Text,
                ["port"] = (int)numericPort.Value,
                ["password"] = textBoxPassword.Text
            };
            _action.ConfigurationSummary = $"OBS @ {textBoxHost.Text}:{(int)numericPort.Value}";
            _action.Configuration = config.ToString();
            return true;
        }
    }
}
