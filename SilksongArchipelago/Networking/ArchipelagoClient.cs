using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace SilksongArchipelago.Networking
{
    /// <summary>
    /// Manages the WebSocket connection to the Archipelago server.
    /// Handles connecting, reconnecting, sending locations, and receiving items.
    /// </summary>
    public class ArchipelagoClient
    {
        public event Action? OnConnected;
        public event Action<string>? OnConnectionFailed;
        public event Action<string>? OnDisconnected;
        public event Action<ItemInfo, int>? OnItemReceived;
        public event Action<string>? OnMessageReceived;

        private ArchipelagoSession? _session;
        private string _slotName = "";
        private string _password = "";
        private bool _isReconnecting = false;
        private bool _isLoggedIn = false;

        public bool IsConnected => (_session?.Socket.Connected ?? false) && _isLoggedIn;
        public int SlotIndex => _session?.ConnectionInfo.Slot ?? -1;
        public int TeamIndex => _session?.ConnectionInfo.Team ?? 0;
        public Dictionary<string, object> SlotData { get; private set; } = new();
        public Dictionary<long, ScoutedItemInfo> ScoutedLocations { get; private set; } = new();

        public string CurrentHost { get; private set; } = "localhost";
        public int CurrentPort { get; private set; } = 38281;
        public string SlotName => _slotName;
        public IReadOnlyList<ItemInfo>? AllReceivedItems => _session?.Items.AllItemsReceived;

        /// <summary>
        /// Connect using host and port asynchronously on a background task.
        /// </summary>
        public void Connect(string host, int port, string slotName, string? password = null)
        {
            CurrentHost = host;
            CurrentPort = port;
            Task.Run(async () =>
            {
                await ConnectAsync(host, port, slotName, password ?? "");
            });
        }

        /// <summary>
        /// Attempt to connect to an Archipelago server.
        /// </summary>
        public async Task<bool> ConnectAsync(string host, int port, string slotName, string password = "")
        {
            CurrentHost = host;
            CurrentPort = port;
            _slotName = slotName;
            _password = password;
            _isLoggedIn = false;

            try
            {
                string protocol = host.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) || host.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
                    ? ""
                    : (port == 443 || host.IndexOf("archipelago.gg", StringComparison.OrdinalIgnoreCase) >= 0 ? "wss://" : "ws://");
                string fullUrl = host.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) || host.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
                    ? host
                    : $"{protocol}{host}:{port}";

                SilksongArchipelagoPlugin.Log.LogInfo($"Creating session for {fullUrl}...");
                _session = ArchipelagoSessionFactory.CreateSession(new Uri(fullUrl));

                _session.Socket.ErrorReceived += (ex, message) =>
                {
                    SilksongArchipelagoPlugin.Log.LogError($"Archipelago Socket Error: {message} ({ex})");
                };

                _session.Socket.SocketClosed += (reason) =>
                {
                    SilksongArchipelagoPlugin.Log.LogWarning($"Archipelago Socket Closed: {reason}");
                    _isLoggedIn = false;
                    OnDisconnected?.Invoke(reason);
                    _ = HandleReconnectAsync();
                };

                _session.Items.ItemReceived += (helper) =>
                {
                    var item = helper.DequeueItem();
                    int index = helper.Index;
                    OnItemReceived?.Invoke(item, index);
                };

                _session.MessageLog.OnMessageReceived += (message) =>
                {
                    OnMessageReceived?.Invoke(message.ToString());
                };

                SilksongArchipelagoPlugin.Log.LogInfo($"Connecting socket to {host}:{port}...");
                await _session.ConnectAsync();
                SilksongArchipelagoPlugin.Log.LogInfo($"Socket connected. Logging in as '{slotName}'...");

                var loginResult = await _session.LoginAsync("Silksong", slotName, ItemsHandlingFlags.AllItems, password: password);
                if (!loginResult.Successful)
                {
                    var fail = (LoginFailure)loginResult;
                    string errors = string.Join("; ", fail.Errors);
                    SilksongArchipelagoPlugin.Log.LogError($"Login failed: {errors}");
                    OnConnectionFailed?.Invoke(errors);
                    return false;
                }

                var success = (LoginSuccessful)loginResult;
                SlotData = success.SlotData;
                _isLoggedIn = true;

                SilksongArchipelagoPlugin.Log.LogInfo($"Connected to Archipelago server as '{slotName}'.");
                if (_session.Locations.AllLocationsChecked != null)
                {
                    SilksongArchipelagoPlugin.Instance?.LocationManager.LoadCheckedLocations(_session.Locations.AllLocationsChecked);
                }
                OnConnected?.Invoke();

                // Scout all locations in background so item names and sprites are known
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var locs = _session.Locations.AllLocations ?? _session.Locations.AllMissingLocations;
                        if (locs != null && locs.Count > 0)
                        {
                            var scouted = await _session.Locations.ScoutLocationsAsync(System.Linq.Enumerable.ToArray(locs));
                            ScoutedLocations = scouted;
                            SilksongArchipelagoPlugin.Log.LogInfo($"Scouted {scouted.Count} locations.");
                        }
                    }
                    catch (Exception ex)
                    {
                        SilksongArchipelagoPlugin.Log.LogWarning($"Failed to scout locations: {ex.Message}");
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogError($"Failed to connect to {host}:{port}: {ex.Message}");
                OnConnectionFailed?.Invoke(ex.Message);
                return false;
            }
        }

        private async Task HandleReconnectAsync()
        {
            if (_isReconnecting || string.IsNullOrEmpty(CurrentHost) || CurrentPort <= 0)
                return;

            _isReconnecting = true;
            SilksongArchipelagoPlugin.Log.LogInfo("Attempting to reconnect in 5 seconds...");
            await Task.Delay(5000);

            while (!IsConnected && _isReconnecting)
            {
                try
                {
                    SilksongArchipelagoPlugin.Log.LogInfo($"Reconnecting to {CurrentHost}:{CurrentPort}...");
                    bool ok = await ConnectAsync(CurrentHost, CurrentPort, _slotName, _password);
                    if (ok)
                    {
                        _isReconnecting = false;
                        return;
                    }
                }
                catch
                {
                    // Retry after delay
                }

                await Task.Delay(10000);
            }

            _isReconnecting = false;
        }


        /// <summary>Send a location check to the server.</summary>
        public void SendLocation(long locationId)
        {
            if (IsConnected)
            {
                _session?.Locations.CompleteLocationChecks(locationId);
            }
        }

        /// <summary>Send multiple location checks to the server.</summary>
        public void SendLocations(IEnumerable<long> locationIds)
        {
            if (IsConnected)
            {
                _session?.Locations.CompleteLocationChecks(new List<long>(locationIds).ToArray());
            }
        }

        /// <summary>Send goal completion status to the server.</summary>
        public void SendGoalCompleted()
        {
            if (IsConnected)
            {
                SilksongArchipelagoPlugin.Log.LogInfo("Sending Goal Completion (CLIENT_GOAL) to Archipelago server!");
                _session?.SetGoalAchieved();
            }
        }

        /// <summary>Disconnect from the server.</summary>
        public void Disconnect()
        {
            _isReconnecting = false;
            _isLoggedIn = false;
            _session?.Socket.DisconnectAsync();
            _session = null;
            OnDisconnected?.Invoke("Manual disconnect");
        }
    }
}
