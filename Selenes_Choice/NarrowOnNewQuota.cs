using HarmonyLib;
using LethalLevelLoader;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(HUDManager), "rackUpNewQuotaText")]
    public class HideMoonsOnNewQuota
    {
        static void Postfix()
        {
            ListProcessor.ProcessLists();
            int NewQuotaSeed = TimeOfDay.Instance.profitQuota + GetLobby.GrabbedLobby; // The random moons after getting a new quota will be the new quota and the lobbyID

            Selenes_Choice.LastUsedSeed = NewQuotaSeed;

            string ignoreList = Selenes_Choice.Config.IgnoreMoons;
            string blacklist = Selenes_Choice.Config.BlacklistMoons;
            string treasurelist = Selenes_Choice.Config.TreasureMoons;
            string exclusionlist = string.Join(",", ignoreList, blacklist, treasurelist);

            List<ExtendedLevel> allLevels = PatchedContent.ExtendedLevels.Where(level => !exclusionlist.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();

            foreach (ExtendedLevel level in allLevels)
            {
                level.IsRouteLocked = false;
                level.IsRouteHidden = true;
            }
            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList();

            ExtendedLevel randomFreeLevel = null;

            Selenes_Choice.instance.mls.LogInfo("Start Seed " + NewQuotaSeed);
            Random.State originalState = Random.state;
            Random.InitState(NewQuotaSeed);

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
