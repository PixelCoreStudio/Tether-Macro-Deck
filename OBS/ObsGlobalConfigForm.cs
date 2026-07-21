using System.Drawing;
using System.Windows.Forms;

namespace VoidCore.Tether.OBS
{
    public class ObsGlobalConfigForm : Form
    {
        private TextBox textBoxHost;
        private NumericUpDown numericPort;
        private TextBox textBoxPassword;
        private CheckBox checkBoxAutoConnect;
        private Button buttonTest;
        private Button buttonSave;
        private Label labelStatus;

        public ObsGlobalConfigForm()
        {
            this.Text = "Tether - OBS Configuration";
            this.Size = new Size(420, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            var labelHost = new Label { Text = "Host:", Location = new Point(20, 20), Size = new Size(80, 20) };
            textBoxHost = new TextBox { Location = new Point(110, 17), Size = new Size(270, 25) };

            var labelPort = new Label { Text = "Port:", Location = new Point(20, 60), Size = new Size(80, 20) };
            numericPort = new NumericUpDown { Location = new Point(110, 57), Size = new Size(100, 25), Minimum = 1, Maximum = 65535, Value = 4455 };

            var labelPassword = new Label { Text = "Password:", Location = new Point(20, 100), Size = new Size(80, 20) };
            textBoxPassword = new TextBox { Location = new Point(110, 97), Size = new Size(270, 25), UseSystemPasswordChar = true };

            checkBoxAutoConnect = new CheckBox
            {
                Text = "Auto connect on the start of Macro deck 2",
                Location = new Point(20, 140),
                Size = new Size(360, 25)
            };

            buttonTest = new Button { Text = "Connect Now", Location = new Point(20, 180), Size = new Size(150, 30) };
            buttonTest.Click += (s, e) => TestConnect();

            labelStatus = new Label
            {
                Location = new Point(180, 185),
                Size = new Size(200, 20),
                Text = ObsConnectionManager.IsConnected ? "✓ Connected" : "Not connected",
                ForeColor = ObsConnectionManager.IsConnected ? Color.Green : Color.Gray
            };

            buttonSave = new Button { Text = "Save & close", Location = new Point(20, 220), Size = new Size(180, 30) };
            buttonSave.Click += (s, e) => SaveAndClose();

            this.Controls.AddRange(new Control[]
            {
                labelHost, textBoxHost,
                labelPort, numericPort,
                labelPassword, textBoxPassword,
                checkBoxAutoConnect,
                buttonTest, labelStatus,
                buttonSave
            });

            LoadValues();
        }

        private void LoadValues()
        {
            var config = ObsConnectionManager.LoadConfig();
            textBoxHost.Text = config.Host;
            numericPort.Value = config.Port;
            textBoxPassword.Text = config.Password;
            checkBoxAutoConnect.Checked = config.AutoConnect;
        }

        private void SaveAndClose()
        {
            ObsConnectionManager.SaveConfig(textBoxHost.Text.Trim(), (int)numericPort.Value, textBoxPassword.Text, checkBoxAutoConnect.Checked);
            if (checkBoxAutoConnect.Checked)
                ObsConnectionManager.Reconnect();
            this.Close();
        }

        private void TestConnect()
        {
            ObsConnectionManager.SaveConfig(textBoxHost.Text.Trim(), (int)numericPort.Value, textBoxPassword.Text, checkBoxAutoConnect.Checked);
            ObsConnectionManager.Reconnect();
            labelStatus.Text = "Connectiontry …";
            labelStatus.ForeColor = Color.Orange;
        }
    }
}