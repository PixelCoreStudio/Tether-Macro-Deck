using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VoidCore.Tether.OBS
{
    public class SourceEntry
    {
        public string DisplayName { get; }

        public string SourceName { get; }

        public string GroupName { get; }

        public string EffectiveScene => GroupName ?? string.Empty;

        public SourceEntry(string displayName, string sourceName, string groupName = null)
        {
            DisplayName = displayName;
            SourceName = sourceName;
            GroupName = groupName;
        }
    }

    public class SourceFetchResult
    {
        public List<SourceEntry> Entries { get; } = new List<SourceEntry>();
        public List<string> Warnings { get; } = new List<string>();
        public int TopLevelItemCount { get; set; }
    }

    internal static class ObsRawSceneItemFetcher
    {
        public static async Task<SourceFetchResult> GetSourceEntriesAsync(string sceneName)
        {
            var cfg = ObsConnectionManager.LoadConfig();
            return await GetSourceEntriesAsync(cfg.Host, cfg.Port, cfg.Password, sceneName);
        }

        public static async Task<SourceFetchResult> GetSourceEntriesAsync(
            string host, int port, string password, string sceneName)
        {
            var result = new SourceFetchResult();

            using var overallCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var ws = new ClientWebSocket();

            CancellationToken PerRequestToken(TimeSpan timeout)
            {
                var linked = CancellationTokenSource.CreateLinkedTokenSource(overallCts.Token);
                linked.CancelAfter(timeout);
                return linked.Token;
            }

            try
            {
                var connectTimeout = TimeSpan.FromSeconds(8);
                await ws.ConnectAsync(new Uri($"ws://{host}:{port}"), PerRequestToken(connectTimeout));

                await HandshakeAsync(ws, password, PerRequestToken(connectTimeout));

                var sceneItems = await RequestSceneItemsAsync(
                    ws, "GetSceneItemList", sceneName, PerRequestToken(TimeSpan.FromSeconds(10)));
                result.TopLevelItemCount = sceneItems.Count;

                var rawDump = new List<string>();
                for (int i = 0; i < sceneItems.Count; i++)
                {
                    var raw = sceneItems[i];
                    string rawName = raw["sourceName"]?.ToString();
                    string rawIsGroup = raw["isGroup"]?.ToString() ?? "<missing>";
                    rawDump.Add($"[{i}] name={(rawName == null ? "<null>" : $"\"{rawName}\"")} isGroup={rawIsGroup}");
                }
                result.Warnings.Add("RAW Top-Level: " + string.Join("; ", rawDump));

                await CollectItemsRecursiveAsync(ws, sceneItems, parentGroupName: null, depth: 0, result);

                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Fetch aborted: {ex.GetType().Name}: {ex.Message}");
            }

            return result;

            async Task CollectItemsRecursiveAsync(
                ClientWebSocket socket, JArray items, string parentGroupName, int depth, SourceFetchResult res)
            {
                string indent = depth == 0 ? "" : new string(' ', depth * 2) + "\u2514 ";

                foreach (var item in items)
                {
                    string name = item["sourceName"]?.ToString();
                    bool isGroup = item["isGroup"]?.ToObject<bool?>() ?? false;

                    if (string.IsNullOrWhiteSpace(name)) continue;

                    if (!isGroup)
                    {
                        string display = depth == 0
                            ? name
                            : $"{indent}{name}  (in {parentGroupName})";
                        res.Entries.Add(new SourceEntry(display, name, parentGroupName));
                    }
                    else
                    {
                        string display = depth == 0
                            ? $"[Group] {name}"
                            : $"{indent}[Group] {name}  (in {parentGroupName})";
                        res.Entries.Add(new SourceEntry(display, name, parentGroupName));

                        try
                        {
                            var groupItems = await RequestSceneItemsAsync(
                                socket, "GetGroupSceneItemList", name, PerRequestToken(TimeSpan.FromSeconds(10)));

                            var rawGroupDump = new List<string>();
                            for (int i = 0; i < groupItems.Count; i++)
                            {
                                var raw = groupItems[i];
                                string rawName = raw["sourceName"]?.ToString();
                                rawGroupDump.Add(rawName == null ? "<null>" : $"\"{rawName}\"");
                            }
                            res.Warnings.Add($"RAW Group \"{name}\": {groupItems.Count} raw element(s) [{string.Join(", ", rawGroupDump)}]");

                            if (groupItems.Count == 0)
                            {
                                res.Warnings.Add($"Group \"{name}\" returned 0 elements (possibly empty or response missed).");
                            }

                            await CollectItemsRecursiveAsync(socket, groupItems, name, depth + 1, res);
                        }
                        catch (Exception ex)
                        {
                            res.Warnings.Add($"Group \"{name}\" could not be loaded: {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private static async Task HandshakeAsync(ClientWebSocket ws, string password, CancellationToken ct)
        {
            var hello = await ReceiveJsonAsync(ws, ct);

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
            await SendJsonAsync(ws, new JObject { ["op"] = 1, ["d"] = identifyData }, ct);

            var identified = await ReceiveJsonAsync(ws, ct);
            if ((int?)identified["op"] != 2)
                throw new InvalidOperationException("OBS did not send Identified.");
        }

        private static async Task<JArray> RequestSceneItemsAsync(
            ClientWebSocket ws, string requestType, string targetName, CancellationToken ct)
        {
            string reqId = Guid.NewGuid().ToString("N");
            await SendJsonAsync(ws, new JObject
            {
                ["op"] = 6,
                ["d"] = new JObject
                {
                    ["requestType"] = requestType,
                    ["requestId"] = reqId,
                    ["requestData"] = new JObject { ["sceneName"] = targetName }
                }
            }, ct);

            while (!ct.IsCancellationRequested)
            {
                var msg = await ReceiveJsonAsync(ws, ct);
                if ((int?)msg["op"] == 7 && msg["d"]?["requestId"]?.ToString() == reqId)
                {
                    bool success = msg["d"]?["requestStatus"]?["result"]?.Value<bool>() ?? true;
                    if (!success)
                    {
                        string comment = msg["d"]?["requestStatus"]?["comment"]?.ToString() ?? "unknown error";
                        throw new InvalidOperationException($"{requestType} failed: {comment}");
                    }
                    return (JArray)(msg["d"]?["responseData"]?["sceneItems"] ?? new JArray());
                }
            }

            throw new TimeoutException($"{requestType} for \"{targetName}\" did not respond in time.");
        }

        private static string BuildAuth(string password, string challenge, string salt)
        {
            using var sha = SHA256.Create();
            string secret = Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt)));
            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(secret + challenge)));
        }

        private static async Task SendJsonAsync(ClientWebSocket ws, JObject obj, CancellationToken ct)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(obj.ToString(Newtonsoft.Json.Formatting.None));
            await ws.SendAsync(new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text, endOfMessage: true, ct);
        }

        private static async Task<JObject> ReceiveJsonAsync(ClientWebSocket ws, CancellationToken ct)
        {
            var sb = new StringBuilder();
            var buffer = new byte[65536];
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            return JObject.Parse(sb.ToString());
        }
    }
}