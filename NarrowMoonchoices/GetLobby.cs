using HarmonyLib;

[HarmonyPatch(typeof(LobbySlot), "JoinButton")]
public static class GetLobby
{
    public static int GrabbedLobby;

    static void Postfix(LobbySlot __instance)
    {
        GrabbedLobby = __instance.lobbyId.Value.GetHashCode();
    }
}
