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
        private const string modGUID = "impulse.Selenes_Choice";
        private const string modName = "SelenesChoice";
        private const string modVersion = "1.1.0";
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
            harmony.PatchAll(typeof(ListProcessor));
            harmony.PatchAll(typeof(UpdateConfig));
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
        [DataMember] public SyncedEntry<int> FreeMoonCount { get; private set; }
        [DataMember] public SyncedEntry<int> RandomMoonCount { get; private set; }
        [DataMember] public SyncedEntry<bool> DailyOrQuota { get; private set; }
        [DataMember] public SyncedEntry<string> IgnoreMoons { get; private set; }
        [DataMember] public SyncedEntry<string> BlacklistMoons { get; private set; }
        public SyncConfig(ConfigFile cfg) : base("Selenes_Choice")
        {
            ConfigManager.Register(this);

            FreeMoonCount = cfg.BindSyncedEntry("General",
                "Free Moon Count",
                1,
                "How many guaranteed free moons should be generated?");

            RandomMoonCount = cfg.BindSyncedEntry("General",
                "Extra Moon Count",
                2,
                "How many moons should be generated on top of the free moons? (These can be free or paid)");

            DailyOrQuota = cfg.BindSyncedEntry("General",
                "New Moons Only on New Quota",
                false,
                "If set to true, the moons will only reshuffle after a new quota is assigned instead of reshuffling daily.");

            IgnoreMoons = cfg.BindSyncedEntry("Lists",
                "Ignore Moons",
                "Gordion",
                "Any moons listed here will not be touched by this mod, they cannot be part of the random moon shuffle. Use this to have moons that are constant, they will always be unhidden and unlocked. Moon names must be spelled correctly and exact, IE Experimentation,Assurance,Vow - Experimentatio would not be counted. (This is to avoid moon name mix-ups)");

            BlacklistMoons = cfg.BindSyncedEntry("Lists",
                "Blacklist Moons",
                "Liquidation",
                "Any moons listed here will be indefinitely hidden and locked, any moons here will also be excluded from the shuffle. Moon names must be spelled correctly and exact, IE Experimentation,Assurance,Vow - Experimentatio would not be counted. (This is to avoid moon name mix-ups)");
        }
    }
}

