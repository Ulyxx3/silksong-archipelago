using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
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
                UI.AddLog("Connected to Archipelago server!", Color.green);
                LocationManager.FlushPendingLocations();
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
            // Process queued items on Unity's main thread
            ItemManager.ProcessQueue();

            // Save updated received index if changed
            if (ItemManager.ReceivedIndex != SaveManager.CurrentSave.ReceivedIndex)
            {
                SaveManager.CurrentSave.ReceivedIndex = ItemManager.ReceivedIndex;
                SaveManager.Save();
            }
        }

        private void OnDestroy()
        {
            ArchipelagoClient.Disconnect();
            _harmony?.UnpatchSelf();
        }
    }
}
