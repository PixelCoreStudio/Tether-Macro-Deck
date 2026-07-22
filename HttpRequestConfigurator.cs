using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace VoidCore.Tether
{
    public class HttpRequestConfigurator : ActionConfigControl
    {
        private readonly HttpRequestAction _macroDeckAction;

        private System.Windows.Forms.TextBox textBoxUrl;
        private System.Windows.Forms.ComboBox comboBoxMethod;
        private System.Windows.Forms.Label labelUrl;
        private System.Windows.Forms.Label labelMethod;

        public HttpRequestConfigurator(HttpRequestAction macroDeckAction, ActionConfigurator actionConfigurator)
        {
            this._macroDeckAction = macroDeckAction;

            InitializeCustomComponents();

            if (!string.IsNullOrEmpty(this._macroDeckAction.Configuration))
            {
                try
                {
                    var config = JObject.Parse(this._macroDeckAction.Configuration);
                    textBoxUrl.Text = config["url"]?.ToString() ?? "";
                    comboBoxMethod.SelectedItem = config["method"]?.ToString() ?? "GET";
                }
                catch { }
            }
            else
            {
                comboBoxMethod.SelectedIndex = 0;
            }
        }

        private void InitializeCustomComponents()
        {
            this.Size = new Size(400, 150);

            labelMethod = new System.Windows.Forms.Label
            {
                Text = "Method:",
                Location = new Point(20, 20),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };

            comboBoxMethod = new System.Windows.Forms.ComboBox
            {
                Location = new Point(110, 17),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxMethod.Items.AddRange(new object[] { "GET", "POST" });

            labelUrl = new System.Windows.Forms.Label
            {
                Text = "URL:",
                Location = new Point(20, 60),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };

            textBoxUrl = new System.Windows.Forms.TextBox
            {
                Location = new Point(110, 57),
                Size = new Size(270, 25)
            };

            this.Controls.Add(labelMethod);
            this.Controls.Add(comboBoxMethod);
            this.Controls.Add(labelUrl);
            this.Controls.Add(textBoxUrl);
        }

        public override bool OnActionSave()
        {
            if (string.IsNullOrWhiteSpace(textBoxUrl.Text))
            {
                return false;
            }

            try
            {
                JObject configuration = new JObject();
                configuration["url"] = textBoxUrl.Text;
                configuration["method"] = comboBoxMethod.SelectedItem?.ToString() ?? "GET";

                this._macroDeckAction.ConfigurationSummary = $"{configuration["method"]} -> {configuration["url"]}";
                this._macroDeckAction.Configuration = configuration.ToString();
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
