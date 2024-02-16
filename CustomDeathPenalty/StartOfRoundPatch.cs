using HarmonyLib;

namespace CustomDeathPenalty
{
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    public class StartOfRound_Awake_Patch
    {
        static void Prefix(StartOfRound __instance)
        {
            foreach (var level in __instance.levels)
            {
                if (level.sceneName.Contains("CompanyBuilding") || level.PlanetName.Contains("Gordion"))
                {
                    ArrivalSwitch.myReferenceToGordionLevel = level;
                    break;
                }
            }
        }
    }
}