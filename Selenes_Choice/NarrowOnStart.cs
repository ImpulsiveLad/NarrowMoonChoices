using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(HangarShipDoor), "Start")]
    public class HideMoonsOnStart
    {
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
                    StartSeed = GetLobby.GrabbedLobby; // Intial seed is the last digits of the lobby code
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
            Selenes_Choice.LastUsedSeed = StartSeed; // LastUsedSeed is here to remember the moons if the host closes and reopens the lobby, all of the 4 shuffles use it
            ES3.Save<int>("LastUsedSeed", Selenes_Choice.LastUsedSeed, GameNetworkManager.Instance.currentSaveFileName);

            List<ExtendedLevel> allLevels = PatchedContent.ExtendedLevels.Where(level => !ListProcessor.Instance.ExclusionList.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();

            foreach (ExtendedLevel level in allLevels)
            {
                level.IsRouteLocked = false;
                level.IsRouteHidden = true;
            }
            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList();
            List<ExtendedLevel> paidLevels = allLevels.Where(level => level.RoutePrice != 0).ToList();

            ExtendedLevel randomFreeLevel = null;

            Selenes_Choice.instance.mls.LogInfo("Start Seed " + StartSeed);
            Random.State originalState = Random.state;
            Random.InitState(StartSeed);

            UpdateConfig.Instance.BracketMoons();

            int randomFreeIndex = Random.Range(0, freeLevels.Count); // gets the one holy "safety moon"
            randomFreeLevel = freeLevels[randomFreeIndex];
            randomFreeLevel.IsRouteHidden = false;
            if (Selenes_Choice.Config.ClearWeather)
            {
                randomFreeLevel.SelectableLevel.currentWeather = LevelWeatherType.None;
            }
            freeLevels.Remove(randomFreeLevel);
            allLevels.Remove(randomFreeLevel);
            Selenes_Choice.PreviousSafetyMoon = randomFreeLevel;

            Selenes_Choice.instance.mls.LogInfo("Safety Moon: " + randomFreeLevel.SelectableLevel.PlanetName);

            for (int i = 0; i < (UpdateConfig.freeMoonCount - 1); i++) // gets other free moons
            {
                int randomExtraFreeIndex = Random.Range(0, freeLevels.Count);
                ExtendedLevel additionalFreeLevels = freeLevels[randomExtraFreeIndex];
                additionalFreeLevels.IsRouteHidden = false;
                freeLevels.Remove(additionalFreeLevels);
                allLevels.Remove(additionalFreeLevels);
            }

            if (UpdateConfig.paidMoonCount != 0)
            {
                for (int i = 0; i < UpdateConfig.paidMoonCount; i++) // gets some paid moons
                {
                    int PaidIndex = Random.Range(0, paidLevels.Count);
                    ExtendedLevel PaidLevel = paidLevels[PaidIndex];
                    PaidLevel.IsRouteHidden = false;
                    paidLevels.Remove(PaidLevel);
                    allLevels.Remove(PaidLevel);
                }
            }

            for (int i = 0; i < UpdateConfig.randomMoonCount; i++) // gets any other additional moons
            {
                int randomIndex = Random.Range(0, allLevels.Count);
                ExtendedLevel randomLevel = allLevels[randomIndex];
                randomLevel.IsRouteHidden = false;
                allLevels.Remove(randomLevel);
            }
            Random.state = originalState;

            foreach (ExtendedLevel level in allLevels)
            {
                if (level.IsRouteHidden)
                {
                    level.IsRouteLocked = true;
                }
            }

            if (TimeOfDay.Instance.daysUntilDeadline == 0)
            {
                ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                int CompanyID = gordionLevel.SelectableLevel.levelID;

                StartOfRound.Instance.ChangeLevel(CompanyID);

                StartOfRound.Instance.ChangePlanet();
            }
            else
            {
                if (randomFreeLevel != null && randomFreeLevel != LevelManager.CurrentExtendedLevel)
                {
                    int randomFreeLevelId = randomFreeLevel.SelectableLevel.levelID;

                    StartOfRound.Instance.ChangeLevel(randomFreeLevelId);

                    StartOfRound.Instance.ChangePlanet();
                }
            }
        }
    }
}
