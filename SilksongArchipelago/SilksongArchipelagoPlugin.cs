using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using SilksongArchipelago.Managers;
using SilksongArchipelago.Networking;
using SilksongArchipelago.UI;

namespace SilksongArchipelago
{
    /// <summary>
    /// Main BepInEx plugin entry point for Silksong Archipelago.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class SilksongArchipelagoPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.ulyxx3.silksong.archipelago";
        public const string PluginName = "Silksong Archipelago";
        public const string PluginVersion = "0.1.0";

        public static SilksongArchipelagoPlugin? Instance { get; private set; }
        internal static ManualLogSource Log = null!;

        public ArchipelagoClient ArchipelagoClient { get; private set; } = null!;
        public ItemManager ItemManager { get; private set; } = null!;
        public LocationManager LocationManager { get; private set; } = null!;
        public SaveManager SaveManager { get; private set; } = null!;
        public ArchipelagoUI UI { get; private set; } = null!;

        // BepInEx Configuration Entries
        public ConfigEntry<string> ConfigServer { get; private set; } = null!;
        public ConfigEntry<int> ConfigPort { get; private set; } = null!;
        public ConfigEntry<string> ConfigSlotName { get; private set; } = null!;
        public ConfigEntry<string> ConfigPassword { get; private set; } = null!;
        public ConfigEntry<KeyCode> ConfigMenuKey { get; private set; } = null!;
        public ConfigEntry<bool> ConfigAutoConnect { get; private set; } = null!;

        private Harmony? _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");

            // Fix Unity / BepInEx Newtonsoft.Json generic collection deserialization conflict
            FixArchipelagoJsonDeserialization();

            // Bind configuration settings
            BindConfig();

            // Initialize managers
            SaveManager = new SaveManager();
            SaveManager.Load();

            ItemManager = new ItemManager();
            ItemManager.SetReceivedIndex(SaveManager.CurrentSave.ReceivedIndex);

            LocationManager = new LocationManager();
            LocationManager.LoadCheckedLocations(SaveManager.CurrentSave.CheckedLocations);

            ArchipelagoClient = new ArchipelagoClient();

            // Initialize In-Game GUI Overlay
            UI = gameObject.AddComponent<ArchipelagoUI>();
            UI.ToggleKey = ConfigMenuKey.Value;
            UI.Host = !string.IsNullOrEmpty(SaveManager.CurrentSave.Host) ? SaveManager.CurrentSave.Host : ConfigServer.Value;
            UI.Port = SaveManager.CurrentSave.Port > 0 ? SaveManager.CurrentSave.Port.ToString() : ConfigPort.Value.ToString();
            UI.SlotName = !string.IsNullOrEmpty(SaveManager.CurrentSave.SlotName) ? SaveManager.CurrentSave.SlotName : ConfigSlotName.Value;
            UI.Password = ConfigPassword.Value;

            // Wire up events
            ArchipelagoClient.OnConnected += () =>
            {
                Log.LogInfo("Connected to Archipelago server! Flushing pending locations...");
                UI.AddLog($"Connected as '{ArchipelagoClient.SlotName}'!", Color.green);
                LocationManager.FlushPendingLocations();
            };

            ArchipelagoClient.OnConnectionFailed += (errors) =>
            {
                Log.LogError($"Archipelago Login Failed: {errors}");
                UI.AddLog($"Login failed: {errors}", new Color(1f, 0.35f, 0.35f));
            };

            ArchipelagoClient.OnDisconnected += (reason) =>
            {
                Log.LogWarning($"Disconnected from Archipelago: {reason}");
                UI.AddLog($"Disconnected: {reason}", new Color(1f, 0.4f, 0.4f));
            };

            ArchipelagoClient.OnItemReceived += (item, index) =>
            {
                ItemManager.EnqueueReceivedItem(item.ItemId, item.ItemName, index);
                UI.AddLog($"Received: {item.ItemName}", new Color(0.95f, 0.82f, 0.45f));
            };

