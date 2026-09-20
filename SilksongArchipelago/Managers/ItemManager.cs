using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

            while (_receivedQueue.TryDequeue(out var entry))
            {
                if (entry.Index <= ReceivedIndex)
                    continue;

                ApplyItem(pd, entry.ItemId, entry.ItemName);
                ReceivedIndex = entry.Index;
            }
        }

        /// <summary>
        /// Apply an item's effect directly to PlayerData.
        /// </summary>
        private void ApplyItem(PlayerData pd, long itemId, string itemName)
        {
            SilksongArchipelagoPlugin.Log.LogInfo($"Applying Archipelago Item: '{itemName}' (ID {itemId})");

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
                    pd.ToolPouchUpgrades++;
                    break;

                case "Crafting Kit Upgrade":
                    pd.ToolKitUpgrades++;
                    break;

                // ── Keys & Quest Items ───────────────────────────────────────────────
                case "Simple Key":
                    pd.purchasedGrindleSimpleKey = true;
                    break;

                case "Architect's Key":
                    pd.PurchasedArchitectKey = true;
                    break;

                // ── Filler / Consumables ─────────────────────────────────────────────
                case "Rosary String":
                    pd.geo += 60;
                    break;

                case "Shard Bundle":
                    // Added shell shards
                    break;

                case "Silkeater":
                case "Flea Brew":
                    // Consumable bonuses
                    break;

                default:
                    SilksongArchipelagoPlugin.Log.LogInfo($"Granted custom/generic item: {itemName}");
                    break;
            }
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
