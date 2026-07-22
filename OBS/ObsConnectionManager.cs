using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private static MacroDeckPlugin _plugin;
        private static Timer _reconnectTimer;
        private static Timer _statusTimer;

        private static readonly object _lock = new object();
        private static bool _connected = false;
        private static bool _updating = false;
        private static bool _connecting = false;

        private static ClientWebSocket _ws;
        private static CancellationTokenSource _loopCts;

        private static readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>>
            _pending = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();

        public static bool IsConnected => _connected;
        public static MacroDeckPlugin Plugin => _plugin;

        public static void Initialize(MacroDeckPlugin plugin)
        {
            _plugin = plugin;

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex && _plugin != null)
                    MacroDeckLogger.Error(_plugin, "OBS: Unhandled background exception: {0}", ex.Message);
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                if (_plugin != null)
                    MacroDeckLogger.Error(_plugin, "OBS: Unobserved task exception (suppressed): {0}", e.Exception.Message);
                e.SetObserved();
            };

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

        public static void Reconnect()
        {
            var cfg = LoadConfig();
            Disconnect();
            Connect(cfg.Host, cfg.Port, cfg.Password);
        }

        public static void Connect(string host, int port, string password)
        {
            lock (_lock)
            {
                if (_connected || _connecting) return;
                _connecting = true;
            }

            var t = new Thread(() => ConnectLoop(host, port, password));
            t.IsBackground = true;
            t.Name = "OBS-WebSocket";
            t.Start();
        }

        public static void Disconnect()
        {
            CancellationTokenSource oldCts;
            ClientWebSocket oldWs;

            lock (_lock)
            {
                _connected = false;
                _connecting = false;
                oldCts = _loopCts;
                oldWs = _ws;
                _loopCts = null;
                _ws = null;
            }

            try { oldCts?.Cancel(); } catch { }
            try { oldCts?.Dispose(); } catch { }
            try { oldWs?.Abort(); } catch { }
            try { oldWs?.Dispose(); } catch { }

            foreach (var kv in _pending)
            {
                kv.Value.TrySetCanceled();
                _pending.TryRemove(kv.Key, out _);
            }
        }

        public static void Send(string requestType, object requestData = null)
        {
            _ = SendAsync(requestType, requestData);
        }

        public static async Task<JObject> InvokeAsync(string requestType, object requestData = null)
        {
            if (!_connected) return null;
            string reqId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[reqId] = tcs;
            try
            {
                var payload = new JObject
                {
                    ["op"] = 6,
                    ["d"] = new JObject
                    {
                        ["requestType"] = requestType,
                        ["requestId"] = reqId,
                        ["requestData"] = requestData == null ? null : JObject.FromObject(requestData)
                    }
                };
                await SendRawAsync(payload, CancellationToken.None).ConfigureAwait(false);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                cts.Token.Register(() => tcs.TrySetCanceled());
                return await tcs.Task.ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
            finally
            {
                _pending.TryRemove(reqId, out _);
            }
        }

        private static async Task SendAsync(string requestType, object requestData)
        {
            if (!_connected) return;
            try
            {
                string reqId = Guid.NewGuid().ToString("N");
                var payload = new JObject
                {
                    ["op"] = 6,
                    ["d"] = new JObject
                    {
                        ["requestType"] = requestType,
                        ["requestId"] = reqId,
                        ["requestData"] = requestData == null ? null : JObject.FromObject(requestData)
                    }
                };
                await SendRawAsync(payload, CancellationToken.None).ConfigureAwait(false);
            }
            catch { }
        }

        private static async Task SendRawAsync(JObject obj, CancellationToken ct)
        {
            ClientWebSocket ws;
            lock (_lock) { ws = _ws; }
            if (ws == null || ws.State != WebSocketState.Open) return;

            byte[] bytes = Encoding.UTF8.GetBytes(obj.ToString(Formatting.None));
            await _sendSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private static readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);

        private static void ConnectLoop(string host, int port, string password)
        {
            try
            {
                ConnectLoopAsync(host, port, password).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (_plugin != null)
                    MacroDeckLogger.Error(_plugin, "OBS: ConnectLoop fatal error (ignored): {0}", ex.Message);
            }
            finally
            {
                lock (_lock)
                {
                    _connected = false;
                    _connecting = false;
                }
            }
        }

        private static async Task ConnectLoopAsync(string host, int port, string password)
        {
            var cts = new CancellationTokenSource();
            var ws = new ClientWebSocket();

            lock (_lock)
            {
                _loopCts = cts;
                _ws = ws;
            }

            try
            {
                using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                await ws.ConnectAsync(new Uri($"ws://{host}:{port}"), connectCts.Token)
                        .ConfigureAwait(false);

                await HandshakeAsync(ws, password, cts.Token).ConfigureAwait(false);

                lock (_lock) { _connected = true; }
                if (_plugin != null)
                    MacroDeckLogger.Information(_plugin, "OBS: Connected.");

                await ReceiveLoopAsync(ws, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (_plugin != null)
                    MacroDeckLogger.Error(_plugin, "OBS: Connection error: {0}", ex.Message);
            }
            finally
            {
                lock (_lock)
                {
                    _connected = false;
                    _connecting = false;
                    if (ReferenceEquals(_ws, ws)) { _ws = null; }
                    if (ReferenceEquals(_loopCts, cts)) { _loopCts = null; }
                }

                try { ws.Abort(); } catch { }
                try { ws.Dispose(); } catch { }
                try { cts.Dispose(); } catch { }
            }
        }

        private static async Task HandshakeAsync(ClientWebSocket ws, string password, CancellationToken ct)
        {
            var hello = await ReceiveMessageAsync(ws, ct).ConfigureAwait(false);

            var identifyData = new JObject
            {
                ["rpcVersion"] = 1,
                ["eventSubscriptions"] = 0
            };

            var authInfo = hello["d"]?["authentication"];
            if (authInfo != null && !string.IsNullOrEmpty(password))
            {
                identifyData["authentication"] = BuildAuth(
                    password,
                    authInfo["challenge"]!.ToString(),
                    authInfo["salt"]!.ToString());
            }

            await ws.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(
                    new JObject { ["op"] = 1, ["d"] = identifyData }.ToString(Formatting.None))),
                WebSocketMessageType.Text, true, ct).ConfigureAwait(false);

            var identified = await ReceiveMessageAsync(ws, ct).ConfigureAwait(false);
            if ((int?)identified["op"] != 2)
                throw new InvalidOperationException("OBS handshake failed: expected op 2 (Identified).");
        }

        private static async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                JObject msg;
                try
                {
                    msg = await ReceiveMessageAsync(ws, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch { break; }

                if ((int?)msg["op"] == 7)
                {
                    string reqId = msg["d"]?["requestId"]?.ToString();
                    if (reqId != null && _pending.TryRemove(reqId, out var tcs))
                        tcs.TrySetResult(msg["d"] as JObject);
                }
            }
        }

        private static async Task<JObject> ReceiveMessageAsync(ClientWebSocket ws, CancellationToken ct)
        {
            var sb = new StringBuilder();
            var buf = new byte[65536];
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                    throw new OperationCanceledException("OBS closed the connection.");
                sb.Append(Encoding.UTF8.GetString(buf, 0, result.Count));
            }
            while (!result.EndOfMessage);

            return JObject.Parse(sb.ToString());
        }

        private static string BuildAuth(string password, string challenge, string salt)
        {
            using var sha = SHA256.Create();
            string secret = Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt)));
            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(secret + challenge)));
        }

        private static void TryAutoConnect()
        {
            lock (_lock)
            {
                if (_connected || _connecting) return;
            }
            try
            {
                var cfg = LoadConfig();
                if (!cfg.AutoConnect) return;
                Connect(cfg.Host, cfg.Port, cfg.Password);
            }
            catch { }
        }

        private static async void UpdateStatusVariables()
        {
            if (_updating || !_connected || _plugin == null) return;
            _updating = true;
            try
            {
                var sceneResp = await InvokeAsync("GetCurrentProgramScene").ConfigureAwait(false);
                if (sceneResp != null)
                {
                    string scene = sceneResp["responseData"]?["currentProgramSceneName"]?.ToString() ?? "";
                    VariableManager.SetValue("obs_current_scene", scene, VariableType.String, _plugin, (string[])null);
                }

                var streamResp = await InvokeAsync("GetStreamStatus").ConfigureAwait(false);
                if (streamResp != null)
                {
                    bool active = streamResp["responseData"]?["outputActive"]?.Value<bool>() ?? false;
                    VariableManager.SetValue("obs_streaming", active ? "true" : "false", VariableType.String, _plugin, (string[])null);
                }

                var recordResp = await InvokeAsync("GetRecordStatus").ConfigureAwait(false);
                if (recordResp != null)
                {
                    bool active = recordResp["responseData"]?["outputActive"]?.Value<bool>() ?? false;
                    bool paused = recordResp["responseData"]?["outputPaused"]?.Value<bool>() ?? false;
                    VariableManager.SetValue("obs_recording", active ? "true" : "false", VariableType.String, _plugin, (string[])null);
                    VariableManager.SetValue("obs_recording_paused", paused ? "true" : "false", VariableType.String, _plugin, (string[])null);
                }
            }
            catch { }
            finally { _updating = false; }
        }
    }
}