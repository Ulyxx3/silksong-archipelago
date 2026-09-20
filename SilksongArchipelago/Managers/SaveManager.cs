using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace SilksongArchipelago.Managers
{
    public class SaveData
    {
        public string ServerUrl { get; set; } = "archipelago.gg:38281";
        public string SlotName { get; set; } = "";
        public string Password { get; set; } = "";
        public int ReceivedIndex { get; set; } = 0;
        public List<long> CheckedLocations { get; set; } = new();
    }

    public class SaveManager
    {
        private string SaveFilePath => Path.Combine(Paths.ConfigPath, "SilksongArchipelago_Save.json");

        public SaveData CurrentSave { get; private set; } = new();

        public void Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    CurrentSave = JsonConvert.DeserializeObject<SaveData>(json) ?? new SaveData();
                    SilksongArchipelagoPlugin.Log.LogInfo($"Loaded Archipelago save data. (Last index: {CurrentSave.ReceivedIndex})");
                }
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogError($"Failed to load save data: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(CurrentSave, Formatting.Indented);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogError($"Failed to save data: {ex.Message}");
            }
        }
    }
}
