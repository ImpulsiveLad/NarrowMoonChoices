using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

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

            int NewLevel = -1;
            if (NetworkManager.Singleton.IsHost)
            {
                if (Selenes_Choice.PreviousSafetyMoon != null && Selenes_Choice.PreviousSafetyMoon != LevelManager.CurrentExtendedLevel)
                {
                    int PreviousSafetyMoonID = Selenes_Choice.PreviousSafetyMoon.SelectableLevel.levelID;

                    StartOfRound.Instance.ChangeLevelClientRpc(PreviousSafetyMoonID, Object.FindObjectOfType<Terminal>().groupCredits);
                    NewLevel = PreviousSafetyMoonID;
                }
                if (NewLevel != -1)
                    ES3.Save("CurrentPlanetID", NewLevel, GameNetworkManager.Instance.currentSaveFileName);
            }
        }
    }
}