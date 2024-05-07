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
            int NewQuotaSeed = TimeOfDay.Instance.profitQuota + GetLobby.GrabbedLobby;

            Selenes_Choice.LastUsedSeed = NewQuotaSeed;

            List<ExtendedLevel> allLevels = PatchedContent.ExtendedLevels.Where(level => !level.ToString().Contains("Gordion") && !level.ToString().Contains("Liquidation") && !level.ContentTags.Any(tag => tag.contentTagName == "Company")).ToList();

            foreach (ExtendedLevel level in allLevels)
            {
                level.IsRouteLocked = false;
                level.IsRouteHidden = true;
            }

            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList();

            ExtendedLevel randomFreeLevel = null;

            if (freeLevels.Count > 0 && allLevels.Count >= (Selenes_Choice.Config.RandomMoonCount.Value - 1))
            {
                Selenes_Choice.instance.mls.LogInfo("New Quota Seed " + NewQuotaSeed);
                Random.State originalState = Random.state;
                Random.InitState(NewQuotaSeed);

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
