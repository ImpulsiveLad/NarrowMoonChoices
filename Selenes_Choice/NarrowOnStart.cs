using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(HangarShipDoor), "Start")]
    public class HideMoonsOnStart
    {
        public static bool OldSave = false;
        public static int StartSeed;
        static void Postfix()
        {
            ListProcessor.Instance.ProcessLists();
            if (!NetworkManager.Singleton.IsHost) // someone that isn't host joins
            {
                ShareSnT.Instance.RequestData(); // Request host data
                ShareSnT.Instance.StartCoroutine(WaitAndProcessData());
            }
            else // host just made a server
            {
                if (ES3.KeyExists("LastUsedSeed", GameNetworkManager.Instance.currentSaveFileName)) // Save file exists
                {
                    Selenes_Choice.LastUsedSeed = ES3.Load<int>("LastUsedSeed", GameNetworkManager.Instance.currentSaveFileName); // Uses last saved moons on the file
                    StartSeed = Selenes_Choice.LastUsedSeed;
                }
                else // New save file
                {
                    StartSeed = Random.Range(1, 100000000);
                }
                ShareSnT.Instance.StartCoroutine(WaitOnStart());
            }
        }
        static IEnumerator WaitAndProcessData() // just in case data takes a few frames
        {
            yield return new WaitUntil(() => ShareSnT.Instance.dataReceivedEvent.WaitOne(0));

            StartSeed = Selenes_Choice.LastUsedSeed;

            ShareSnT.Instance.StartCoroutine(WaitOnStart());
        }
        static IEnumerator WaitOnStart() // Gives some time for terminal formatter to load
        {
            yield return new WaitForSeconds(2);
            ProcessData();
        }
        static void ProcessData()
        {
            CommonShuffle.ShuffleMoons(StartSeed);

            if (TimeOfDay.Instance.daysUntilDeadline == 0)
            {
                ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                int CompanyID = gordionLevel.SelectableLevel.levelID;

                if (gordionLevel != LevelManager.CurrentExtendedLevel)
                {
                    StartOfRound.Instance.ChangeLevelServerRpc(CompanyID, Object.FindObjectOfType<Terminal>().groupCredits);
                }
            }
            else
            {
                if (ES3.KeyExists("OldSave", GameNetworkManager.Instance.currentSaveFileName))
                {
                    OldSave = ES3.Load<bool>("OldSave", GameNetworkManager.Instance.currentSaveFileName);
                    Selenes_Choice.instance.mls.LogInfo("Save Exists");
                }
                else
                {
                    OldSave = false;
                    Selenes_Choice.instance.mls.LogInfo("Save does not exist yet");
                }
                if (Selenes_Choice.PreviousSafetyMoon != null && Selenes_Choice.PreviousSafetyMoon != LevelManager.CurrentExtendedLevel && !OldSave && NetworkManager.Singleton.IsHost)
                {
                    int PreviousSafetyMoonID = Selenes_Choice.PreviousSafetyMoon.SelectableLevel.levelID;

                    StartOfRound.Instance.ChangeLevelServerRpc(PreviousSafetyMoonID, Object.FindObjectOfType<Terminal>().groupCredits);
                }
            }
            ES3.Save("CurrentPlanetID", StartOfRound.Instance.currentLevelID, GameNetworkManager.Instance.currentSaveFileName);
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
    public class MarkAsSaved
    {
        static void Postfix()
        {
            if (!HideMoonsOnStart.OldSave && Selenes_Choice.Config.DontAutoRouteOnJoin)
            {
                HideMoonsOnStart.OldSave = true;
                ES3.Save<bool>("OldSave", HideMoonsOnStart.OldSave, GameNetworkManager.Instance.currentSaveFileName);
                Selenes_Choice.instance.mls.LogInfo("Saving Save");
            }
        }
    }
}