using HarmonyLib;

namespace SilksongArchipelago.Patches
{
    /// <summary>
    /// Detects when Hornet picks up a collectible item in the game world
    /// and reports the check to LocationManager.
    /// </summary>
    [HarmonyPatch(typeof(CollectableItemPickup), "DoPickupAction")]
    public static class Patch_CollectableItemPickup_DoPickupAction
    {
        static void Postfix(CollectableItemPickup __instance, bool __result)
        {
            if (!__result)
                return;

            string itemName = __instance.Item != null ? __instance.Item.name : "";
            string sceneName = __instance.gameObject.scene.name ?? "";

            SilksongArchipelagoPlugin.Log.LogInfo(
                $"Item picked up: '{itemName}' in scene '{sceneName}'");

            // TODO: Match sceneName and itemName to mapped Archipelago Location ID
        }
    }
}
