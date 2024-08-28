﻿using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(HUDManager), "rackUpNewQuotaText")]
    public class HideMoonsOnNewQuota
    {
        public static int glump;
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
            int NewQuotaSeed = TimeOfDay.Instance.profitQuota + GetLobby.GrabbedLobby; // The random moons after getting a new quota will be the new quota and the lobbyID

            Selenes_Choice.LastUsedSeed = NewQuotaSeed;
            ES3.Save<int>("LastUsedSeed", Selenes_Choice.LastUsedSeed, GameNetworkManager.Instance.currentSaveFileName);

            PriceManager.ResetPrices();
            UpdateConfig.Instance.BracketMoons();
            List<ExtendedLevel> Levels = PatchedContent.ExtendedLevels.Where(level => !ListProcessor.Instance.ExclusionList.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();
            List<ExtendedLevel> allLevels = Levels.Where(level => !UpdateConfig.RecentlyVisitedMoons.Contains(level)).ToList();

            foreach (ExtendedLevel level in allLevels)
            {
                level.IsRouteLocked = false;
                level.IsRouteHidden = true;
            }
            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList();
            List<ExtendedLevel> paidLevels = allLevels.Where(level => level.RoutePrice != 0).ToList();

            ExtendedLevel randomFreeLevel = null;

            Selenes_Choice.instance.mls.LogInfo("Start Seed " + NewQuotaSeed);
            System.Random rand = new System.Random(NewQuotaSeed);

            int randomFreeIndex = rand.Next(freeLevels.Count); // gets the one holy "safety moon"
            randomFreeLevel = freeLevels[randomFreeIndex];
            randomFreeLevel.IsRouteHidden = false;
            if (Selenes_Choice.Config.ClearWeather)
            {
                if (Selenes_Choice.LoadedWR)
                {
                    WeatherRegistry.WeatherController.ChangeWeather(randomFreeLevel.SelectableLevel, LevelWeatherType.None);
                }
                else
                {
                    randomFreeLevel.SelectableLevel.currentWeather = LevelWeatherType.None;
                }
            }
            allLevels.Remove(randomFreeLevel);
            freeLevels.Remove(randomFreeLevel);
            if (Selenes_Choice.Config.RememberMoons)
            {
                UpdateConfig.RecentlyVisitedMoons.Add(randomFreeLevel);
            }
            Selenes_Choice.PreviousSafetyMoon = randomFreeLevel;

            Selenes_Choice.instance.mls.LogInfo("Safety Moon: " + randomFreeLevel.SelectableLevel.PlanetName);

            for (int i = 0; i < (UpdateConfig.freeMoonCount - 1); i++) // gets other free moons
            {
                int randomExtraFreeIndex = rand.Next(freeLevels.Count);
                ExtendedLevel additionalFreeLevels = freeLevels[randomExtraFreeIndex];
                additionalFreeLevels.IsRouteHidden = false;
                freeLevels.Remove(additionalFreeLevels);
                allLevels.Remove(additionalFreeLevels);
                if (Selenes_Choice.Config.RememberMoons && Selenes_Choice.Config.RememberAll)
                {
                    UpdateConfig.RecentlyVisitedMoons.Add(additionalFreeLevels);
                }
            }

            if (UpdateConfig.paidMoonCount != 0)
            {
                for (int i = 0; i < UpdateConfig.paidMoonCount; i++) // gets some paid moons
                {
                    int PaidIndex = rand.Next(paidLevels.Count);
                    ExtendedLevel PaidLevel = paidLevels[PaidIndex];
                    PaidLevel.IsRouteHidden = false;
                    paidLevels.Remove(PaidLevel);
                    allLevels.Remove(PaidLevel);
                    if (Selenes_Choice.Config.RememberMoons && Selenes_Choice.Config.RememberAll)
                    {
                        UpdateConfig.RecentlyVisitedMoons.Add(PaidLevel);
                    }
                    if (Selenes_Choice.Config.DiscountMoons)
                    {
                        PriceManager.originalPrices[PaidLevel] = PaidLevel.RoutePrice;
                        int seed = NewQuotaSeed + glump;
                        System.Random discountRand = new System.Random(seed);
                        int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                        int newRandomNumber = 100 - randomNumber;
                        float discountValue = newRandomNumber / 100f;
                        PaidLevel.RoutePrice = (int)(PaidLevel.RoutePrice * discountValue);
                        glump++;
                    }
                }
            }

            for (int i = 0; i < UpdateConfig.randomMoonCount; i++) // gets any other additional moons
            {
                int randomIndex = rand.Next(allLevels.Count);
                ExtendedLevel randomLevel = allLevels[randomIndex];
                randomLevel.IsRouteHidden = false;
                allLevels.Remove(randomLevel);
                if (Selenes_Choice.Config.RememberMoons && Selenes_Choice.Config.RememberAll)
                {
                    UpdateConfig.RecentlyVisitedMoons.Add(randomLevel);
                }
                if (Selenes_Choice.Config.DiscountMoons)
                {
                    PriceManager.originalPrices[randomLevel] = randomLevel.RoutePrice;
                    int seed = NewQuotaSeed + glump;
                    System.Random discountRand = new System.Random(seed);
                    int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                    int newRandomNumber = 100 - randomNumber;
                    float discountValue = newRandomNumber / 100f;
                    randomLevel.RoutePrice = (int)(randomLevel.RoutePrice * discountValue);
                    glump++;
                }
            }
            glump = 0;

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

                StartOfRound.Instance.ChangeLevelServerRpc(randomFreeLevelId, Object.FindObjectOfType<Terminal>().groupCredits);
            }

            ES3.Save("CurrentPlanetID", StartOfRound.Instance.currentLevelID, GameNetworkManager.Instance.currentSaveFileName);
        }
    }
}