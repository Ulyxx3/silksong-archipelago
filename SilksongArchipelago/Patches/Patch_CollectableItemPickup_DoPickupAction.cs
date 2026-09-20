using HarmonyLib;
using SilksongArchipelago.Managers;

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
            string objectName = __instance.gameObject.name ?? "";

            SilksongArchipelagoPlugin.Log.LogInfo(
                $"Item picked up: '{itemName}', object: '{objectName}' in scene '{sceneName}'");

            if (LocationMapping.TryGetPickupLocation(sceneName, itemName, objectName, out long locationId))
            {
                SilksongArchipelagoPlugin.Instance?.LocationManager.CheckLocation(locationId);
            }
            else
            {
                SilksongArchipelagoPlugin.Log.LogWarning(
                    $"No matching Archipelago location found for item '{itemName}' in scene '{sceneName}'");
            }
        }
    }
}
