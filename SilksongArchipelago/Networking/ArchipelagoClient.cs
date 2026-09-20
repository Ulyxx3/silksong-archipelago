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
        public event Action<string>? OnDisconnected;
        public event Action<ItemInfo, int>? OnItemReceived;
        public event Action<string>? OnMessageReceived;

        private ArchipelagoSession? _session;
        private string _serverUrl = "";
        private string _slotName = "";
        private string _password = "";
        private bool _isReconnecting = false;

        public bool IsConnected => _session?.Socket.Connected ?? false;
        public int SlotIndex => _session?.ConnectionInfo.Slot ?? -1;
        public int TeamIndex => _session?.ConnectionInfo.Team ?? 0;
        public Dictionary<string, object> SlotData { get; private set; } = new();

        /// <summary>
        /// Attempt to connect to an Archipelago server.
        /// </summary>
        public async Task<bool> ConnectAsync(string serverUrl, string slotName, string password = "")
        {
            _serverUrl = serverUrl;
            _slotName = slotName;
            _password = password;

            try
            {
                _session = ArchipelagoSessionFactory.CreateSession(new Uri(serverUrl));

                _session.Socket.ErrorReceived += (ex, message) =>
                {
                    SilksongArchipelagoPlugin.Log.LogError($"Archipelago Socket Error: {message} ({ex})");
                };

                _session.Socket.SocketClosed += (reason) =>
                {
                    SilksongArchipelagoPlugin.Log.LogWarning($"Archipelago Socket Closed: {reason}");
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

                var loginResult = _session.TryConnectAndLogin("Silksong", slotName, ItemsHandlingFlags.AllItems, password: password);
                if (!loginResult.Successful)
                {
                    var fail = (LoginFailure)loginResult;
                    string errors = string.Join("; ", fail.Errors);
                    SilksongArchipelagoPlugin.Log.LogError($"Login failed: {errors}");
                    return false;
                }

                var success = (LoginSuccessful)loginResult;
                SlotData = success.SlotData;

                SilksongArchipelagoPlugin.Log.LogInfo($"Connected to Archipelago server as '{slotName}'.");
                OnConnected?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogError($"Failed to connect to {serverUrl}: {ex.Message}");
                return false;
            }
        }


        private async Task HandleReconnectAsync()
        {
            if (_isReconnecting || string.IsNullOrEmpty(_serverUrl))
                return;

            _isReconnecting = true;
            SilksongArchipelagoPlugin.Log.LogInfo("Attempting to reconnect in 5 seconds...");
            await Task.Delay(5000);

            while (!IsConnected && _isReconnecting)
            {
                try
                {
                    SilksongArchipelagoPlugin.Log.LogInfo($"Reconnecting to {_serverUrl}...");
                    bool ok = await ConnectAsync(_serverUrl, _slotName, _password);
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
            _session?.Socket.DisconnectAsync();
            _session = null;
            OnDisconnected?.Invoke("Manual disconnect");
        }
    }
}