            LocationManager.OnLocationChecked += (locId) =>
            {
                SaveManager.CurrentSave.CheckedLocations.Add(locId);
                SaveManager.Save();

                string locTitle = LocationMapping.LocationsById.TryGetValue(locId, out var entry)
                    ? entry.Title
                    : $"Location {locId}";
                UI.AddLog($"Checked: {locTitle}", Color.cyan);
            };

            // Apply Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.LogInfo("Harmony patches applied successfully.");

            // Auto-connect if enabled
            if (ConfigAutoConnect.Value && !string.IsNullOrEmpty(ConfigServer.Value) && !string.IsNullOrEmpty(ConfigSlotName.Value))
            {
                Log.LogInfo($"Auto-connecting to {ConfigServer.Value}:{ConfigPort.Value} as '{ConfigSlotName.Value}'...");
                ArchipelagoClient.Connect(ConfigServer.Value, ConfigPort.Value, ConfigSlotName.Value, ConfigPassword.Value);
            }
        }

        private void BindConfig()
        {
            ConfigServer = Config.Bind("Archipelago", "Server", "localhost", "Host or IP address of the Archipelago server.");
            ConfigPort = Config.Bind("Archipelago", "Port", 38281, "Port number of the Archipelago server.");
            ConfigSlotName = Config.Bind("Archipelago", "SlotName", "Hornet", "Player slot name defined in the YAML.");
            ConfigPassword = Config.Bind("Archipelago", "Password", "", "Server password (if required).");
            ConfigMenuKey = Config.Bind("Interface", "MenuToggleKey", KeyCode.F2, "Key to toggle the in-game Archipelago menu.");
            ConfigAutoConnect = Config.Bind("Archipelago", "AutoConnect", false, "Automatically connect on game startup.");
        }

        private void Update()
        {
            var pd = PlayerData.instance;
            if (pd != null)
            {
                // If a fresh new game is loaded in Slot 4 (playTime < 2s) but ReceivedIndex > 0 or CheckedLocations > 0, reset state
                if (pd.playTime < 2.0f && (SaveManager.CurrentSave.ReceivedIndex > 0 || SaveManager.CurrentSave.CheckedLocations.Count > 0))
                {
                    Log.LogInfo("Fresh save slot detected! Resetting Archipelago state (ReceivedIndex and CheckedLocations).");
                    SaveManager.CurrentSave.ReceivedIndex = 0;
                    SaveManager.CurrentSave.CheckedLocations.Clear();
                    LocationManager.Clear();
                    ItemManager.SetReceivedIndex(0);
                    SaveManager.Save();
                }

                // If connected and items exist on server, ensure all items up to count are enqueued
                var allItems = ArchipelagoClient.AllReceivedItems;
                if (ArchipelagoClient.IsConnected && allItems != null)
                {
                    for (int i = ItemManager.ReceivedIndex; i < allItems.Count; i++)
                    {
                        var item = allItems[i];
                        ItemManager.EnqueueReceivedItem(item.ItemId, item.ItemName, i + 1);
                    }
                }

                // Process queued items on Unity's main thread
                ItemManager.ProcessQueue();

                // Save updated received index if changed
                if (ItemManager.ReceivedIndex != SaveManager.CurrentSave.ReceivedIndex)
                {
                    SaveManager.CurrentSave.ReceivedIndex = ItemManager.ReceivedIndex;
                    SaveManager.Save();
                }
            }
        }

        private void FixArchipelagoJsonDeserialization()
        {
            try
            {
                // === CRITICAL FIX ===
                // Silksong uses Newtonsoft.Json.UnityConverters which auto-discovers ALL JsonConverter
                // subclasses from all loaded assemblies. Archipelago.MultiClient.Net brings in
                // PermissionsEnumConverter whose CanConvert() returns true for typeof(int), causing
                // it to hijack serialization of ALL integer fields in PlayerData/SaveGameData.
                // This results in InvalidCastException during SaveDataUtility.SerializeSaveData.

                // Step 1: Remove Archipelago's poisonous converters from Unity's default settings
                var unitySettingsField = typeof(Newtonsoft.Json.UnityConverters.UnityConverterInitializer)
                    .GetProperty("defaultUnityConvertersSettings", BindingFlags.Public | BindingFlags.Static);
                if (unitySettingsField != null)
                {
                    var settings = unitySettingsField.GetValue(null) as JsonSerializerSettings;
                    if (settings?.Converters != null)
                    {
                        int removed = 0;
                        for (int i = settings.Converters.Count - 1; i >= 0; i--)
                        {
                            var converter = settings.Converters[i];
                            if (converter == null) continue;
                            string typeName = converter.GetType().FullName ?? "";
                            if (typeName.Contains("Archipelago") || typeName.Contains("Permissions"))
                            {
                                settings.Converters.RemoveAt(i);
                                removed++;
                            }
                        }
                        Log.LogInfo($"Removed {removed} Archipelago converter(s) from Unity JSON settings.");
                    }
                }

                // Step 2: Prevent UnityConverterInitializer from re-adding converters on future Init()
                var shouldAddField = typeof(Newtonsoft.Json.UnityConverters.UnityConverterInitializer)
                    .GetProperty("shouldAddConvertsToDefaultSettings", BindingFlags.Public | BindingFlags.Static);
                if (shouldAddField != null && shouldAddField.CanWrite)
                {
                    shouldAddField.SetValue(null, false);
                    Log.LogInfo("Disabled Unity JSON converter auto-sync to prevent re-poisoning.");
                }

                // Step 3: Null out SaveDataUtility._serializer so it gets recreated clean
                var serializerField = typeof(SaveDataUtility)
                    .GetField("_serializer", BindingFlags.NonPublic | BindingFlags.Static);
                if (serializerField != null)
                {
                    serializerField.SetValue(null, null);
                    Log.LogInfo("Reset SaveDataUtility._serializer for clean recreation.");
                }

                // Step 4: Fix Archipelago's own packet deserialization (existing fix)
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings();

                var mapField = typeof(ArchipelagoPacketConverter)
                    .GetField("PacketDeserializationMap", BindingFlags.NonPublic | BindingFlags.Static);
                if (mapField?.GetValue(null) is Dictionary<ArchipelagoPacketType, Func<JObject, ArchipelagoPacketBase>> map)
                {
                    var cleanSerializer = JsonSerializer.Create(new JsonSerializerSettings());
                    map[ArchipelagoPacketType.RoomInfo] = (obj) => obj.ToObject<RoomInfoPacket>(cleanSerializer);
                    map[ArchipelagoPacketType.Connected] = (obj) => obj.ToObject<ConnectedPacket>(cleanSerializer);
                    map[ArchipelagoPacketType.ConnectionRefused] = (obj) => obj.ToObject<ConnectionRefusedPacket>(cleanSerializer);
                    map[ArchipelagoPacketType.ReceivedItems] = (obj) => obj.ToObject<ReceivedItemsPacket>(cleanSerializer);
                    map[ArchipelagoPacketType.LocationInfo] = (obj) => obj.ToObject<LocationInfoPacket>(cleanSerializer);
                    map[ArchipelagoPacketType.RoomUpdate] = (obj) => obj.ToObject<RoomUpdatePacket>(cleanSerializer);
                    map[ArchipelagoPacketType.PrintJSON] = (obj) => obj.ToObject<PrintJsonPacket>(cleanSerializer);
                    map[ArchipelagoPacketType.DataPackage] = (obj) => obj.ToObject<DataPackagePacket>(cleanSerializer);
                    map[ArchipelagoPacketType.Bounced] = (obj) => obj.ToObject<BouncedPacket>(cleanSerializer);
                    map[ArchipelagoPacketType.Retrieved] = (obj) => obj.ToObject<RetrievedPacket>(cleanSerializer);
                    map[ArchipelagoPacketType.SetReply] = (obj) => obj.ToObject<SetReplyPacket>(cleanSerializer);
                    Log.LogInfo("Archipelago JSON packet deserializers initialized with clean serializers.");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to patch JSON deserializers: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            ArchipelagoClient.Disconnect();
            _harmony?.UnpatchSelf();
        }
    }
}
