using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using static Selenes_Choice.Selenes_Choice;

namespace Selenes_Choice
{
    public static class CommonShuffle
    {
        public static int FreeMoonsPicked;
        public static int RareMoonsPicked;
        public static int PaidMoonsPicked;
        public static int RandomMoonsPicked;
        public static string FreMNs;
        public static string RarMNs;
        public static string PaiMNs;
        public static string RanMNs;
        public static void ShuffleMoons(int Seed)
        {
            LastUsedSeed = Seed; // LastUsedSeed is here to remember the moons if the host closes and reopens the lobby, all of the 4 shuffles use it
            ES3.Save<int>("LastUsedSeed", LastUsedSeed, GameNetworkManager.Instance.currentSaveFileName);

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
            List<ExtendedLevel> rareLevels = paidLevels.Where(level => level.RoutePrice >= Config.ValueThreshold).ToList();

            ExtendedLevel randomFreeLevel = null;
            ExtendedLevel Anysafetylevel = null;
            ExtendedLevel paidsafetylevel = null;
            ExtendedLevel raresafetylevel = null;

            instance.mls.LogInfo("Seed " + Seed);
            Random rand = new Random(Seed);

            if (UpdateConfig.freeMoonCount > 0)
            {
                int randomFreeIndex = rand.Next(freeLevels.Count); // gets the one holy "safety moon"
                randomFreeLevel = freeLevels[randomFreeIndex];
                randomFreeLevel.IsRouteHidden = false;
                if (Config.ClearWeather)
                {
                    if (WeatherRegistryCompatibility.enabled)
                    {
                        WeatherRegistryCompatibility.ClearWeatherWithWR(randomFreeLevel);
                    }
                    else
                    {
                        randomFreeLevel.SelectableLevel.currentWeather = LevelWeatherType.None;
                    }
                }
                freeLevels.Remove(randomFreeLevel);
                allLevels.Remove(randomFreeLevel);
                FreeMoonsPicked++;
                if (Config.RememberMoons)
                {
                    UpdateConfig.RecentlyVisitedMoons.Add(randomFreeLevel);
                }
                PreviousSafetyMoon = randomFreeLevel;

                instance.mls.LogInfo("Free Safety Moon: " + randomFreeLevel.SelectableLevel.PlanetName);
                FreMNs += $"{randomFreeLevel.NumberlessPlanetName},";

                UpdateConfig.freeMoonCount--;
            }
            else if (UpdateConfig.randomMoonCount > 0)
            {
                int Anysafetyindex = rand.Next(allLevels.Count);
                Anysafetylevel = allLevels[Anysafetyindex];
                Anysafetylevel.IsRouteHidden = false;
                if (Config.ClearWeather)
                {
                    if (WeatherRegistryCompatibility.enabled)
                    {
                        WeatherRegistryCompatibility.ClearWeatherWithWR(Anysafetylevel);
                    }
                    else
                    {
                        Anysafetylevel.SelectableLevel.currentWeather = LevelWeatherType.None;
                    }
                }
                allLevels.Remove(Anysafetylevel);
                paidLevels.Remove(Anysafetylevel);
                rareLevels.Remove(Anysafetylevel);
                RandomMoonsPicked++;
                if (Config.RememberMoons)
                {
                    UpdateConfig.RecentlyVisitedMoons.Add(Anysafetylevel);
                }
                PreviousSafetyMoon = Anysafetylevel;
                if (Config.DiscountMoons)
                {
                    PriceManager.originalPrices[Anysafetylevel] = Anysafetylevel.RoutePrice;
                    int seed = Seed + glump;
                    Random discountRand = new Random(seed);
                    int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                    int newRandomNumber = 100 - randomNumber;
                    float discountValue = newRandomNumber / 100f;
                    Anysafetylevel.RoutePrice = (int)(Anysafetylevel.RoutePrice * discountValue);
                    glump++;
                }

                instance.mls.LogInfo("Random Safety Moon: " + Anysafetylevel.SelectableLevel.PlanetName);
                RanMNs += $"{Anysafetylevel.NumberlessPlanetName},";

                UpdateConfig.randomMoonCount--;
            }
            else if (UpdateConfig.paidMoonCount > 0)
            {
                int paidsafetyindex = rand.Next(paidLevels.Count);
                paidsafetylevel = paidLevels[paidsafetyindex];
                paidsafetylevel.IsRouteHidden = false;
                if (Config.ClearWeather)
                {
                    if (WeatherRegistryCompatibility.enabled)
                    {
                        WeatherRegistryCompatibility.ClearWeatherWithWR(paidsafetylevel);
                    }
                    else
                    {
                        paidsafetylevel.SelectableLevel.currentWeather = LevelWeatherType.None;
                    }
                }
                allLevels.Remove(paidsafetylevel);
                paidLevels.Remove(paidsafetylevel);
                rareLevels.Remove(paidsafetylevel);
                PaidMoonsPicked++;
                if (Config.RememberMoons)
                {
                    UpdateConfig.RecentlyVisitedMoons.Add(paidsafetylevel);
                }
                PreviousSafetyMoon = paidsafetylevel;
                if (Config.DiscountMoons)
                {
                    PriceManager.originalPrices[paidsafetylevel] = paidsafetylevel.RoutePrice;
                    int seed = Seed + glump;
                    Random discountRand = new Random(seed);
                    int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                    int newRandomNumber = 100 - randomNumber;
                    float discountValue = newRandomNumber / 100f;
                    paidsafetylevel.RoutePrice = (int)(paidsafetylevel.RoutePrice * discountValue);
                    glump++;
                }

                instance.mls.LogInfo("Paid Safety Moon: " + paidsafetylevel.SelectableLevel.PlanetName);
                PaiMNs += $"{paidsafetylevel.NumberlessPlanetName},";

                UpdateConfig.paidMoonCount--;
            }
            else if (UpdateConfig.rareMoonCount > 0)
            {
                int raresafetyindex = rand.Next(rareLevels.Count);
                raresafetylevel = rareLevels[raresafetyindex];
                raresafetylevel.IsRouteHidden = false;
                if (Config.ClearWeather)
                {
                    if (WeatherRegistryCompatibility.enabled)
                    {
                        WeatherRegistryCompatibility.ClearWeatherWithWR(raresafetylevel);
                    }
                    else
                    {
                        raresafetylevel.SelectableLevel.currentWeather = LevelWeatherType.None;
                    }
                }
                allLevels.Remove(raresafetylevel);
                paidLevels.Remove(raresafetylevel);
                rareLevels.Remove(raresafetylevel);
                RareMoonsPicked++;
                if (Config.RememberMoons)
                {
                    UpdateConfig.RecentlyVisitedMoons.Add(raresafetylevel);
                }
                PreviousSafetyMoon = raresafetylevel;
                if (Config.DiscountMoons)
                {
                    PriceManager.originalPrices[raresafetylevel] = raresafetylevel.RoutePrice;
                    int seed = Seed + glump;
                    Random discountRand = new Random(seed);
                    int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                    int newRandomNumber = 100 - randomNumber;
                    float discountValue = newRandomNumber / 100f;
                    raresafetylevel.RoutePrice = (int)(raresafetylevel.RoutePrice * discountValue);
                    glump++;
                }

                instance.mls.LogInfo("Rare Safety Moon: " + raresafetylevel.SelectableLevel.PlanetName);
                RarMNs += $"{raresafetylevel.NumberlessPlanetName},";

                UpdateConfig.rareMoonCount--;
            }
            else
            {
                instance.mls.LogInfo("You gotta have at least one moon in the shuffle foo");
            }

            if (UpdateConfig.freeMoonCount > 0)
            {
                for (int i = 0; i < UpdateConfig.freeMoonCount; i++) // gets other free moons
                {
                    int randomExtraFreeIndex = rand.Next(freeLevels.Count);
                    ExtendedLevel additionalFreeLevels = freeLevels[randomExtraFreeIndex];
                    additionalFreeLevels.IsRouteHidden = false;
                    freeLevels.Remove(additionalFreeLevels);
                    allLevels.Remove(additionalFreeLevels);
                    FreeMoonsPicked++;
                    if (Config.RememberMoons && Config.RememberAll)
                    {
                        UpdateConfig.RecentlyVisitedMoons.Add(additionalFreeLevels);
                    }
                    FreMNs += $"{additionalFreeLevels.NumberlessPlanetName},";
                }
            }

            if (UpdateConfig.rareMoonCount > 0)
            {
                for (int i = 0; i < UpdateConfig.rareMoonCount; i++)
                {
                    int RareIndex = rand.Next(rareLevels.Count);
                    ExtendedLevel RareLevel = rareLevels[RareIndex];
                    RareLevel.IsRouteHidden = false;
                    rareLevels.Remove(RareLevel);
                    paidLevels.Remove(RareLevel);
                    allLevels.Remove(RareLevel);
                    RareMoonsPicked++;
                    if (Config.RememberMoons && Config.RememberAll)
                    {
                        UpdateConfig.RecentlyVisitedMoons.Add(RareLevel);
                    }
                    if (Config.DiscountMoons)
                    {
                        PriceManager.originalPrices[RareLevel] = RareLevel.RoutePrice;
                        int seed = Seed + glump;
                        Random discountRand = new Random(seed);
                        int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                        int newRandomNumber = 100 - randomNumber;
                        float discountValue = newRandomNumber / 100f;
                        RareLevel.RoutePrice = (int)(RareLevel.RoutePrice * discountValue);
                        glump++;
                    }
                    RarMNs += $"{RareLevel.NumberlessPlanetName},";
                }
            }

            if (UpdateConfig.paidMoonCount > 0)
            {
                for (int i = 0; i < UpdateConfig.paidMoonCount; i++) // gets some paid moons
                {
                    int PaidIndex = rand.Next(paidLevels.Count);
                    ExtendedLevel PaidLevel = paidLevels[PaidIndex];
                    PaidLevel.IsRouteHidden = false;
                    paidLevels.Remove(PaidLevel);
                    allLevels.Remove(PaidLevel);
                    PaidMoonsPicked++;
                    if (Config.RememberMoons && Config.RememberAll)
                    {
                        UpdateConfig.RecentlyVisitedMoons.Add(PaidLevel);
                    }
                    if (Config.DiscountMoons)
                    {
                        PriceManager.originalPrices[PaidLevel] = PaidLevel.RoutePrice;
                        int seed = Seed + glump;
                        Random discountRand = new Random(seed);
                        int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                        int newRandomNumber = 100 - randomNumber;
                        float discountValue = newRandomNumber / 100f;
                        PaidLevel.RoutePrice = (int)(PaidLevel.RoutePrice * discountValue);
                        glump++;
                    }
                    PaiMNs += $"{PaidLevel.NumberlessPlanetName},";
                }
            }

            if (UpdateConfig.randomMoonCount > 0)
            {
                for (int i = 0; i < UpdateConfig.randomMoonCount; i++) // gets any other additional moons
                {
                    int randomIndex = rand.Next(allLevels.Count);
                    ExtendedLevel randomLevel = allLevels[randomIndex];
                    randomLevel.IsRouteHidden = false;
                    allLevels.Remove(randomLevel);
                    RandomMoonsPicked++;
                    if (Config.RememberMoons && Config.RememberAll)
                    {
                        UpdateConfig.RecentlyVisitedMoons.Add(randomLevel);
                    }
                    if (Config.DiscountMoons)
                    {
                        PriceManager.originalPrices[randomLevel] = randomLevel.RoutePrice;
                        int seed = Seed + glump;
                        Random discountRand = new Random(seed);
                        int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                        int newRandomNumber = 100 - randomNumber;
                        float discountValue = newRandomNumber / 100f;
                        randomLevel.RoutePrice = (int)(randomLevel.RoutePrice * discountValue);
                        glump++;
                    }
                    RanMNs += $"{randomLevel.NumberlessPlanetName},";
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
            instance.mls.LogInfo($"Picked Moons:\n                                 Free Count = {FreeMoonsPicked}\n                                 Free Names: {FreMNs}\n                                 Rare Count = {RareMoonsPicked}\n                                 Rare Names: {RarMNs}\n                                 Paid Count = {PaidMoonsPicked}\n                                 Paid Names: {PaiMNs}\n                                 Random Count = {RandomMoonsPicked}\n                                 Random Names: {RanMNs}");
            FreeMoonsPicked = 0;
            RareMoonsPicked = 0;
            PaidMoonsPicked = 0;
            RandomMoonsPicked = 0;
            FreMNs = "";
            RarMNs = "";
            PaiMNs = "";
            RanMNs = "";
        }
    }
}