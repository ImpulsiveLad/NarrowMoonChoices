﻿using HarmonyLib;
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
        public static int DaysSpent;
        public static int StartSeed;
        public static int glump;
        static void Postfix()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("mrov.WeatherRegistry", out var pluginInfo))
            {
                var version = pluginInfo.Metadata.Version;
                Selenes_Choice.LoadedWR = true;
                Selenes_Choice.instance.mls.LogInfo($"Detected Weather Registry version {version}, changing weather clearer...");
            }
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

            Selenes_Choice.instance.mls.LogInfo("Start Seed " + StartSeed);
            System.Random rand = new System.Random(StartSeed);

            int randomFreeIndex = rand.Next(freeLevels.Count); // gets the one holy "safety moon"
            randomFreeLevel = freeLevels[randomFreeIndex];
            randomFreeLevel.IsRouteHidden = false;
            if (Selenes_Choice.Config.ClearWeather)
            {
                if (Selenes_Choice.LoadedWR)
                {
                    WeatherRegistry.WeatherController.ChangeWeather( randomFreeLevel.SelectableLevel, LevelWeatherType.None);
                }
                else
                {
                    randomFreeLevel.SelectableLevel.currentWeather = LevelWeatherType.None;
                }
            }
            freeLevels.Remove(randomFreeLevel);
            allLevels.Remove(randomFreeLevel);
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
                        int seed = StartSeed + glump;
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
                    int seed = StartSeed + glump;
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

            if (TimeOfDay.Instance.daysUntilDeadline == 0 && NetworkManager.Singleton.IsHost)
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
                if (ES3.KeyExists("DaysSpent", GameNetworkManager.Instance.currentSaveFileName))
                {
                    DaysSpent = ES3.Load<int>("DaysSpent", GameNetworkManager.Instance.currentSaveFileName);
                }
                else
                {
                    DaysSpent = 0;
                }
                Selenes_Choice.instance.mls.LogInfo("Days Spent: " + DaysSpent);
                if (randomFreeLevel != null && randomFreeLevel != LevelManager.CurrentExtendedLevel && DaysSpent == 0 && NetworkManager.Singleton.IsHost)
                {
                    int randomFreeLevelId = randomFreeLevel.SelectableLevel.levelID;

                    StartOfRound.Instance.ChangeLevelServerRpc(randomFreeLevelId, Object.FindObjectOfType<Terminal>().groupCredits);
                }
            }
            ES3.Save("CurrentPlanetID", StartOfRound.Instance.currentLevelID, GameNetworkManager.Instance.currentSaveFileName);
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
    public class IncrementDaysSpent
    {
        static void Postfix()
        {
            Selenes_Choice.stats.daysSpent++;
            ES3.Save<int>("DaysSpent", Selenes_Choice.stats.daysSpent, GameNetworkManager.Instance.currentSaveFileName);
        }
    }
}