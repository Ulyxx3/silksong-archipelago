using HarmonyLib;

namespace SilksongArchipelago.Patches
{
    /// <summary>
    /// Detects enemy and boss deaths, reporting boss check completion
    /// and triggering victory when a goal boss (Phantom, Last Judge, Lost Lace) is defeated.
    /// </summary>
    [HarmonyPatch(typeof(HealthManager), nameof(HealthManager.Die), typeof(float?), typeof(AttackTypes), typeof(bool))]
    public static class Patch_HealthManager_Die
    {
        static void Postfix(HealthManager __instance)
        {
            string enemyName = __instance.gameObject.name;

            SilksongArchipelagoPlugin.Log.LogInfo($"Entity defeated: '{enemyName}'");

            // Goal detection
            var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
            if (client != null && client.IsConnected)
            {
                long goalOption = 2; // Default True Ending
                if (client.SlotData.TryGetValue("goal", out var gObj) && gObj is long gVal)
                {
                    goalOption = gVal;
                }

                if (goalOption == 0 && (enemyName.Contains("Phantom") || enemyName.Contains("Last Judge")))
                {
                    SilksongArchipelagoPlugin.Log.LogInfo("Goal achieved: Defeated Act 2 Boss!");
                    client.SendGoalCompleted();
                }
                else if (goalOption == 2 && enemyName.Contains("Lost Lace"))
                {
                    SilksongArchipelagoPlugin.Log.LogInfo("Goal achieved: Defeated Lost Lace (True Ending)!");
                    client.SendGoalCompleted();
                }
            }
        }
    }
}
