using System;
using System.Globalization;
using System.Threading;
using ObsWebSocket.Net;
using ObsWebSocket.Net.Protocol.Enums;
using ObsWebSocket.Net.Protocol.Requests;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.Variables;

namespace VoidCore.Tether.OBS
{
    public static class ObsConnectionManager
    {
        private const string KeyHost = "obs_host";
        private const string KeyPort = "obs_port";
        private const string KeyPassword = "obs_password";
        private const string KeyAutoConnect = "obs_autoconnect";

        private static ObsWebSocketClient _client;
        private static readonly object _lock = new object();
        private static bool _connected = false;
        private static bool _updating = false;

        private static MacroDeckPlugin _plugin;
        private static Timer _reconnectTimer;
        private static Timer _statusTimer;

        public static ObsWebSocketClient Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_client == null)
                        _client = new ObsWebSocketClient();
                    return _client;
                }
            }
        }

        public static bool IsConnected => _connected;

        public static void Initialize(MacroDeckPlugin plugin)
        {
            _plugin = plugin;

            _reconnectTimer = new Timer(_ => TryAutoConnect(), null, 0, 10000);

            _statusTimer = new Timer(_ => UpdateStatusVariables(), null, 4000, 4000);
        }

        public static (string Host, int Port, string Password, bool AutoConnect) LoadConfig()
        {
            string host = PluginConfiguration.GetValue(_plugin, KeyHost);
            string portStr = PluginConfiguration.GetValue(_plugin, KeyPort);
            string password = PluginConfiguration.GetValue(_plugin, KeyPassword) ?? "";
            string autoStr = PluginConfiguration.GetValue(_plugin, KeyAutoConnect);

            if (string.IsNullOrWhiteSpace(host)) host = "localhost";
            int port = int.TryParse(portStr, out var p) ? p : 4455;
            bool autoConnect = autoStr != "false";

            return (host, port, password, autoConnect);
        }

        public static void SaveConfig(string host, int port, string password, bool autoConnect)
        {
            PluginConfiguration.SetValue(_plugin, KeyHost, host);
            PluginConfiguration.SetValue(_plugin, KeyPort, port.ToString(CultureInfo.InvariantCulture));
            PluginConfiguration.SetValue(_plugin, KeyPassword, password);
            PluginConfiguration.SetValue(_plugin, KeyAutoConnect, autoConnect ? "true" : "false");
        }

        private static void TryAutoConnect()
        {
            try
            {
                if (_connected) return;
                var config = LoadConfig();
                if (!config.AutoConnect) return;
                Connect(config.Host, config.Port, config.Password);
            }
            catch { }
        }

        public static void Reconnect()
        {
            var config = LoadConfig();
            Disconnect();
            Connect(config.Host, config.Port, config.Password);
        }

        public static void Connect(string host, int port, string password)
        {
            lock (_lock)
            {
                if (_connected) return;

                try
                {
                    _client = new ObsWebSocketClient(host, port, password);
                    _client.OnIdentified += () =>
                    {
                        _connected = true;
                        if (_plugin != null) MacroDeckLogger.Info(_plugin, "OBS: Connected.");
                    };
                    _client.OnClosed += () => { _connected = false; };
                    _client.OnConnectionFailed += (ex) => { _connected = false; };
                    _client.Connect();
                }
                catch (Exception ex)
                {
                    _connected = false;
                    if (_plugin != null) MacroDeckLogger.Error(_plugin, $"OBS: Connection Failed: {ex.Message}");
                }
            }
        }

        public static void Disconnect()
        {
            try
            {
                _client?.Close();
                _connected = false;
            }
            catch { }
        }

        private static async void UpdateStatusVariables()
        {
            if (_updating || !_connected || _plugin == null) return;
            _updating = true;

            try
            {
                try
                {
                    var sceneResponse = await Instance.Invoke<GetCurrentProgramSceneResponse>(RequestType.GetCurrentProgramScene);
                    if (sceneResponse != null)
                        VariableManager.SetValue("obs_current_scene", sceneResponse.CurrentProgramSceneName ?? "", VariableType.String, _plugin, false);
                }
                catch { }

                try
                {
                    var streamResponse = await Instance.Invoke<GetStreamStatusResponse>(RequestType.GetStreamStatus);
                    if (streamResponse != null)
                        VariableManager.SetValue("obs_streaming", streamResponse.OutputActive ? "true" : "false", VariableType.String, _plugin, false);
                }
                catch { }

                try
                {
                    var recordResponse = await Instance.Invoke<GetRecordStatusResponse>(RequestType.GetRecordStatus);
                    if (recordResponse != null)
                    {
                        VariableManager.SetValue("obs_recording", recordResponse.OutputActive ? "true" : "false", VariableType.String, _plugin, false);
                        VariableManager.SetValue("obs_recording_paused", recordResponse.OutputPaused ? "true" : "false", VariableType.String, _plugin, false);
                    }
                }
                catch { }

                try
                {
                    var inputsResponse = await Instance.Invoke<GetInputListResponse>(RequestType.GetInputList);
                    if (inputsResponse?.Inputs != null)
                    {
                        foreach (var input in inputsResponse.Inputs)
                        {
                            try
                            {
                                var muteResponse = await Instance.Invoke<GetInputMuteResponse>(
                                    RequestType.GetInputMute, new { inputName = input.InputName });
                                if (muteResponse != null)
                                    VariableManager.SetValue($"obs_mute_{input.InputName}", muteResponse.InputMuted ? "true" : "false", VariableType.String, _plugin, false);
                            }
                            catch { }

                            try
                            {
                                var volumeResponse = await Instance.Invoke<GetInputVolumeResponse>(
                                    RequestType.GetInputVolume, new { inputName = input.InputName });
                                if (volumeResponse != null)
                                    VariableManager.SetValue($"obs_volume_{input.InputName}",
                                        Math.Round(volumeResponse.InputVolumeDb, 1).ToString(CultureInfo.InvariantCulture),
                                        VariableType.String, _plugin, false);
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            finally
            {
                _updating = false;
            }
        }
    }
}