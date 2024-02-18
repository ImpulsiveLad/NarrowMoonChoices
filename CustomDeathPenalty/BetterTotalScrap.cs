using HarmonyLib;
using UnityEngine;

namespace CustomDeathPenalty
{
    public static class GlobalVariables
    {
        public static int RemainingScrapInLevel;
    }
    [HarmonyPatch(typeof(StartOfRound), "ShipLeave")]
    public class ShipleaveCalc
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            GlobalVariables.RemainingScrapInLevel = CalculateRemainingScrapInLevel();
        }
        public static int CalculateRemainingScrapInLevel()
        {
            GrabbableObject[] array = Object.FindObjectsOfType<GrabbableObject>();
            int remainingValue = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].itemProperties.isScrap && !array[i].isInShipRoom && !array[i].isInElevator && !array[i].scrapPersistedThroughRounds)
                {
                    remainingValue += array[i].scrapValue;
                }
            }
            return remainingValue;
        }
    }

    [HarmonyPatch(typeof(HUDManager), "FillEndGameStats")]
    public class HUDManagerPatch
    {
        [HarmonyPostfix]
        public static void FillEndGameStatsPostfix(HUDManager __instance, int scrapCollected)
        {
            float finalCount = (int)(scrapCollected + GlobalVariables.RemainingScrapInLevel);
            __instance.statsUIElements.quotaDenominator.text = finalCount.ToString();
        }
    }
}
