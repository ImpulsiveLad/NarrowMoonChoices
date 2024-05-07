using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

namespace NarrowMoonChoices
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
                NarrowMoonChoices.instance.mls.LogInfo("LobbyID: " + GrabbedLobby);
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
    public class ShareSnT : NetworkBehaviour
    {
        public static ShareSnT Instance { get; private set; }

        public int lastUsedSeedPrev;
        public int timesCalledPrev;

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
                lastUsedSeedPrev = NarrowMoonChoices.LastUsedSeed;
                timesCalledPrev = ResetShipPatch.TimesCalled;

                UpdateAndSendData();
                NarrowMoonChoices.instance.mls.LogInfo("Host received message");
            }
            else if (!NetworkManager.Singleton.IsHost)
            {
                reader.ReadValue(out int lastUsedSeedPrev);
                reader.ReadValue(out int timesCalledPrev);
                NarrowMoonChoices.LastUsedSeed = lastUsedSeedPrev;
                ResetShipPatch.TimesCalled = timesCalledPrev;
                NarrowMoonChoices.instance.mls.LogInfo("Client received message");
            }
        }
        private void Update()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                if (NarrowMoonChoices.LastUsedSeed != lastUsedSeedPrev || ResetShipPatch.TimesCalled != timesCalledPrev)
                {
                    UpdateAndSendData();

                    lastUsedSeedPrev = NarrowMoonChoices.LastUsedSeed;
                    timesCalledPrev = ResetShipPatch.TimesCalled;
                    NarrowMoonChoices.instance.mls.LogInfo("Host has saved new seed, " + lastUsedSeedPrev);
                    NarrowMoonChoices.instance.mls.LogInfo("Host has saved new ejection, " + timesCalledPrev);
                }
            }
        }
    }
    [HarmonyPatch(typeof(HangarShipDoor), "Start")]
    public static class AnchorTheShare
    {
        static void Postfix(HangarShipDoor __instance)
        {
            if (__instance.gameObject.GetComponent<ShareSnT>() == null)
            {
                __instance.gameObject.AddComponent<ShareSnT>();
            }
        }
    }
}