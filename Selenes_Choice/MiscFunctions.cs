using HarmonyLib;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using LethalLevelLoader;
using System.Collections.Generic;
using System.Threading;

namespace Selenes_Choice
{
    public class ShareSnT : NetworkBehaviour
    {
        public static ShareSnT Instance { get; private set; }

        public int lastUsedSeedPrev;

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
            using (var writer = new FastBufferWriter(1024, Allocator.Temp))
            {
                SerializableList serializableRVMString = new SerializableList { RVMString = UpdateConfig.RVMString };
                SerializableDictionary serializableDRVString = new SerializableDictionary { DRVString = UpdateConfig.DRVString };

                writer.WriteValueSafe(lastUsedSeedPrev);
                writer.WriteValueSafe(serializableRVMString);
                writer.WriteValueSafe(serializableDRVString);

                NetworkManager.CustomMessagingManager.SendNamedMessage(MessageName, NetworkManager.ConnectedClientsIds, writer);
            }
        }
        private void OnMessageReceived(ulong clientId, FastBufferReader reader)
        {
            if (NetworkManager.Singleton.IsHost && clientId != NetworkManager.ServerClientId)
            {
                lastUsedSeedPrev = Selenes_Choice.LastUsedSeed;
                dataReceivedEvent.Set();

                UpdateAndSendData();
                Selenes_Choice.instance.mls.LogInfo("Host received message");
            }
            else if (!NetworkManager.Singleton.IsHost)
            {
                reader.ReadValue(out int lastUsedSeedPrev);
                Selenes_Choice.LastUsedSeed = lastUsedSeedPrev;

                SerializableList serializableRVMString;
                SerializableDictionary serializableDRVString;

                reader.ReadValueSafe(out serializableRVMString);
                reader.ReadValueSafe(out serializableDRVString);

                UpdateConfig.RVMString = serializableRVMString.RVMString;
                UpdateConfig.DRVString = serializableDRVString.DRVString;
                dataReceivedEvent.Set();
                Selenes_Choice.instance.mls.LogInfo("Client received message");
            }
        }
        private void Update()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                if (Selenes_Choice.LastUsedSeed != lastUsedSeedPrev)
                {
                    UpdateAndSendData();

                    lastUsedSeedPrev = Selenes_Choice.LastUsedSeed;
                    Selenes_Choice.instance.mls.LogInfo("Host has saved new seed, " + lastUsedSeedPrev);
                }
            }
        }
        [System.Serializable]
        public class SerializableList : INetworkSerializable
        {
            public List<string> RVMString = new List<string>();

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                int count = RVMString.Count;
                serializer.SerializeValue(ref count);

                if (serializer.IsReader)
                {
                    RVMString = new List<string>(count);
                }

                for (int i = 0; i < count; i++)
                {
                    string value = i < RVMString.Count ? RVMString[i] : string.Empty;
                    serializer.SerializeValue(ref value);
                    if (serializer.IsReader)
                    {
                        if (i < RVMString.Count)
                        {
                            RVMString[i] = value;
                        }
                        else
                        {
                            RVMString.Add(value);
                        }
                    }
                }
            }
        }
        [System.Serializable]
        public class SerializableDictionary : INetworkSerializable
        {
            public Dictionary<string, int> DRVString = new Dictionary<string, int>();

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                int count = DRVString.Count;
                serializer.SerializeValue(ref count);

                if (serializer.IsReader)
                {
                    DRVString = new Dictionary<string, int>(count);
                }

                List<string> keys = new List<string>(DRVString.Keys);
                List<int> values = new List<int>(DRVString.Values);

                for (int i = 0; i < count; i++)
                {
                    string key = i < keys.Count ? keys[i] : string.Empty;
                    int value = i < values.Count ? values[i] : 0;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref value);

                    if (serializer.IsReader)
                    {
                        DRVString[key] = value;
                    }
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
        public static int rareMoonCount = Selenes_Choice.Config.RareMoonCount;

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
            List<ExtendedLevel> FreeRVMs = RecentlyVisitedMoons.Where(level => level.RoutePrice == 0).ToList();

            if (Selenes_Choice.Config.ReturnFrees && FreeRVMs.Count != 0)
            {
                if (Selenes_Choice.Config.ReturnMany)
                {
                    if (freeLevels.Count <= 0) // this loop repopulates the free levels when there are none left to be the 'safety moon' (they were all locked by RVM)
                    {
                        string AddedLevels = "No Free Moons were found, reusing RV free moon(s): ";

                        foreach (ExtendedLevel level in FreeRVMs)
                        {
                            freeLevels.Add(level);
                            allLevels.Add(level);
                            levelsToRemove.Add(level);
                            AddedLevels += level.NumberlessPlanetName + ", ";
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
                }
                else
                {
                    if (freeLevels.Count <= 0)
                    {
                        string AddedLevel = "No Free Moons were found, reusing one RV free moon: ";
                        List<ExtendedLevel> eligibleLevels = new List<ExtendedLevel>();

                        foreach (ExtendedLevel level in RecentlyVisitedMoons)
                        {
                            if (level.RoutePrice == 0)
                            {
                                eligibleLevels.Add(level);
                            }
                        }
                        System.Random rand = new System.Random(StartOfRound.Instance.randomMapSeed);
                        ExtendedLevel selectedLevel = eligibleLevels[rand.Next(eligibleLevels.Count)];

                        freeLevels.Add(selectedLevel);
                        allLevels.Add(selectedLevel);
                        AddedLevel += selectedLevel.NumberlessPlanetName + ".";

                        Selenes_Choice.instance.mls.LogInfo(AddedLevel);

                        RecentlyVisitedMoons.Remove(selectedLevel);
                        DaysOnRecentlyVisitedList.Remove(selectedLevel);
                        RVMString.Remove(selectedLevel.NumberlessPlanetName);
                        DRVString.Remove(selectedLevel.NumberlessPlanetName);
                    }
                }
            }
            else
            {
                if (Selenes_Choice.Config.ReturnMany)
                {
                    if (allLevels.Count <= 0)
                    {
                        string AddedLevels = "No Moons were found, reusing RV moons: ";

                        foreach (ExtendedLevel level in RecentlyVisitedMoons)
                        {
                            allLevels.Add(level);
                            levelsToRemove.Add(level);
                            AddedLevels += level.NumberlessPlanetName + ", ";
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
                }
                else
                {
                    if (allLevels.Count <= 0)
                    {
                        string AddedLevel = "No Moons were found, reusing one RV moon: ";
                        List<ExtendedLevel> eligibleLevels = new List<ExtendedLevel>();

                        foreach (ExtendedLevel level in RecentlyVisitedMoons)
                        {
                            eligibleLevels.Add(level);
                        }
                        System.Random rand = new System.Random(StartOfRound.Instance.randomMapSeed);
                        ExtendedLevel selectedLevel = eligibleLevels[rand.Next(eligibleLevels.Count)];

                        allLevels.Add(selectedLevel);
                        AddedLevel += selectedLevel.NumberlessPlanetName + ".";

                        Selenes_Choice.instance.mls.LogInfo(AddedLevel);

                        RecentlyVisitedMoons.Remove(selectedLevel);
                        DaysOnRecentlyVisitedList.Remove(selectedLevel);
                        RVMString.Remove(selectedLevel.NumberlessPlanetName);
                        DRVString.Remove(selectedLevel.NumberlessPlanetName);
                    }
                }
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
            List<ExtendedLevel> rareLevels = paidLevels.Where(level => level.RoutePrice >= Selenes_Choice.Config.ValueThreshold).ToList(); // Threshold value determines rare moons
            Selenes_Choice.instance.mls.LogInfo("rare levels " + rareLevels.Count);

            int oldFreeMoonCount = freeMoonCount;
            int oldPaidMoonCount = paidMoonCount;
            int oldRandomMoonCount = randomMoonCount;
            int oldRareMoonCount = rareMoonCount;

            freeMoonCount = Selenes_Choice.Config.FreeMoonCount;
            if (freeMoonCount < 0)
            {
                freeMoonCount = 0;
            }
            rareMoonCount = Selenes_Choice.Config.RareMoonCount;
            if (rareMoonCount < 0)
            {
                rareMoonCount = 0;
            }
            paidMoonCount = Selenes_Choice.Config.PaidMoonCount;
            if (paidMoonCount < 0)
            {
                paidMoonCount = 0;
            }
            randomMoonCount = Selenes_Choice.Config.RandomMoonCount;
            if (randomMoonCount < 0)
            {
                randomMoonCount = 0;
            }

            int MoonsToSpawn = freeMoonCount + paidMoonCount + randomMoonCount + rareMoonCount; // MoonsToSpawn is the total "called for" moons after removing negatives
            if (MoonsToSpawn == 0)
            {
                freeMoonCount = 1;
            }
            int RandomMax = allLevels.Count - MoonsToSpawn; // RandomMax is the max count of Random as 
            if (RandomMax < 0)
            {
                MoonsToSpawn = allLevels.Count; // loop keeps RandomMax out of the negatives and caps out the MoonsToSpawn by how many levels are in the shuffle
                RandomMax = 0;
            }

            if (freeMoonCount > freeLevels.Count) // only free moons can be free
            {
                freeMoonCount = freeLevels.Count;
            }
            MoonsToSpawn -= freeMoonCount; // account for free moons
            if (rareMoonCount > rareLevels.Count) // only moons above the threshold can be rare
            {
                rareMoonCount = rareLevels.Count;
            }
            MoonsToSpawn -= rareMoonCount; // account for rare moons
            if (paidMoonCount > paidLevels.Count - rareMoonCount) // account for paid moons that are already being picked as rare moons
            {
                paidMoonCount = paidLevels.Count - rareMoonCount;
            }
            MoonsToSpawn -= paidMoonCount; // account for paid moons
            if (randomMoonCount > RandomMax) // uses random max here
            {
                randomMoonCount = RandomMax;
            }
            MoonsToSpawn -= randomMoonCount; // account for random moons
            if (MoonsToSpawn > 0 && Selenes_Choice.Config.RollOverMoons) // easy math to get the total "called for" moons to be rolled to random moons if its on and the MoonsToSpawn wasn't more than the # of moons
            {
                randomMoonCount += MoonsToSpawn; // at this point the MoonsToSpawn will have been decreased so that if none of the above moons hit their max it will be exactly 0 (AllLevels.Count(or less) - (freeMoonCount + paidMoonCount + randomMoonCount + rareMoonCount))
            }

            if (oldFreeMoonCount != freeMoonCount)
            {
                Selenes_Choice.instance.mls.LogInfo("freeMoonCount changed from " + oldFreeMoonCount + " to " + freeMoonCount);
            }
            if (oldPaidMoonCount != paidMoonCount)
            {
                Selenes_Choice.instance.mls.LogInfo("paidMoonCount changed from " + oldPaidMoonCount + " to " + paidMoonCount);
            }
            if (oldRareMoonCount != rareMoonCount)
            {
                Selenes_Choice.instance.mls.LogInfo("rareMoonCount changed from " + oldRareMoonCount + " to " + rareMoonCount);
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
            if (oldmaxDiscount != maxDiscount)
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