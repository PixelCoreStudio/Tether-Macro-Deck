using System;
using System.Collections.Generic;
using SuchByte.MacroDeck.Plugins;

namespace VoidCore.Tether
{
    public class Main : MacroDeckPlugin
    {
        // Dieses Event wird aufgerufen, wenn Macro Deck das Plugin aktiviert.
        // Alle Metadaten (Name, Entwickler, Beschreibung) kommen jetzt rein aus der plugin.json!
        public override void Enable()
        {
            // Hier registrieren wir unsere HTTP-Aktion
            this.Actions = new List<PluginAction>
            {
                new HttpRequestAction()
            };
        }
    }
}