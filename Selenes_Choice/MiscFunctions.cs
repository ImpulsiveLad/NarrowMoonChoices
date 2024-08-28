using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using LethalLevelLoader;
using System.Collections.Generic;
using System.Threading;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(Lobby), "get_Id")]
    public static class GetLobby
    {
        public static int GrabbedLobby;
        public static int LastLoggedLobby;
        static void Postfix(Lobby __instance, ref SteamId __result)
        {
            ulong LobbyID = __result.Value;

            GrabbedLobby = (int)(LobbyID % 1000000000); // Takes only the last 9 digits of the LobbyID

            if (GrabbedLobby != LastLoggedLobby)
            {
                Selenes_Choice.instance.mls.LogInfo("LobbyID: " + GrabbedLobby);
                LastLoggedLobby = GrabbedLobby;
            }
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "ResetShip")]
    public static class ResetShipPatch
    {
        public static int TimesCalled = 0;
        static void Postfix()
        {
            TimesCalled++; // Counts times the ship is reset (basically game overs)
        }
    }
    public class ShareSnT : NetworkBehaviour // this is all just to transmit 2 variables to late players
    {
        public static ShareSnT Instance { get; private set; }

        public int lastUsedSeedPrev;
        public int timesCalledPrev;
        public ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

        private void Awake()
        {
            Instance = this;

            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, OnMessageReceived);
        }
        [Tooltip("HostSnT")]
        public string MessageName = "Host Seed And Game Overs Count";
        public void RequestData()
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                var writer = new FastBufferWriter(0, Allocator.Temp);
                NetworkManager.CustomMessagingManager.SendNamedMessage(MessageName, NetworkManager.ServerClientId, writer);
            }
        }
        public void UpdateAndSendData()
        {
            using (var writer = new FastBufferWriter(12, Allocator.Temp))
            {
                writer.WriteValueSafe(lastUsedSeedPrev);
                writer.WriteValueSafe(timesCalledPrev);

                NetworkManager.CustomMessagingManager.SendNamedMessage(MessageName, NetworkManager.ConnectedClientsIds, writer);
            }
        }
        private void OnMessageReceived(ulong clientId, FastBufferReader reader)
        {
            if (NetworkManager.Singleton.IsHost && clientId != NetworkManager.ServerClientId)
            {
                lastUsedSeedPrev = Selenes_Choice.LastUsedSeed;
                timesCalledPrev = ResetShipPatch.TimesCalled;
                dataReceivedEvent.Set();

                UpdateAndSendData();
                Selenes_Choice.instance.mls.LogInfo("Host received message");
            }
            else if (!NetworkManager.Singleton.IsHost)
            {
                reader.ReadValue(out int lastUsedSeedPrev);
                reader.ReadValue(out int timesCalledPrev);
                Selenes_Choice.LastUsedSeed = lastUsedSeedPrev;
                ResetShipPatch.TimesCalled = timesCalledPrev;
                dataReceivedEvent.Set();
                Selenes_Choice.instance.mls.LogInfo("Client received message");
            }
        }
        private void Update()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                if (Selenes_Choice.LastUsedSeed != lastUsedSeedPrev || ResetShipPatch.TimesCalled != timesCalledPrev)
                {
                    UpdateAndSendData();

                    lastUsedSeedPrev = Selenes_Choice.LastUsedSeed;
                    timesCalledPrev = ResetShipPatch.TimesCalled;
                    Selenes_Choice.instance.mls.LogInfo("Host has saved new seed, " + lastUsedSeedPrev);
                    Selenes_Choice.instance.mls.LogInfo("Host has saved new ejection, " + timesCalledPrev);
                }
            }
        }
    }
    [HarmonyPatch(typeof(HangarShipDoor), "Start")]
    public static class AnchorTheShare // This just exist to ..... put scripts on the ship door for safekeeping lol
    {
        static void Postfix(HangarShipDoor __instance)
        {
            if (__instance.gameObject.GetComponent<ShareSnT>() == null)
            {
                __instance.gameObject.AddComponent<ShareSnT>();
            }
        }
    }
    public class UpdateConfig
    {
        private static readonly UpdateConfig _instance = new UpdateConfig();

        public static int freeMoonCount = Selenes_Choice.Config.FreeMoonCount;
        public static int paidMoonCount = Selenes_Choice.Config.PaidMoonCount;
        public static int randomMoonCount = Selenes_Choice.Config.RandomMoonCount;

        public static int minDiscount = Selenes_Choice.Config.MinDiscount;
        public static int maxDiscount = Selenes_Choice.Config.MaxDiscount; // all these 5 above are there to dynamically bracket data safely without touching the config values

        public static List<ExtendedLevel> RecentlyVisitedMoons = new List<ExtendedLevel>(); // levels that have been recently visited
        public static Dictionary<ExtendedLevel, int> DaysOnRecentlyVisitedList = new Dictionary<ExtendedLevel, int>(); // dictionary of how long its been since a RVM was visited
        public static List<string> RVMString = new List<string>(); // string version of RVM (for savedata)
        public static Dictionary<string, int> DRVString = new Dictionary<string, int>(); // string version of DRV (for savedata)
        public static List<ExtendedLevel> levelsToRemove = new List<ExtendedLevel>(); // short lived list of moons to remove completely from RVMs
        public static Dictionary<ExtendedLevel, int> ListStatus = new Dictionary<ExtendedLevel, int>(); // for logging

        public static bool LoadedSaveData = false; // marks if it has checked for save data and if so, loaded it
        public static bool IncreaseDays = false; // so it doesn't increase the DRV values on joining
        public static string MoonStatusUpdate = "RVL Status:"; // logging

        private UpdateConfig() { }
        public static UpdateConfig Instance
        {
            get
            {
                return _instance;
            }
        }
        public void BracketMoons()
        {
            if (!LoadedSaveData) // this loop only runs when you first join a session
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    if (ES3.KeyExists("RecentlyVisitedMoonsList", GameNetworkManager.Instance.currentSaveFileName) && ES3.KeyExists("DaysSinceLastVisit", GameNetworkManager.Instance.currentSaveFileName))
                    {
                        RVMString = ES3.Load<List<string>>("RecentlyVisitedMoonsList", GameNetworkManager.Instance.currentSaveFileName);
                        DRVString = ES3.Load<Dictionary<string, int>>("DaysSinceLastVisit", GameNetworkManager.Instance.currentSaveFileName); // loads stringified data only on host

                        RecentlyVisitedMoons.Clear();
                        DaysOnRecentlyVisitedList.Clear(); // empty list and dict from last session

                        List<ExtendedLevel> AllELs = PatchedContent.ExtendedLevels;
                        foreach (ExtendedLevel level in AllELs) // assigns the real RVM list and DRV dict based on the saved strings (ExtendedLevel is not built into ES3 obv)
                        {
                            if (RVMString.Contains(level.NumberlessPlanetName))
                            {
                                RecentlyVisitedMoons.Add(level);

                                if (DRVString.ContainsKey(level.NumberlessPlanetName))
                                {
                                    DaysOnRecentlyVisitedList[level] = DRVString[level.NumberlessPlanetName];
                                }
                                else
                                {
                                    DaysOnRecentlyVisitedList[level] = 0;
                                }
                            }
                        }
                        RVMString.Clear();
                        DRVString.Clear(); // clear out the string list and dict to prevent data from getting reset to an older version of a save
                        Selenes_Choice.instance.mls.LogInfo("Loaded Saved RVL data.");
                    }
                }
                else // same as host for the client, the RVMString and DRVString should be sent via rpc on joining
                {
                    RecentlyVisitedMoons.Clear();
                    DaysOnRecentlyVisitedList.Clear();

                    List<ExtendedLevel> AllELs = PatchedContent.ExtendedLevels;
                    foreach (ExtendedLevel level in AllELs)
                    {
                        if (RVMString.Contains(level.NumberlessPlanetName))
                        {
                            RecentlyVisitedMoons.Add(level);

                            if (DRVString.ContainsKey(level.NumberlessPlanetName))
                            {
                                DaysOnRecentlyVisitedList[level] = DRVString[level.NumberlessPlanetName];
                            }
                            else
                            {
                                DaysOnRecentlyVisitedList[level] = 0;
                            }
                        }
                    }
                    RVMString.Clear();
                    DRVString.Clear(); // clear out the string list and dict to prevent data from getting reset to an older version of a save
                    Selenes_Choice.instance.mls.LogInfo("Loaded Saved RVL data.");
                }
                LoadedSaveData = true; // if there was savedata then it is now using that, otherwise then it skipped over that
            }

            foreach (ExtendedLevel level in RecentlyVisitedMoons)
            {
                level.IsRouteLocked = true; // lock/hide RVMs
                level.IsRouteHidden = true;
                if (level.NumberlessPlanetName == "Gordion") // the company cannot be locked that would be cring
                {
                    levelsToRemove.Add(level);
                }

                if (!ListStatus.ContainsKey(level))
                {
                    ListStatus[level] = -1; // just to avoid missing key errors
                }

                if (IncreaseDays)// this loop doesn't run when you first join a session
                {
                    if (DaysOnRecentlyVisitedList.ContainsKey(level) && LevelManager.CurrentExtendedLevel.NumberlessPlanetName != "Gordion") // increment days if you aren't on gordion and the key exists
                    {
                        DaysOnRecentlyVisitedList[level]++;
                        DRVString[level.NumberlessPlanetName]++;
                        ListStatus[level] = 0;
                    }
                    if (DaysOnRecentlyVisitedList.ContainsKey(level) && LevelManager.CurrentExtendedLevel.NumberlessPlanetName == "Gordion") // don't increment on gordion when it has a day value
                    {
                        ListStatus[level] = 4;
                    }
                    if (!DaysOnRecentlyVisitedList.ContainsKey(level)) // adds day count at day 0 if it doesn't have one yet
                    {
                        DaysOnRecentlyVisitedList[level] = 0;
                        ListStatus[level] = 1;
                    }
                    if (TimeOfDay.Instance.daysUntilDeadline < 0 && ListStatus[level] == 1) // if it was just added alongside when it routed to gordion, it will not be considered recently visited/selected
                    {
                        levelsToRemove.Add(level);
                        ListStatus[level] = -1;
                    }

                    if (DaysOnRecentlyVisitedList.ContainsKey(level))
                    {
                        if (DaysOnRecentlyVisitedList[level] >= Selenes_Choice.Config.DaysToRemember) // removes the level from the RVMs when it has been there long enough
                        {
                            levelsToRemove.Add(level);
                            ListStatus[level] = 2;
                        }
                    }
                }
                else
                {
                    ListStatus[level] = 3;
                }
            }
            foreach (ExtendedLevel level in levelsToRemove) // shred that shit
            {
                RecentlyVisitedMoons.Remove(level);
                DaysOnRecentlyVisitedList.Remove(level);
                RVMString.Remove(level.NumberlessPlanetName);
                DRVString.Remove(level.NumberlessPlanetName);
            }
            levelsToRemove.Clear();

            var sortedRVMs = RecentlyVisitedMoons.OrderBy(level => DaysOnRecentlyVisitedList[level]).ThenBy(level => level.NumberlessPlanetName).ToList();
            foreach (ExtendedLevel level in sortedRVMs) // literally all logging, sorry I have ocd so it must be sorted by how long its been on the list then alphabetical and I HAD to make sure it uses the right tenses
            {
                if (ListStatus[level] == 0)
                {
                    if (DaysOnRecentlyVisitedList[level] != 1)
                    {
                        MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} has now been on the RVL for {DaysOnRecentlyVisitedList[level]} days.";
                    }
                    else
                    {
                        MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} has now been on the RVL for {DaysOnRecentlyVisitedList[level]} day.";
                    }
                }
                if (ListStatus[level] == 1)
                {
                    MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} is new to the RVL, starting it at {DaysOnRecentlyVisitedList[level]} days.";
                }
                if (ListStatus[level] == 2)
                {
                    if (Selenes_Choice.Config.DaysToRemember.Value != 1)
                    {
                        MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} is no longer on the RVL list. It has been in purgatory for {Selenes_Choice.Config.DaysToRemember.Value} days.";
                    }
                    else
                    {
                        MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} is no longer on the RVL list. It has been in purgatory for {Selenes_Choice.Config.DaysToRemember.Value} day.";
                    }
                }
                if (ListStatus[level] == 3)
                {
                    if (DaysOnRecentlyVisitedList[level] != 1)
                    {
                        MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} has been reloaded, keeping its previous day count of {DaysOnRecentlyVisitedList[level]} days.";
                    }
                    else
                    {
                        MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} has been reloaded, keeping its previous day count of {DaysOnRecentlyVisitedList[level]} day.";
                    }
                }
                if (ListStatus[level] == 4)
                {
                    if (DaysOnRecentlyVisitedList[level] != 1)
                    {
                        MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} remains at {DaysOnRecentlyVisitedList[level]} days, the current level is Gordion.";
                    }
                    else
                    {
                        MoonStatusUpdate += $"\nMoon: {level.NumberlessPlanetName} remains at {DaysOnRecentlyVisitedList[level]} day, the current level is Gordion.";
                    }
                }
                ListStatus[level] = -1;
            }
            if (MoonStatusUpdate != "RVL Status:")
            {
                Selenes_Choice.instance.mls.LogInfo(MoonStatusUpdate); // finally logs the fat string
                MoonStatusUpdate = "RVL Status:";
            }
            foreach (ExtendedLevel level in levelsToRemove) // SHRED IT
            {
                RecentlyVisitedMoons.Remove(level);
                DaysOnRecentlyVisitedList.Remove(level);
                RVMString.Remove(level.NumberlessPlanetName);
                DRVString.Remove(level.NumberlessPlanetName);
            }
            levelsToRemove.Clear();

            List<ExtendedLevel> Levels = PatchedContent.ExtendedLevels.Where(level => !ListProcessor.Instance.ExclusionList.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();
            List<ExtendedLevel> allLevels = Levels.Where(level => !RecentlyVisitedMoons.Contains(level)).ToList(); // every level available to the shuffle

            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList(); // free levels that can be shuffled
            if (freeLevels.Count <= 0) // this loop repopulates the free levels when there are none left to be the 'safety moon' (they were all locked by RVM)
            {
                string AddedLevels = "No Free Moons were found, reusing RV free moon(s): ";
                foreach (ExtendedLevel level in RecentlyVisitedMoons)
                {
                    if (level.RoutePrice == 0)
                    {
                        freeLevels.Add(level);
                        allLevels.Add(level);
                        levelsToRemove.Add(level);
                        AddedLevels += level.NumberlessPlanetName + ", ";
                    }
                }
                if (AddedLevels.Contains(",") && AddedLevels.LastIndexOf(", ") == (AddedLevels.Length - 2))
                {
                    AddedLevels = AddedLevels.Remove(AddedLevels.LastIndexOf(", "), 2);
                    AddedLevels += ".";
                }
                Selenes_Choice.instance.mls.LogInfo(AddedLevels);
                foreach (ExtendedLevel level in levelsToRemove) // SHRED THAT SHIT BBY GRRL
                {
                    RecentlyVisitedMoons.Remove(level);
                    DaysOnRecentlyVisitedList.Remove(level);
                    RVMString.Remove(level.NumberlessPlanetName);
                    DRVString.Remove(level.NumberlessPlanetName);
                }
                levelsToRemove.Clear();
            }

            foreach (ExtendedLevel level in RecentlyVisitedMoons)
            {
                if (!DRVString.ContainsKey(level.NumberlessPlanetName))
                {
                    RVMString.Add(level.NumberlessPlanetName);
                    DRVString.Add(level.NumberlessPlanetName, DaysOnRecentlyVisitedList[level]); // update the string versions of the list and dict
                }
            }

            if (NetworkManager.Singleton.IsHost)
            {
                ES3.Save("RecentlyVisitedMoonsList", RVMString, GameNetworkManager.Instance.currentSaveFileName);
                ES3.Save("DaysSinceLastVisit", DRVString, GameNetworkManager.Instance.currentSaveFileName); // commit those to the savedata
            }

            Selenes_Choice.instance.mls.LogInfo("allLevels " + allLevels.Count);
            Selenes_Choice.instance.mls.LogInfo("freeLevels " + freeLevels.Count);

            List<ExtendedLevel> paidLevels = allLevels.Where(level => level.RoutePrice != 0).ToList(); // levels that aren't free
            Selenes_Choice.instance.mls.LogInfo("paid levels " + paidLevels.Count);

            int oldFreeMoonCount = freeMoonCount;
            int oldPaidMoonCount = paidMoonCount;
            int oldRandomMoonCount = randomMoonCount;

            if (paidLevels.Count == 0 && Selenes_Choice.Config.PaidMoonRollover) // this and the else reset the randomMoonCount when it gets reduced (due to moons being removed by RVMs)
            {
                randomMoonCount = Selenes_Choice.Config.RandomMoonCount + Selenes_Choice.Config.PaidMoonCount;
            }
            else
            {
                randomMoonCount = Selenes_Choice.Config.RandomMoonCount;
            }

            if (freeMoonCount < 1) // there must be at least one(1) free moon in the modpack or help me god
            {
                freeMoonCount = 1;
            }
            if (freeMoonCount > freeLevels.Count)
            {
                freeMoonCount = freeLevels.Count;
            }
            if (paidMoonCount < 0)
            {
                paidMoonCount = 0;
            }
            if (paidMoonCount > paidLevels.Count)
            {
                paidMoonCount = paidLevels.Count;
            }
            if (randomMoonCount < 0)
            {
                randomMoonCount = 0;
            }
            if (randomMoonCount > (allLevels.Count - freeMoonCount) - paidMoonCount)
            {
                randomMoonCount = (allLevels.Count - freeMoonCount) - paidMoonCount;
            }

            if (oldFreeMoonCount != freeMoonCount)
            {
                Selenes_Choice.instance.mls.LogInfo("freeMoonCount changed from " + oldFreeMoonCount + " to " + freeMoonCount);
            }
            if (oldPaidMoonCount != paidMoonCount)
            {
                Selenes_Choice.instance.mls.LogInfo("paidMoonCount changed from " + oldPaidMoonCount + " to " + paidMoonCount);
            }
            if (oldRandomMoonCount != randomMoonCount)
            {
                Selenes_Choice.instance.mls.LogInfo("randomMoonCount changed from " + oldRandomMoonCount + " to " + randomMoonCount);
            }

            int oldminDiscount = minDiscount;
            int oldmaxDiscount = maxDiscount;

            minDiscount = Mathf.Clamp(minDiscount, 0, 100);
            maxDiscount = Mathf.Clamp(maxDiscount, minDiscount, 100); // min<max

            if (oldminDiscount != minDiscount)
            {
                Selenes_Choice.instance.mls.LogInfo("minDiscount changed from " + oldminDiscount + " to " + minDiscount);
            }
            if (oldminDiscount != maxDiscount)
            {
                Selenes_Choice.instance.mls.LogInfo("maxDiscount changed from " + oldmaxDiscount + " to " + maxDiscount);
            }

            IncreaseDays = true;
        }
        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static class ResetSaveStatusOnDC
        {
            static void Postfix() // reset data for RVM feature on leaving (some are repeats but why not empty it anyway just in case)
            {
                LoadedSaveData = false;
                IncreaseDays = false;
                RecentlyVisitedMoons.Clear();
                DaysOnRecentlyVisitedList.Clear();
                RVMString.Clear();
                DRVString.Clear();
                levelsToRemove.Clear();
                ListStatus.Clear();
            }
        }
    }
    public class SyncRVM
    {
        private static readonly SyncRVM _instance = new SyncRVM();
        private SyncRVM() { }

        public static SyncRVM Instance
        {
            get
            {
                return _instance;
            }
        }
        [ClientRpc]
        public void SendSavedDataClientRpc(List<string> rvmString, Dictionary<string, int> drvString)
        {
            if (!NetworkManager.Singleton.IsHost && UpdateConfig.RVMString.Count == 0 && UpdateConfig.DRVString.Count == 0)
            {
                UpdateConfig.RVMString = rvmString;
                UpdateConfig.DRVString = drvString;
                Selenes_Choice.instance.mls.LogInfo("Received RVM data from host.");
            }
            else
            {
                Selenes_Choice.instance.mls.LogInfo("Data is up to date.");
            }
        }
        public void SyncDataWithClient()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                List<string> rvmString = ES3.Load<List<string>>("RecentlyVisitedMoonsList", GameNetworkManager.Instance.currentSaveFileName);
                Dictionary<string, int> drvString = ES3.Load<Dictionary<string, int>>("DaysSinceLastVisit", GameNetworkManager.Instance.currentSaveFileName);
                SendSavedDataClientRpc(rvmString, drvString);
            }
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
    public static class SyncRVM2
    {
        public static void Postfix()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Selenes_Choice.instance.mls.LogInfo("Sending RVM data to new client.");
                SyncRVM.Instance.SyncDataWithClient();
                Selenes_Choice.instance.mls.LogInfo("RVM data sent to new client.");
            }
        }
    }
    public class ListProcessor
    {
        private static readonly ListProcessor _instance = new ListProcessor();

        public string ExclusionList { get; private set; }
        private ListProcessor() { }

        public static ListProcessor Instance
        {
            get
            {
                return _instance;
            }
        }
        public void ProcessLists()
        {
            string ignoreList = Selenes_Choice.Config.IgnoreMoons;
            string blacklist = Selenes_Choice.Config.BlacklistMoons;
            string treasurelist = Selenes_Choice.Config.TreasureMoons;
            ExclusionList = string.Join(",", ignoreList, blacklist, treasurelist);

            if (Selenes_Choice.Config.StoryMoonCompat.Value)
            {
                string storylist = "Penumbra,Sector-0";
                ExclusionList = string.Join(",", ExclusionList, storylist);
            }

            string IgnoreList = Selenes_Choice.Config.IgnoreMoons;

            List<ExtendedLevel> IgnoreThese = PatchedContent.ExtendedLevels.Where(level => IgnoreList.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();

            foreach (ExtendedLevel level in IgnoreThese)
            {
                if (level.NumberlessPlanetName.Equals("Gordion"))
                {
                    level.IsRouteLocked = false;
                    level.IsRouteHidden = true;
                }
                else
                {
                    level.IsRouteLocked = false;
                    level.IsRouteHidden = false;
                }
            }
            string BlackList = Selenes_Choice.Config.BlacklistMoons;

            List<ExtendedLevel> BlacklistThese = PatchedContent.ExtendedLevels.Where(level => BlackList.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();

            foreach (ExtendedLevel level in BlacklistThese)
            {
                level.IsRouteLocked = true;
                level.IsRouteHidden = true;
            }
            string TreasureList = Selenes_Choice.Config.TreasureMoons;

            List<ExtendedLevel> TreasureThese = PatchedContent.ExtendedLevels.Where(level => TreasureList.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();

            foreach (ExtendedLevel level in TreasureThese)
            {
                level.IsRouteLocked = false;
                level.IsRouteHidden = true;
                if (Selenes_Choice.Config.TreasureBool)
                {
                    level.SelectableLevel.minScrap = (int)(level.SelectableLevel.minScrap * Selenes_Choice.Config.TreasureBonus);
                    level.SelectableLevel.maxScrap = (int)(level.SelectableLevel.maxScrap * Selenes_Choice.Config.TreasureBonus);
                }
            }
        }
    }
    public static class GlobalVariables
    {
        public static int RemainingScrapInLevel;
    }
    [HarmonyPatch(typeof(StartOfRound), "ShipLeave")]
    public class ShipleaveCalc
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            GlobalVariables.RemainingScrapInLevel = CalculateRemainingScrapInLevel();
        }
        public static int CalculateRemainingScrapInLevel()
        {
            GrabbableObject[] array = Object.FindObjectsOfType<GrabbableObject>();
            int remainingValue = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].itemProperties.isScrap && !array[i].isInShipRoom && !array[i].isInElevator && !array[i].scrapPersistedThroughRounds)
                {
                    remainingValue += array[i].scrapValue;
                }
            }
            return remainingValue;
        }
    }
    [HarmonyPatch(typeof(HUDManager), "FillEndGameStats")]
    public class HUDManagerPatch
    {
        [HarmonyPostfix]
        public static void FillEndGameStatsPostfix(HUDManager __instance, int scrapCollected)
        {
            float finalCount = (int)(scrapCollected + GlobalVariables.RemainingScrapInLevel);
            __instance.statsUIElements.quotaDenominator.text = finalCount.ToString();
        }
    }
    public static class PriceManager
    {
        public static Dictionary<ExtendedLevel, int> originalPrices = new Dictionary<ExtendedLevel, int>();
        public static void ResetPrices()
        {
            foreach (var pair in originalPrices)
            {
                pair.Key.RoutePrice = pair.Value;
            }
            originalPrices.Clear();
        }
    }
}