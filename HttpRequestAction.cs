using Newtonsoft.Json.Linq;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.GUI.CustomControls; // WICHTIG für ActionConfigControl
using SuchByte.MacroDeck.GUI;               // WICHTIG für ActionConfigurator
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace VoidCore.Tether
{
    public class HttpRequestAction : PluginAction
    {
        // Name der Aktion im Macro Deck Menü
        public override string Name => "HTTP Request senden";

        // Beschreibung der Aktion
        public override string Description => "Sendet einen GET oder POST Request an eine angegebene URL.";

        // Verknüpft die Aktion mit dem Einstellungsfenster (Configurator)
        public override bool CanConfigure => true;

        // Übergibt das GUI-Fenster an Macro Deck: unser eigener Configurator mit URL-Textfeld
        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new HttpRequestConfigurator(this, actionConfigurator);
        }

        // Führt die eigentliche Aktion aus, wenn die Taste gedrückt wird
        public override void Trigger(string clientId, ActionButton actionButton)
        {
            if (string.IsNullOrEmpty(this.Configuration))
            {
                return; // Keine Konfiguration vorhanden
            }

            try
            {
                var config = JObject.Parse(this.Configuration);
                string url = config["url"]?.ToString();
                string method = config["method"]?.ToString() ?? "GET";

                if (string.IsNullOrWhiteSpace(url)) return;

                // Führt den HTTP-Request im Hintergrund aus, um Macro Deck nicht einzufrieren
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
                        // Fehlerbehandlung (z.B. ins Log schreiben, falls nötig)
                    }
                });
            }
            catch
            {
                // Ungültiges Konfigurationsformat geladen
            }
        }
    }
}