using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GlobalEnums;

namespace SilksongArchipelago.Managers
{
    /// <summary>
    /// Maps Archipelago item IDs to in-game effects and handles granting
    /// items to Hornet in Silksong.
    /// </summary>
    public class ItemManager
    {
        public const long BaseId = 777000;

        /// <summary>Index of the last received item (for resync across sessions).</summary>
        public int ReceivedIndex { get; private set; }

        private readonly ConcurrentQueue<(long ItemId, string ItemName, int Index)> _receivedQueue = new();

        /// <summary>
        /// Queue an item received from the network to be processed on the Unity main thread.
        /// </summary>
        public void EnqueueReceivedItem(long itemId, string itemName, int index)
        {
            if (index <= ReceivedIndex)
                return; // already processed

            _receivedQueue.Enqueue((itemId, itemName, index));
        }

        /// <summary>
        /// Process queued items on the main thread (called from Plugin.Update).
        /// </summary>
        public void ProcessQueue()
        {
            var pd = PlayerData.instance;
            if (pd == null)
                return; // Not in game or save not loaded yet

            bool processedAny = false;
            while (_receivedQueue.TryDequeue(out var entry))
            {
                if (entry.Index <= ReceivedIndex)
                    continue;

                ApplyItem(pd, entry.ItemId, entry.ItemName);
                ReceivedIndex = entry.Index;
                processedAny = true;
            }

            if (processedAny)
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
        }

