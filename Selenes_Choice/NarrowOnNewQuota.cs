using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using UnityEngine;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(HUDManager), "rackUpNewQuotaText")]
    public class HideMoonsOnNewQuota
    {
        static void Postfix()
        {
            ShareSnT.Instance.StartCoroutine(WaitOnQuota());
        }
        static IEnumerator WaitOnQuota()
        {
            yield return new WaitForSeconds(2);
            ProcessData();
        }
        static void ProcessData()
        {
            if (!Selenes_Choice.Config.DailyOrQuota)
            {
                return;
            }

            CommonShuffle.ShuffleMoons(StartOfRound.Instance.randomMapSeed);

            if (Selenes_Choice.PreviousSafetyMoon != null && Selenes_Choice.PreviousSafetyMoon != LevelManager.CurrentExtendedLevel)
            {
                int PreviousSafetyMoonID = Selenes_Choice.PreviousSafetyMoon.SelectableLevel.levelID;

                StartOfRound.Instance.ChangeLevelServerRpc(PreviousSafetyMoonID, Object.FindObjectOfType<Terminal>().groupCredits);
            }

            ES3.Save("CurrentPlanetID", StartOfRound.Instance.currentLevelID, GameNetworkManager.Instance.currentSaveFileName);
        }
    }
}