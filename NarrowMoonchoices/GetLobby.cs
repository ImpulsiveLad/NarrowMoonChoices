using HarmonyLib;
using Steamworks;
using Steamworks.Data;
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
}