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
            if (!NetworkManager.Singleton.IsHost)
            {
                ShareSnT.Instance.RequestData();
                ShareSnT.Instance.StartCoroutine(WaitAndProcessData());
            }
            else
            {
                if (StartSeed == 0)
                {
                    StartSeed = GetLobby.GrabbedLobby;
                }
                else
                {
                    StartSeed = ShareSnT.Instance.lastUsedSeedPrev;
                }
                ProcessData();
            }
        }
        static IEnumerator WaitAndProcessData()
        {
            yield return new WaitUntil(() => Selenes_Choice.LastUsedSeed != 0);

            StartSeed = Selenes_Choice.LastUsedSeed;

            ProcessData();
        }
        static void ProcessData()
        {
            Selenes_Choice.LastUsedSeed = StartSeed;

            string blacklist = Selenes_Choice.Config.IgnoreMoons;

            List<ExtendedLevel> allLevels = PatchedContent.ExtendedLevels.Where(level => !blacklist.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();


            foreach (ExtendedLevel level in allLevels)
            {
                level.IsRouteLocked = false;
                level.IsRouteHidden = true;
            }
            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList();

            ExtendedLevel randomFreeLevel = null;

            if (freeLevels.Count > 0 && allLevels.Count >= (Selenes_Choice.Config.RandomMoonCount.Value - 1))
            {
                Selenes_Choice.instance.mls.LogInfo("Start Seed " + StartSeed);
                Random.State originalState = Random.state;
                Random.InitState(StartSeed);

                int randomFreeIndex = Random.Range(0, freeLevels.Count);
                randomFreeLevel = freeLevels[randomFreeIndex];
                randomFreeLevel.IsRouteHidden = false;
                allLevels.Remove(randomFreeLevel);

                Selenes_Choice.instance.mls.LogInfo("Safety Moon: " + randomFreeLevel.SelectableLevel.PlanetName);

                for (int i = 0; i < (Selenes_Choice.Config.RandomMoonCount.Value - 1); i++)
                {
                    int randomIndex = Random.Range(0, allLevels.Count);
                    ExtendedLevel randomLevel = allLevels[randomIndex];
                    randomLevel.IsRouteHidden = false;
                    allLevels.RemoveAt(randomIndex);
                }
                Random.state = originalState;
            }
            else
            {
                Selenes_Choice.instance.mls.LogInfo("Uh oh, the config value for moon count is higher than the actual amount of moons ! ! !");
            }
            foreach (ExtendedLevel level in allLevels)
            {
                if (level.IsRouteHidden)
                {
                    level.IsRouteLocked = true;
                }
            }
            if (randomFreeLevel != null && randomFreeLevel != LevelManager.CurrentExtendedLevel)
            {
                int randomFreeLevelId = randomFreeLevel.SelectableLevel.levelID;

                StartOfRound.Instance.ChangeLevel(randomFreeLevelId);

                StartOfRound.Instance.ChangePlanet();
            }
        }
    }
}
