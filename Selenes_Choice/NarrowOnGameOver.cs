using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(StartOfRound), "ResetShip")]
    public class HideMoonsOnGameOver
    {
        static void Postfix()
        {
            ShareSnT.Instance.StartCoroutine(WaitOnDeath());
        }
        static IEnumerator WaitOnDeath()
        {
            yield return new WaitForSeconds(2);
            ProcessData();
        }
        static void ProcessData()
        {
            CommonShuffle.ShuffleMoons(StartOfRound.Instance.randomMapSeed);

            if (TimeOfDay.Instance.daysUntilDeadline == 0)
            {
                ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                int CompanyID = gordionLevel.SelectableLevel.levelID;

                if (gordionLevel != LevelManager.CurrentExtendedLevel)
                {
                    StartOfRound.Instance.ChangeLevel(CompanyID);
                    StartOfRound.Instance.ChangePlanet();
                }
            }
            else
            {
                if (Selenes_Choice.PreviousSafetyMoon != null && Selenes_Choice.PreviousSafetyMoon != LevelManager.CurrentExtendedLevel)
                {
                    int PreviousSafetyMoonID = Selenes_Choice.PreviousSafetyMoon.SelectableLevel.levelID;

                    StartOfRound.Instance.ChangeLevel(PreviousSafetyMoonID);
                    StartOfRound.Instance.ChangePlanet();
                }
            }
            ES3.Save("CurrentPlanetID", StartOfRound.Instance.currentLevelID, GameNetworkManager.Instance.currentSaveFileName);
        }
    }
}