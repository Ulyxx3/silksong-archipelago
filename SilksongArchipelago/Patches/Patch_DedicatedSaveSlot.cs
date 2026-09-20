using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SilksongArchipelago.Patches
{
    /// <summary>
    /// Labels Save Slot 4 as "ARCHIPELAGO" in the save slot selection UI.
    /// Slot 4 (user4.dat) is used as the dedicated Archipelago save file,
    /// using the game's fully native save infrastructure.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_DedicatedSaveSlot
    {
        public const int ArchipelagoSlotIndex = 4;

        [HarmonyPatch(typeof(SaveSlotButton), "Prepare")]
        [HarmonyPostfix]
        public static void Postfix_SaveSlotButton_Prepare(SaveSlotButton __instance)
        {
            try
            {
                if (__instance == null) return;

                var saveSlotField = Traverse.Create(__instance).Field("saveSlot");
                if (saveSlotField == null) return;

                var slotEnum = saveSlotField.GetValue<SaveSlotButton.SaveSlot>();
                if (slotEnum != SaveSlotButton.SaveSlot.Slot4) return;

                var slotNumberCg = Traverse.Create(__instance).Field<CanvasGroup>("slotNumberText").Value;
                if (slotNumberCg == null) return;

                var tmpText = slotNumberCg.GetComponentInChildren<TMP_Text>(true);
                if (tmpText != null)
                {
                    tmpText.text = "ARCHIPELAGO";
                }
                else
                {
                    var uiText = slotNumberCg.GetComponentInChildren<Text>(true);
                    if (uiText != null)
                    {
                        uiText.text = "ARCHIPELAGO";
                    }
                }
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogError($"Failed to label Slot 4 as ARCHIPELAGO: {ex}");
            }
        }
    }
}
