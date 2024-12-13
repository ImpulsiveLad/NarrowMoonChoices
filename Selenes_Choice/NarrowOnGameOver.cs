using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

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

            int NewLevel = -1;

            if (TimeOfDay.Instance.daysUntilDeadline == 0)
            {
                ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                int CompanyID = gordionLevel.SelectableLevel.levelID;

                if (gordionLevel != LevelManager.CurrentExtendedLevel)
                {
                    StartOfRound.Instance.ChangeLevel(CompanyID);
                    StartOfRound.Instance.ChangePlanet();
                    NewLevel = CompanyID;
                }
            }
            else
            {
                if (Selenes_Choice.PreviousSafetyMoon != null && Selenes_Choice.PreviousSafetyMoon != LevelManager.CurrentExtendedLevel)
                {
                    int PreviousSafetyMoonID = Selenes_Choice.PreviousSafetyMoon.SelectableLevel.levelID;

                    StartOfRound.Instance.ChangeLevel(PreviousSafetyMoonID);
                    StartOfRound.Instance.ChangePlanet();
                    NewLevel = PreviousSafetyMoonID;
                }
            }
            if (NewLevel != -1 && NetworkManager.Singleton.IsHost)
                ES3.Save("CurrentPlanetID", NewLevel, GameNetworkManager.Instance.currentSaveFileName);
        }
    }
}