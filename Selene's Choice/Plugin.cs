using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using CSync.Extensions;
using CSync.Lib;
using System.Runtime.Serialization;

namespace Selenes_Choice
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Selenes_Choice : BaseUnityPlugin
    {
        private const string modGUID = "impulse.Selene's Choice";
        private const string modName = "Selene's Choice";
        private const string modVersion = "1.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);

        public ManualLogSource mls;

        public static Selenes_Choice instance;

        public static int LastUsedSeed;

        public new static SyncConfig Config;
        void Awake()
        {
            instance = this;

            Config = new SyncConfig(base.Config);

            harmony.PatchAll(typeof(GetLobby));
            harmony.PatchAll(typeof(ResetShipPatch));
            harmony.PatchAll(typeof(ShareSnT));
            harmony.PatchAll(typeof(AnchorTheShare));
            harmony.PatchAll(typeof(HideMoonsOnStart));
            harmony.PatchAll(typeof(HideMoonsOnGameOver));

            if (Config.DailyOrQuota == false)
            {
                harmony.PatchAll(typeof(HideMoonsOnDayChange));
            }
            if (Config.DailyOrQuota == true)
            {
                harmony.PatchAll(typeof(HideMoonsOnNewQuota));
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }
    }
    [DataContract]
    public class SyncConfig : SyncedConfig2<SyncConfig>
    {
        [DataMember] public SyncedEntry<int> RandomMoonCount { get; private set; }
        [DataMember] public SyncedEntry<bool> DailyOrQuota { get; private set; }
        [DataMember] public SyncedEntry<string> IgnoreMoons { get; private set; }
        public SyncConfig(ConfigFile cfg) : base("Arachnophilia")
        {
            ConfigManager.Register(this);

            RandomMoonCount = cfg.BindSyncedEntry("General",
                "Random Moon Count",
                3,
                "How many random moons should be available at any given time?");

            DailyOrQuota = cfg.BindSyncedEntry("General",
                "New Moons Only on New Quota",
                false,
                "If set to true, the moons will only reshuffle after a new quota is assigned instead of reshuffling daily.");

            IgnoreMoons = cfg.BindSyncedEntry("General",
                "Ignore Moons",
                "The Company,Liquidation",
                "Any moons listed here will not be touched by this mod, they cannot be part of the random moon shuffle.");
        }
    }
}