        /// <summary>
        /// Apply an item's effect directly to PlayerData, ToolItemManager, and CollectableItemManager.
        /// </summary>
        private void ApplyItem(PlayerData pd, long itemId, string itemName)
        {
            SilksongArchipelagoPlugin.Log.LogInfo($"Applying Archipelago Item: '{itemName}' (ID {itemId})");

            bool handled = false;

            // 1. Try matching Tools in ToolItemManager
            try
            {
                var allTools = ToolItemManager.GetAllTools();
                if (allTools != null)
                {
                    foreach (var tool in allTools)
                    {
                        if (tool == null) continue;
                        if (MatchesName(tool.name, tool.DisplayName, itemName))
                        {
                            tool.Unlock(null, ToolItem.PopupFlags.None);
                            var data = tool.SavedData;
                            data.IsUnlocked = true;
                            data.AmountLeft = ToolItemManager.GetToolStorageAmount(tool);
                            data.HasBeenSeen = true;
                            data.HasBeenSelected = true;
                            data.IsHidden = false;
                            tool.SavedData = data;
                            pd.SeenToolGetPrompt = true;
                            pd.SeenToolWeaponGetPrompt = true;
                            handled = true;
                            SilksongArchipelagoPlugin.Log.LogInfo($"Unlocked Tool via ToolItemManager: {tool.name}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogWarning($"Error unlocking tool '{itemName}': {ex.Message}");
            }

            // 2. Try matching Crests in ToolItemManager
            if (!handled)
            {
                try
                {
                    var allCrests = ToolItemManager.GetAllCrests();
                    if (allCrests != null)
                    {
                        foreach (var crest in allCrests)
                        {
                            if (crest == null) continue;
                            if (MatchesName(crest.name, crest.DisplayName, itemName))
                            {
                                crest.Unlock();
                                handled = true;
                                SilksongArchipelagoPlugin.Log.LogInfo($"Unlocked Crest via ToolItemManager: {crest.name}");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SilksongArchipelagoPlugin.Log.LogWarning($"Error unlocking crest '{itemName}': {ex.Message}");
                }
            }

            // 3. Try matching CollectableItem in CollectableItemManager
            try
            {
                var cim = ManagerSingleton<CollectableItemManager>.Instance;
                if (cim != null)
                {
                    var coll = cim.GetAllCollectables();
                    if (coll != null)
                    {
                        foreach (var ci in coll)
                        {
                            if (ci == null) continue;
                            if (MatchesName(ci.name, ci.GetDisplayName(CollectableItem.ReadSource.Inventory), itemName))
                            {
                                ci.Collect(1, showPopup: false);
                                handled = true;
                                SilksongArchipelagoPlugin.Log.LogInfo($"Collected Item via CollectableItemManager: {ci.name}");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogWarning($"Error collecting item '{itemName}': {ex.Message}");
            }

            // 4. Core abilities, specific upgrades, and fallbacks
            switch (itemName)
            {
                // ── Abilities ────────────────────────────────────────────────────────
                case "Swift Step":
                    pd.hasDash = true;
                    pd.HasSeenDash = true;
                    break;

                case "Cling Grip":
                    pd.hasWalljump = true;
                    pd.HasSeenWalljump = true;
                    break;

                case "Drifter's Cloak":
                    pd.hasBrolly = true;
                    break;

                case "Faydown Cloak":
                    pd.hasDoubleJump = true;
                    break;

                case "Silk Soar":
                    pd.hasSuperJump = true;
                    pd.HasSeenSuperJump = true;
                    break;

                case "Needolin":
                    pd.hasNeedolin = true;
                    pd.HasSeenNeedolin = true;
                    break;

                case "Clawline":
                    pd.hasHarpoonDash = true;
                    pd.HasSeenHarpoon = true;
                    break;

                // ── Silk Skills ──────────────────────────────────────────────────────
                case "Needle Strike":
                    pd.hasNeedleThrow = true;
                    break;

                case "Thread Storm":
                    pd.hasThreadSphere = true;
                    break;

                case "Cross Stitch":
                    pd.hasParry = true;
                    break;

                case "Silkspear":
                case "Pale Nails":
                case "Rune Rage":
                case "Sharpdart":
                    pd.hasSilkSpecial = true;
                    break;

                // ── Crest Upgrades ───────────────────────────────────────────────────
                case "Crest of Hunter Upgrade 1":
                    pd.UnlockedExtraBlueSlot = true;
                    break;

                case "Crest of Hunter Upgrade 2":
                    pd.UnlockedExtraYellowSlot = true;
                    break;

                // ── Health & Silk Upgrades ───────────────────────────────────────────
                case "Mask Shard":
                    pd.heartPieces++;
                    if (pd.heartPieces >= 4)
                    {
                        pd.heartPieces = 0;
                        pd.maxHealthBase++;
                        pd.maxHealth++;
                        pd.health = pd.maxHealth;
                    }
                    break;

                case "Spool Fragment":
                    pd.silkSpoolParts++;
                    if (pd.silkSpoolParts >= 2)
                    {
                        pd.silkSpoolParts = 0;
                        pd.silkMax += 3;
                    }
                    break;

                case "Silk Heart":
                    pd.maxHealthBase++;
                    pd.maxHealth++;
                    pd.health = pd.maxHealth;
                    break;

                case "Tool Pouch Upgrade":
                case "Tool Pouch":
                    pd.ToolPouchUpgrades++;
                    break;

                case "Crafting Kit Upgrade":
                case "Crafting Kit":
                    pd.ToolKitUpgrades++;
                    break;

                case "Needle Upgrade":
                case "Nail Upgrade":
                    pd.nailUpgrades++;
                    break;

                // ── Keys & Quest Items ───────────────────────────────────────────────
                case "Key of Indolent":
                    pd.HasSlabKeyA = true;
                    break;

                case "Key of Heretic":
                    pd.HasSlabKeyB = true;
                    break;

                case "Simple Key":
                    pd.purchasedGrindleSimpleKey = true;
                    break;

                case "Architect's Key":
                    pd.PurchasedArchitectKey = true;
                    break;

                // ── Currencies & Consumables ─────────────────────────────────────────
                case "Rosary String":
                    try { CurrencyManager.AddCurrency(60, CurrencyType.Money); } catch { pd.geo += 60; }
                    break;

                case "Shard Bundle":
                    try { CurrencyManager.AddCurrency(50, CurrencyType.Shard); } catch { }
                    break;

                case "Silkeater":
                case "Flea Brew":
                    try { CurrencyManager.AddCurrency(100, CurrencyType.Money); } catch { pd.geo += 100; }
                    break;
            }
        }

        private static bool MatchesName(string assetName, string displayName, string query)
        {
            if (string.IsNullOrEmpty(query)) return false;

            if (!string.IsNullOrEmpty(assetName))
            {
                if (assetName.Equals(query, StringComparison.OrdinalIgnoreCase)) return true;
                if (assetName.Replace(" ", "").Equals(query.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)) return true;
            }

            if (!string.IsNullOrEmpty(displayName))
            {
                if (displayName.Equals(query, StringComparison.OrdinalIgnoreCase)) return true;
                if (displayName.Replace(" ", "").Equals(query.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// Set the received index (restored from save file).
        /// </summary>
        public void SetReceivedIndex(int index)
        {
            ReceivedIndex = index;
        }
    }
}
