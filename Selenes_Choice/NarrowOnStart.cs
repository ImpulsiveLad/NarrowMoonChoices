using GameNetcodeStuff;
using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public class HideMoonsOnStart
    {
        public static int StartSeed;
        static void Postfix()
        {
            ListProcessor.ProcessLists();
            if (!NetworkManager.Singleton.IsHost) // someone that isn't host joins
            {
                ShareSnT.Instance.RequestData();
                ShareSnT.Instance.StartCoroutine(WaitAndProcessData());
            }
            else // host just made a server
            {
                if (StartSeed == 0)
                {
                    StartSeed = GetLobby.GrabbedLobby; // first ever moons generated when the host joins are based on the lobbyID
                }
                else
                {
                    StartSeed = ShareSnT.Instance.lastUsedSeedPrev; // This is for if the host leaves and rejoins
                }
                ProcessData();
            }
        }
        static IEnumerator WaitAndProcessData() // just in case data takes a few frames
        {
            yield return new WaitUntil(() => Selenes_Choice.LastUsedSeed != 0);

            StartSeed = Selenes_Choice.LastUsedSeed;

            ProcessData();
        }
        static void ProcessData()
        {
            Selenes_Choice.LastUsedSeed = StartSeed; // LastUsedSeed is here to remember the moons if the host closes and reopens the lobby, all of the 4 shuffles use it

            string ignoreList = Selenes_Choice.Config.IgnoreMoons;
            string blacklist = Selenes_Choice.Config.BlacklistMoons;
            string exclusionlist = string.Join(",", ignoreList, blacklist);

            List<ExtendedLevel> allLevels = PatchedContent.ExtendedLevels.Where(level => !exclusionlist.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();

            foreach (ExtendedLevel level in allLevels)
            {
                level.IsRouteLocked = false;
                level.IsRouteHidden = true;
            }
            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList();

            ExtendedLevel randomFreeLevel = null;

            Selenes_Choice.instance.mls.LogInfo("Start Seed " + StartSeed);
            Random.State originalState = Random.state;
            Random.InitState(StartSeed);

            UpdateConfig.BracketMoons();

            int randomFreeIndex = Random.Range(0, freeLevels.Count); // gets the one holy "safety moon"
            randomFreeLevel = freeLevels[randomFreeIndex];
            randomFreeLevel.IsRouteHidden = false;
            allLevels.Remove(randomFreeLevel);
            freeLevels.Remove(randomFreeLevel);

            Selenes_Choice.instance.mls.LogInfo("Safety Moon: " + randomFreeLevel.SelectableLevel.PlanetName);

            for (int i = 0; i < (UpdateConfig.freeMoonCount - 1); i++) // gets other free moons
            {
                int randomExtraFreeIndex = Random.Range(0, freeLevels.Count);
                ExtendedLevel additionalFreeLevels = freeLevels[randomExtraFreeIndex];
                additionalFreeLevels.IsRouteHidden = false;
                freeLevels.Remove(additionalFreeLevels);
                allLevels.Remove(additionalFreeLevels);
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
