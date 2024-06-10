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
        public static int maxDiscount = Selenes_Choice.Config.MaxDiscount;
        private UpdateConfig() { }
        public static UpdateConfig Instance
        {
            get
            {
                return _instance;
            }
        }
        public void BracketMoons() // Call this every time you need to use the config values
        {
            List<ExtendedLevel> allLevels = PatchedContent.ExtendedLevels.Where(level => !ListProcessor.Instance.ExclusionList.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();
            Selenes_Choice.instance.mls.LogInfo("allLevels " + allLevels.Count);
            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList();
            Selenes_Choice.instance.mls.LogInfo("freeLevels " + freeLevels.Count);
            List<ExtendedLevel> paidLevels = allLevels.Where(level => level.RoutePrice != 0).ToList();
            Selenes_Choice.instance.mls.LogInfo("paid levels " + paidLevels.Count);

            if (paidLevels.Count == 0 && Selenes_Choice.Config.PaidMoonRollover)
            {
                randomMoonCount = Selenes_Choice.Config.RandomMoonCount + Selenes_Choice.Config.PaidMoonCount;
            }

            int oldFreeMoonCount = freeMoonCount;
            int oldPaidMoonCount = paidMoonCount;
            int oldRandomMoonCount = randomMoonCount;

            if (freeMoonCount < 1)
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
            maxDiscount = Mathf.Clamp(maxDiscount, minDiscount, 100);

            if (oldminDiscount != minDiscount)
            {
                Selenes_Choice.instance.mls.LogInfo("minDiscount changed from " + oldminDiscount + " to " + minDiscount);
            }
            if (oldminDiscount != maxDiscount)
            {
                Selenes_Choice.instance.mls.LogInfo("maxDiscount changed from " + oldmaxDiscount + " to " + maxDiscount);
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