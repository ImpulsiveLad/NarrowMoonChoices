﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Extensions;
using CSync.Lib;
using HarmonyLib;
using LethalLevelLoader;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;
using static Selenes_Choice.UpdateConfig;

namespace Selenes_Choice
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.sigurd.csync", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mrov.WeatherRegistry", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("BULLETBOT.PermanentMoons", BepInDependency.DependencyFlags.SoftDependency)]
    public class Selenes_Choice : BaseUnityPlugin
    {
        private const string modGUID = "impulse.Selenes_Choice";
        private const string modName = "SelenesChoice";
        private const string modVersion = "2.4.3";
        private readonly Harmony harmony = new Harmony(modGUID);

        public ManualLogSource mls;

        public static Selenes_Choice instance;

        public static int LastUsedSeed;

        public static int glump;

        public static ExtendedLevel PreviousSafetyMoon;

        public static int SafetyType = -1;

        public new static SyncConfig Config;

        void Awake()
        {
            instance = this;

            Config = new SyncConfig(base.Config);

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            harmony.PatchAll(typeof(ShareSnT));
            harmony.PatchAll(typeof(AnchorTheShare));
            harmony.PatchAll(typeof(ListProcessor));
            harmony.PatchAll(typeof(UpdateConfig));
            harmony.PatchAll(typeof(HideMoonsOnStart));
            harmony.PatchAll(typeof(HideMoonsOnGameOver));
            harmony.PatchAll(typeof(GlobalVariables));
            harmony.PatchAll(typeof(ShipleaveCalc));
            harmony.PatchAll(typeof(HUDManagerPatch));
            harmony.PatchAll(typeof(ResetSaveStatusOnDC));
            harmony.PatchAll(typeof(HideMoonsOnDayChange));
            harmony.PatchAll(typeof(HideMoonsOnNewQuota));
            harmony.PatchAll(typeof(AutoRouteToCompany));
            harmony.PatchAll(typeof(KeepWeather));
            harmony.PatchAll(typeof(SaveAfterRouting));
            harmony.PatchAll(typeof(MarkAsSaved));

            if (WeatherRegistryCompatibility.enabled)
            {
                WeatherRegistryCompatibility.ChangeWeatherClearer();
            }
            else
            {
                instance.mls.LogInfo($"Weather Registry not detected, using default weather clearer...");
            }

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
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
        [DataMember] public SyncedEntry<string> TreasureMoons { get; private set; }
        [DataMember] public SyncedEntry<bool> TreasureBool { get; private set; }
        [DataMember] public SyncedEntry<float> TreasureBonus { get; private set; }
        [DataMember] public SyncedEntry<int> PaidMoonCount { get; private set; }
        [DataMember] public SyncedEntry<bool> RollOverMoons { get; private set; }
        [DataMember] public SyncedEntry<bool> StoryMoonCompat { get; private set; }
        [DataMember] public SyncedEntry<bool> ClearWeather { get; private set; }
        [DataMember] public SyncedEntry<bool> DiscountMoons { get; private set; }
        [DataMember] public SyncedEntry<int> MinDiscount { get; private set; }
        [DataMember] public SyncedEntry<int> MaxDiscount { get; private set; }
        [DataMember] public SyncedEntry<bool> RememberMoons { get; private set; }
        [DataMember] public SyncedEntry<bool> RememberAll { get; private set; }
        [DataMember] public SyncedEntry<int> DaysToRemember { get; private set; }
        [DataMember] public SyncedEntry<bool> ReturnMany { get; private set; }
        [DataMember] public SyncedEntry<bool> ReturnFrees { get; private set; }
        [DataMember] public SyncedEntry<int> ValueThreshold { get; private set; }
        [DataMember] public SyncedEntry<int> RareMoonCount { get; private set; }
        public SyncConfig(ConfigFile cfg) : base("Selenes_Choice")
        {
            ConfigManager.Register(this);

            FreeMoonCount = cfg.BindSyncedEntry("_General_",
                "Free Moon Count",
                1,
                "How many guaranteed free moons should be included?");

            PaidMoonCount = cfg.BindSyncedEntry("_General_",
                "Paid Moon Count",
                1,
                "How many guaranteed paid moons should be included?");

            RandomMoonCount = cfg.BindSyncedEntry("_General_",
                "Extra Moon Count",
                1,
                "How many additional moons should be included? (These can be free or paid)");

            RareMoonCount = cfg.BindSyncedEntry("_General_",
                "Rare Moon Count",
                0,
                "How many paid moons above the threshold value below should be included?");

            ValueThreshold = cfg.BindSyncedEntry("_General_",
                "Rare Moon Threshold",
                650,
                "Moons equal or higher than this value will be used for the Rare Moon Count.");

            RollOverMoons = cfg.BindSyncedEntry("_General_",
                "Roll Over Moons",
                true,
                "If this is true and the current number of available free or paid moons do not exceed or reach the Free Moon Count/Paid Moon Count, they will be rolled over into the Random Moon Count.\nFor Example, if you have free/paid/random counts of 5/5/5 but there are only 3 paid moons then it will become 5/3/7 so there are still 15 moons generated.");

            DailyOrQuota = cfg.BindSyncedEntry("_General_",
                "New Moons Only on New Quota",
                false,
                "If set to true, the moons will reshuffle only after a new quota is assigned, not daily.");

            ClearWeather = cfg.BindSyncedEntry("_General_",
                "Clear Weather on the Safety Moon?",
                false,
                "If set to true, the first free moon selected and the one that will be auto-routed to will always have clear weather.");

            RememberMoons = cfg.BindSyncedEntry("_Remember Moons_",
                "Remember Moons?",
                true,
                "If set to true, the 'safety moon' is removed from the shuffle for x days (see settings below).");

            RememberAll = cfg.BindSyncedEntry("_Remember Moons_",
                "Remember All?",
                false,
                "If set to true, all moons that were included in a last shuffle will be removed for x days in addition to the 'safety moon' (setting above must also be true).");

            DaysToRemember = cfg.BindSyncedEntry("_Remember Moons_",
                "Days to Remember",
                3,
                "The number of days that remembered moons will be excluded from the shuffle for.");

            ReturnMany = cfg.BindSyncedEntry("_Remember Moons_",
                "Return Many Moons?",
                false,
                "If set to true, instead of 'forgetting' just one random moon when they all become locked by the Remembrance, it will 'forget' all currenly remembered moons (clear the RVM List).");

            ReturnFrees = cfg.BindSyncedEntry("_Remember Moons_",
                "Only Return Free Moons?",
                false,
                "If set to true, instead of waiting for all moons to be 'remembered' before 'forgetting' them, it will 'forget' a random free moon once there are no free moons remaining (or all free moons if this and the above setting are true).");

            IgnoreMoons = cfg.BindSyncedEntry("Lists",
                "Ignore Moons",
                "Gordion",
                "Any moons listed here will not be touched by this mod, they cannot be part of the random moon shuffle. Use this to have moons that are constant, they will always be unhidden and unlocked. Moon names must be spelled exactly and correctly. For example, ‘Experimentation,Assurance,Vow’ would be counted, but ‘Experimentatio’ would not. (This is to avoid moon name mix-ups)");

            BlacklistMoons = cfg.BindSyncedEntry("Lists",
                "Blacklist Moons",
                "Liquidation",
                "Any moons listed here will be indefinitely hidden and locked, any moons here will also be excluded from the shuffle. Moon names must be spelled exactly and correctly. For example, ‘Experimentation,Assurance,Vow’ would be counted, but ‘Experimentatio’ would not. (This is to avoid moon name mix-ups).");

            TreasureMoons = cfg.BindSyncedEntry("Lists",
                "Treasure(?) Moons",
                "Embrion,StarlancerZero,Cosmocos",
                "Any moons listed here will remain hidden but still be routable (if you know the routing key *winky face*) Just as the other two lists, these are not in the shuffle. The config section below allows you to make them be 'Treasure Moons.' Moon names must be spelled exactly and correctly. For example, ‘Experimentation,Assurance,Vow’ would be counted, but ‘Experimentatio’ would not. (This is to avoid moon name mix-ups)");

            TreasureBool = cfg.BindSyncedEntry("Treasure",
                "Bonus For Secret Moons?",
                false,
                "If set to true, moons from the Treasure Moons list will have a bonus value applied.");

            TreasureBonus = cfg.BindSyncedEntry("Treasure",
                "Treasure Bonus",
                1.25f,
                "This multiplier is applied to the scrap value and count on treasure moons if the setting above is true.");

            StoryMoonCompat = cfg.BindSyncedEntry("Compat",
                "Story Log Unlock Compat",
                true,
                "If set to true, certain moons will be excluded from the shuffle and untouched by this mod. Currently, this only includes two moons from Rosie's Moons.");

            DiscountMoons = cfg.BindSyncedEntry("Discounts",
                "Enable Moon Discounts?",
                false,
                "If set to true, paid moons selected by the shuffle with have a discount based on the next two settings.");

            MinDiscount = cfg.BindSyncedEntry("Discounts",
                "Min Discount",
                40,
                "Minimum percent for a moon to have its price reduced by. Must be less than the max.");

            MaxDiscount = cfg.BindSyncedEntry("Discounts",
                "Max Discount",
                60,
                "Maximum percent for a moon to have its price reduced by. Must be more than the min.");
        }
    }
    public static class WeatherRegistryCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("mrov.WeatherRegistry");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void ChangeWeatherClearer()
        {
            Selenes_Choice.instance.mls.LogInfo($"Detected Weather Registry, changing weather clearer...");
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void ClearWeatherWithWR(ExtendedLevel level)
        {
            WeatherRegistry.WeatherController.ChangeWeather(level.SelectableLevel, LevelWeatherType.None);
        }
    }
    public static class PermanentMoonsCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BULLETBOT.PermanentMoons");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static Dictionary<string, object> GrabPMList()
        {
            return Plugin.GetSave();
        }
    }
}