using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SilksongArchipelago.Managers;
using Archipelago.MultiClient.Net.Models;

namespace SilksongArchipelago.Patches
{
    /// <summary>
    /// Harmony patches for ShopMenuStock, ShopItemStats, and ShopItem to display
    /// Archipelago items, target player names, descriptions, and the official Archipelago icon in shops,
    /// and to dispatch checks to the Archipelago server upon purchase.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_ShopMenuStock
    {
        private static List<ShopItemStats>? GetAvailableStock(ShopMenuStock stock)
        {
            if (stock == null) return null;
            return Traverse.Create(stock).Field<List<ShopItemStats>>("availableStock").Value;
        }

        [ThreadStatic] private static bool _inDisplayNameHook;
        [ThreadStatic] private static bool _inDescriptionHook;
        [ThreadStatic] private static bool _inSpriteHook;

        public static long GetLocationIdForItem(ShopMenuStock? stock, ShopItem item, int itemNum = -1, string? currentDisplayName = null)
        {
            if (item == null) return -1;

            string title = stock?.Title ?? "";
            string itemName = item.name ?? "";
            string displayName = !string.IsNullOrEmpty(currentDisplayName) ? currentDisplayName : (item.DisplayName ?? "");
            string savedName = item.Item != null ? (item.Item.name ?? "") : "";
            string scene = GameManager.instance != null ? (GameManager.instance.sceneName ?? "") : "";

            bool isBoneBottom = scene.IndexOf("Bone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                scene.IndexOf("Bonetown", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                title.IndexOf("Pebb", StringComparison.OrdinalIgnoreCase) >= 0;

            bool isDeepDocks = scene.IndexOf("Dock", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               title.IndexOf("Forge Daughter", StringComparison.OrdinalIgnoreCase) >= 0;

            bool isShakra = title.IndexOf("Shakra", StringComparison.OrdinalIgnoreCase) >= 0;

            // ── Pebb in Bone Bottom ──────────────────────────────────────────────────
            if (isBoneBottom)
            {
                if (itemNum == 0 || MatchesTerm(itemName, displayName, savedName, "Magnetite", "magnétite", "Brooch", "Broche", "Rosary Magnet"))
                    return 777181; // Magnetite Brooch - Bone Bottom

                if (itemNum == 1 || MatchesTerm(itemName, displayName, savedName, "Mask", "masque", "Heart", "coeur"))
                    return 777063; // Mask Shard - Bone Bottom

                if (itemNum == 2 || MatchesTerm(itemName, displayName, savedName, "Craftmetal", "artisan", "metal", "métal", "craft"))
                    return 777147; // Craftmetal - Bone Bottom

                if (itemNum == 3 || MatchesTerm(itemName, displayName, savedName, "Key", "Clé", "Cle"))
                    return 777300; // Simple Key - Bone Bottom

                if (itemNum == 4 || MatchesTerm(itemName, displayName, savedName, "Rosary", "chapelet"))
                    return 777002; // Pebb - Bone Bottom
            }

            // ── Forge Daughter in Deep Docks ─────────────────────────────────────────
            if (isDeepDocks)
            {
                if (itemNum == 0 || MatchesTerm(itemName, displayName, savedName, "Sting", "Aiguillon"))
                    return 777185; // Sting Shard - Deep Docks
                if (itemNum == 1 || MatchesTerm(itemName, displayName, savedName, "Magma"))
                    return 777186; // Magma Bell - Deep Docks
                if (itemNum == 2 || MatchesTerm(itemName, displayName, savedName, "Silkshot"))
                    return 777179;
                if (itemNum == 3 || MatchesTerm(itemName, displayName, savedName, "Kit"))
                    return 777148;
                return 777001; // Forge Daughter - Deep Docks
            }

            // ── Shakra locations by scene ────────────────────────────────────────────
            if (isShakra || MatchesTerm(itemName, displayName, savedName, "Map", "Carte", "Quill", "Plume", "Pins", "Épingle"))
            {
                if (itemNum == 0 || MatchesTerm(itemName, displayName, savedName, "Compass", "Boussole"))
                    return 777183; // Compass - Mosslands

                if (scene.IndexOf("Bone", StringComparison.OrdinalIgnoreCase) >= 0) return 777004;
                if (scene.IndexOf("Dock", StringComparison.OrdinalIgnoreCase) >= 0) return 777005;
                if (scene.IndexOf("Marrow", StringComparison.OrdinalIgnoreCase) >= 0) return 777003;
                if (scene.IndexOf("Field", StringComparison.OrdinalIgnoreCase) >= 0) return 777006;
                if (scene.IndexOf("Hunter", StringComparison.OrdinalIgnoreCase) >= 0) return 777008;
                if (scene.IndexOf("Grey", StringComparison.OrdinalIgnoreCase) >= 0) return 777009;
                if (scene.IndexOf("Shell", StringComparison.OrdinalIgnoreCase) >= 0) return 777010;
                if (scene.IndexOf("Sinner", StringComparison.OrdinalIgnoreCase) >= 0) return 777012;
                if (scene.IndexOf("Blast", StringComparison.OrdinalIgnoreCase) >= 0) return 777013;
                if (scene.IndexOf("Worm", StringComparison.OrdinalIgnoreCase) >= 0) return 777014;
                if (scene.IndexOf("Karak", StringComparison.OrdinalIgnoreCase) >= 0) return 777016;
                if (scene.IndexOf("Bile", StringComparison.OrdinalIgnoreCase) >= 0) return 777019;
                if (scene.IndexOf("Fay", StringComparison.OrdinalIgnoreCase) >= 0) return 777021;
                if (scene.IndexOf("Bell", StringComparison.OrdinalIgnoreCase) >= 0) return 777022;
                return 777003;
            }

            // ── Other Vendors ────────────────────────────────────────────────────────
            if (title.IndexOf("Mort", StringComparison.OrdinalIgnoreCase) >= 0) return 777007;
            if (title.IndexOf("Frey", StringComparison.OrdinalIgnoreCase) >= 0) return 777011;
            if (title.IndexOf("Grindle", StringComparison.OrdinalIgnoreCase) >= 0) return 777015;
            if (title.IndexOf("Mottled Skarr", StringComparison.OrdinalIgnoreCase) >= 0) return 777017;
            if (title.IndexOf("Twelfth Architect", StringComparison.OrdinalIgnoreCase) >= 0) return 777018;
            if (title.IndexOf("Jubilana", StringComparison.OrdinalIgnoreCase) >= 0) return 777020;

            // ── Fallback item unique matching ─────────────────────────────────────────
            if (MatchesTerm(itemName, displayName, savedName, "Magnetite", "magnétite", "Rosary Magnet"))
                return 777181;
            if (MatchesTerm(itemName, displayName, savedName, "Straight Pin"))
                return 777184;
            if (MatchesTerm(itemName, displayName, savedName, "Warding Bell"))
                return 777187;
            if (MatchesTerm(itemName, displayName, savedName, "Weighted Belt"))
                return 777188;
            if (MatchesTerm(itemName, displayName, savedName, "Flintslate"))
                return 777189;
            if (MatchesTerm(itemName, displayName, savedName, "Threefold Pin"))
                return 777191;
            if (MatchesTerm(itemName, displayName, savedName, "Longpin"))
                return 777192;
            if (MatchesTerm(itemName, displayName, savedName, "Silkspeed Anklets"))
                return 777193;

            return -1;
        }

        private static bool MatchesTerm(string itemName, string displayName, string savedName, params string[] terms)
        {
            foreach (var t in terms)
            {
                if (itemName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    displayName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    savedName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static ScoutedItemInfo? GetScoutedItem(ShopItem? item, ShopMenuStock? stock = null, int itemNum = -1, string? currentDisplayName = null)
        {
            if (item == null) return null;
            var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
            if (client == null || !client.IsConnected) return null;

            long locId = GetLocationIdForItem(stock, item, itemNum, currentDisplayName);
            if (locId > 0 && client.ScoutedLocations.TryGetValue(locId, out var info))
            {
                return info;
            }
            return null;
        }

        // ── Harmony Patches for ShopItem properties ──────────────────────────────────
        [HarmonyPatch(typeof(ShopItem), "get_DisplayName")]
        [HarmonyPostfix]
        public static void Postfix_ShopItem_get_DisplayName(ShopItem __instance, ref string __result)
        {
            if (_inDisplayNameHook) return;
            try
            {
                _inDisplayNameHook = true;
                var scouted = GetScoutedItem(__instance, currentDisplayName: __result);
                if (scouted == null) return;

                var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
                if (scouted.Player.Slot != client?.SlotIndex)
                {
                    __result = $"{scouted.ItemDisplayName} ({scouted.Player.Name})";
                }
                else
                {
                    __result = scouted.ItemDisplayName;
                }
            }
            finally
            {
                _inDisplayNameHook = false;
            }
        }

        [HarmonyPatch(typeof(ShopItem), "get_Description")]
        [HarmonyPostfix]
        public static void Postfix_ShopItem_get_Description(ShopItem __instance, ref string __result)
        {
            if (_inDescriptionHook) return;
            try
            {
                _inDescriptionHook = true;
                var scouted = GetScoutedItem(__instance);
                if (scouted == null) return;

                var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
                if (scouted.Player.Slot != client?.SlotIndex)
                {
                    __result = $"Archipelago item for {scouted.Player.Name} in {scouted.Player.Game}.";
                }
                else
                {
                    __result = $"Archipelago item for Hornet ({scouted.ItemDisplayName}).";
                }
            }
            finally
            {
                _inDescriptionHook = false;
            }
        }

        public static Sprite? GetSpriteForScoutedItem(ScoutedItemInfo? scouted, Archipelago.MultiClient.Net.ArchipelagoSession? client = null)
        {
            if (scouted == null) return null;
            return SpriteManager.ArchipelagoIconSprite;
        }

        public static void UnblockUIInput()
        {
            try
            {
                StaticVariableList.SetValue("IsUIListInputBlocked", false);
                if (HeroController.instance != null)
                {
                    var blockersField = typeof(HeroController).GetField("inputBlockers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (blockersField?.GetValue(HeroController.instance) is System.Collections.IEnumerable list)
                    {
                        var clearMethod = list.GetType().GetMethod("Clear");
                        clearMethod?.Invoke(list, null);
                    }
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(ShopMenuStock), "BuildItemList")]
        [HarmonyPrefix]
        public static void Prefix_ShopMenuStock_BuildItemList()
        {
            UnblockUIInput();
        }

        [HarmonyPatch(typeof(InventoryPaneInput), "OnEnable")]
        [HarmonyPrefix]
        public static void Prefix_InventoryPaneInput_OnEnable()
        {
            UnblockUIInput();
        }

        // ── Harmony Patches for ShopItem properties ──────────────────────────────────
        [HarmonyPatch(typeof(ShopItem), "get_ItemSprite")]
        [HarmonyPostfix]
        public static void Postfix_ShopItem_get_ItemSprite(ShopItem __instance, ref Sprite __result)
        {
            if (_inSpriteHook) return;
            try
            {
                _inSpriteHook = true;
                var stock = UnityEngine.Object.FindAnyObjectByType<ShopMenuStock>();
                int itemNum = -1;
                if (stock != null)
                {
                    var avail = GetAvailableStock(stock);
                    if (avail != null)
                    {
                        for (int i = 0; i < avail.Count; i++)
                        {
                            if (avail[i]?.Item == __instance) { itemNum = i; break; }
                        }
                    }
                }
                var scouted = GetScoutedItem(__instance, stock, itemNum, __instance.DisplayName);
                long locId = GetLocationIdForItem(stock, __instance, itemNum, __instance.DisplayName);
                if (scouted != null || locId > 0)
                {
                    var icon = (scouted != null ? GetSpriteForScoutedItem(scouted) : null) ?? SpriteManager.ArchipelagoIconSprite;
                    if (icon != null)
                    {
                        __result = icon;
                    }
                }
            }
            finally
            {
                _inSpriteHook = false;
            }
        }

        [HarmonyPatch(typeof(ShopItem), "get_IsPurchased")]
        [HarmonyPostfix]
        public static void Postfix_ShopItem_get_IsPurchased(ShopItem __instance, ref bool __result)
        {
            if (__result) return;
            var plugin = SilksongArchipelagoPlugin.Instance;
            if (plugin == null || !plugin.ArchipelagoClient.IsConnected) return;

            var stock = UnityEngine.Object.FindAnyObjectByType<ShopMenuStock>();
            int itemNum = -1;
            if (stock != null)
            {
                var avail = GetAvailableStock(stock);
                if (avail != null)
                {
                    for (int i = 0; i < avail.Count; i++)
                    {
                        if (avail[i]?.Item == __instance) { itemNum = i; break; }
                    }
                }
            }
            long locId = GetLocationIdForItem(stock, __instance, itemNum, __instance.DisplayName);
            if (locId > 0 && plugin.LocationManager.IsLocationChecked(locId))
            {
                __result = true;
            }
        }

        // ── Harmony Patches for ShopItemStats ────────────────────────────────────────
        [HarmonyPatch(typeof(ShopItemStats), "GetName")]
        [HarmonyPostfix]
        public static void Postfix_ShopItemStats_GetName(ShopItemStats __instance, ref string __result)
        {
            if (__instance == null || __instance.Item == null) return;
            var scouted = GetScoutedItem(__instance.Item, __instance.GetComponentInParent<ShopMenuStock>(), __instance.ItemNumber, currentDisplayName: __result);
            if (scouted == null) return;

            var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
            if (scouted.Player.Slot != client?.SlotIndex)
            {
                __result = $"{scouted.ItemDisplayName} ({scouted.Player.Name})";
            }
            else
            {
                __result = scouted.ItemDisplayName;
            }
        }

        [HarmonyPatch(typeof(ShopItemStats), "GetDesc")]
        [HarmonyPostfix]
        public static void Postfix_ShopItemStats_GetDesc(ShopItemStats __instance, ref string __result)
        {
            if (__instance == null || __instance.Item == null) return;
            var scouted = GetScoutedItem(__instance.Item, __instance.GetComponentInParent<ShopMenuStock>(), __instance.ItemNumber);
            if (scouted == null) return;

            var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
            if (scouted.Player.Slot != client?.SlotIndex)
            {
                __result = $"Archipelago item for {scouted.Player.Name} in {scouted.Player.Game}.";
            }
            else
            {
                __result = $"Archipelago item for Hornet ({scouted.ItemDisplayName}).";
            }
        }

        [HarmonyPatch(typeof(ShopItemStats), "SetItem")]
        [HarmonyPostfix]
        public static void Postfix_ShopItemStats_SetItem(ShopItemStats __instance, ShopItem item)
        {
            ApplyCustomAppearance(__instance, item);
        }

        [HarmonyPatch(typeof(ShopItemStats), "UpdateAppearance")]
        [HarmonyPostfix]
        public static void Postfix_ShopItemStats_UpdateAppearance(ShopItemStats __instance)
        {
            if (__instance == null) return;
            ApplyCustomAppearance(__instance, __instance.Item);
        }

        private static void ApplyCustomAppearance(ShopItemStats stats, ShopItem? item)
        {
            if (stats == null || item == null) return;
            var stock = stats.GetComponentInParent<ShopMenuStock>();
            var scouted = GetScoutedItem(item, stock, stats.ItemNumber);
            long locId = GetLocationIdForItem(stock, item, stats.ItemNumber, item.DisplayName);
            if (scouted == null && locId <= 0) return;

            var icon = (scouted != null ? GetSpriteForScoutedItem(scouted) : null) ?? SpriteManager.ArchipelagoIconSprite;
            var sr = Traverse.Create(stats).Field<SpriteRenderer>("itemSprite").Value;
            if (icon != null && sr != null)
            {
                sr.sprite = icon;
                sr.transform.localScale = Vector3.one * 0.75f;
            }
        }

        // ── Harmony Patches for ShopMenuStock ────────────────────────────────────────
        [HarmonyPatch(typeof(ShopMenuStock), "GetName")]
        [HarmonyPostfix]
        public static void Postfix_ShopMenuStock_GetName(ShopMenuStock __instance, int itemNum, ref string __result)
        {
            var avail = GetAvailableStock(__instance);
            if (avail != null && itemNum >= 0 && itemNum < avail.Count && avail[itemNum]?.Item != null)
            {
                var scouted = GetScoutedItem(avail[itemNum].Item, __instance, itemNum, currentDisplayName: __result);
                if (scouted != null)
                {
                    var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
                    __result = scouted.Player.Slot != client?.SlotIndex
                        ? $"{scouted.ItemDisplayName} ({scouted.Player.Name})"
                        : scouted.ItemDisplayName;
                }
            }
        }

        [HarmonyPatch(typeof(ShopMenuStock), "GetDesc")]
        [HarmonyPostfix]
        public static void Postfix_ShopMenuStock_GetDesc(ShopMenuStock __instance, int itemNum, ref string __result)
        {
            var avail = GetAvailableStock(__instance);
            if (avail != null && itemNum >= 0 && itemNum < avail.Count && avail[itemNum]?.Item != null)
            {
                var scouted = GetScoutedItem(avail[itemNum].Item, __instance, itemNum);
                if (scouted != null)
                {
                    var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
                    __result = scouted.Player.Slot != client?.SlotIndex
                        ? $"Archipelago item for {scouted.Player.Name} in {scouted.Player.Game}."
                        : $"Archipelago item for Hornet ({scouted.ItemDisplayName}).";
                }
            }
        }

        [HarmonyPatch(typeof(ShopMenuStock), "GetItemSprite")]
        [HarmonyPostfix]
        public static void Postfix_ShopMenuStock_GetItemSprite(ShopMenuStock __instance, int itemNum, ref Sprite __result)
        {
            var avail = GetAvailableStock(__instance);
            if (avail != null && itemNum >= 0 && itemNum < avail.Count && avail[itemNum]?.Item != null)
            {
                var scouted = GetScoutedItem(avail[itemNum].Item, __instance, itemNum);
                long locId = GetLocationIdForItem(__instance, avail[itemNum].Item, itemNum, avail[itemNum].Item.DisplayName);
                if (scouted != null || locId > 0)
                {
                    var icon = (scouted != null ? GetSpriteForScoutedItem(scouted) : null) ?? SpriteManager.ArchipelagoIconSprite;
                    if (icon != null) __result = icon;
                }
            }
        }

        [HarmonyPatch(typeof(ShopMenuStock), "GetItemSpriteScale")]
        [HarmonyPostfix]
        public static void Postfix_ShopMenuStock_GetItemSpriteScale(ShopMenuStock __instance, int itemNum, ref Vector3 __result)
        {
            long locId = -1;
            var avail = GetAvailableStock(__instance);
            if (avail != null && itemNum >= 0 && itemNum < avail.Count && avail[itemNum]?.Item != null)
            {
                locId = GetLocationIdForItem(__instance, avail[itemNum].Item, itemNum, avail[itemNum].Item.DisplayName);
            }
            if (locId > 0)
            {
                __result = Vector3.one;
            }
        }

        // ── Harmony Patch for Purchasing: Intercept and Dispatch to Archipelago ──────
        [HarmonyPatch(typeof(ShopItem), "SetPurchased")]
        [HarmonyPrefix]
        public static bool Prefix_ShopItem_SetPurchased(ShopItem __instance, Action onComplete, int subItemIndex)
        {
            if (__instance == null) return true;
            var plugin = SilksongArchipelagoPlugin.Instance;
            if (plugin == null || !plugin.ArchipelagoClient.IsConnected) return true;

            var stock = UnityEngine.Object.FindAnyObjectByType<ShopMenuStock>();
            int itemNum = -1;
            if (stock != null)
            {
                var avail = GetAvailableStock(stock);
                if (avail != null)
                {
                    for (int i = 0; i < avail.Count; i++)
                    {
                        if (avail[i]?.Item == __instance) { itemNum = i; break; }
                    }
                }
            }
            long locId = GetLocationIdForItem(stock, __instance, itemNum, __instance.DisplayName);
            if (locId <= 0) return true;

            // Prevent repurchase if already checked
            if (plugin.LocationManager.IsLocationChecked(locId))
            {
                SilksongArchipelagoPlugin.Log.LogInfo($"Shop item for location {locId} already checked. Preventing duplicate purchase.");
                stock?.BuildItemList();
                onComplete?.Invoke();
                return false;
            }

            SilksongArchipelagoPlugin.Log.LogInfo(
                $"Purchasing ShopItem '{__instance.DisplayName}'! Dispatching Archipelago location {locId}");

            // Deduct currency
            try
            {
                switch (__instance.CurrencyType)
                {
                    case CurrencyType.Money:
                        CurrencyManager.TakeGeo(__instance.Cost);
                        break;
                    case CurrencyType.Shard:
                        CurrencyManager.TakeShards(__instance.Cost);
                        break;
                }
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogWarning($"Could not deduct currency: {ex.Message}");
            }

            // Dispatch location check to Archipelago
            plugin.LocationManager.CheckLocation(locId);

            // Consume required item if any
            if (__instance.RequiredItem != null)
            {
                try
                {
                    __instance.RequiredItem.Consume(__instance.RequiredItemAmount, showCounter: true);
                }
                catch { }
            }

            // Set playerDataBoolName if present so item is marked purchased in-game
            try
            {
                string boolName = Traverse.Create(__instance).Field<string>("playerDataBoolName").Value;
                if (!string.IsNullOrEmpty(boolName) && PlayerData.instance != null)
                {
                    Traverse.Create(PlayerData.instance).Field(boolName).SetValue(true);
                }
            }
            catch { }

            var scouted = GetScoutedItem(__instance, stock, itemNum, __instance.DisplayName);
            string scoutDesc = scouted != null
                ? $"{scouted.ItemDisplayName} for {scouted.Player.Name}"
                : __instance.DisplayName;
            plugin.UI.AddLog($"Bought: {scoutDesc}", UnityEngine.Color.green);

            // Rebuild stock so purchased item immediately disappears from shop
            stock?.BuildItemList();

            // Complete purchase animation/dialogue
            onComplete?.Invoke();

            // Skip vanilla SetPurchased so vanilla item is not granted for out-of-world items
            return false;
        }

        [HarmonyPatch(typeof(ShopItem), "SetPurchased")]
        [HarmonyPostfix]
        public static void Postfix_ShopItem_SetPurchased(ShopItem __instance)
        {
            if (__instance == null) return;
            var plugin = SilksongArchipelagoPlugin.Instance;
            if (plugin == null || !plugin.ArchipelagoClient.IsConnected) return;

            var stock = UnityEngine.Object.FindAnyObjectByType<ShopMenuStock>();
            long locId = GetLocationIdForItem(stock, __instance, -1, __instance.DisplayName);
            if (locId > 0 && !plugin.LocationManager.IsLocationChecked(locId))
            {
                SilksongArchipelagoPlugin.Log.LogInfo(
                    $"ShopItem fallback check: Dispatching location {locId}");
                plugin.LocationManager.CheckLocation(locId);
            }
        }
    }
}
