using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SilksongArchipelago.Managers;
using SilksongArchipelago.Networking;

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

        private Harmony? _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");

            // Initialize managers
            SaveManager = new SaveManager();
            SaveManager.Load();

            ItemManager = new ItemManager();
            ItemManager.SetReceivedIndex(SaveManager.CurrentSave.ReceivedIndex);

            LocationManager = new LocationManager();
            LocationManager.LoadCheckedLocations(SaveManager.CurrentSave.CheckedLocations);

            ArchipelagoClient = new ArchipelagoClient();

            // Wire up events
            ArchipelagoClient.OnConnected += () =>
            {
                Log.LogInfo("Connected! Flushing pending locations...");
                LocationManager.FlushPendingLocations();
            };

            ArchipelagoClient.OnItemReceived += (item, index) =>
            {
                ItemManager.EnqueueReceivedItem(item.ItemId, item.ItemName, index);
            };

            LocationManager.OnLocationChecked += (locId) =>
            {
                SaveManager.CurrentSave.CheckedLocations.Add(locId);
                SaveManager.Save();
            };

            // Apply Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.LogInfo("Harmony patches applied successfully.");
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
