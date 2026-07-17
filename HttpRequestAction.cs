using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.GUI;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace VoidCore.Tether
{
    public class HttpRequestAction : PluginAction
    {
        public override string Name => "HTTP Request";

        public override string Description => "Send a GET or POST Request to the given URL.";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new HttpRequestConfigurator(this, actionConfigurator);
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            if (string.IsNullOrEmpty(this.Configuration))
            {
                return;
            }

            try
            {
                var config = JObject.Parse(this.Configuration);
                string url = config["url"]?.ToString();
                string method = config["method"]?.ToString() ?? "GET";

                if (string.IsNullOrWhiteSpace(url)) return;

                Task.Run(async () =>
                {
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            HttpResponseMessage response;
                            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                            {
                                response = await client.PostAsync(url, null);
                            }
                            else
                            {
                                response = await client.GetAsync(url);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                });
            }
            catch
            {
            }
        }
    }
}
