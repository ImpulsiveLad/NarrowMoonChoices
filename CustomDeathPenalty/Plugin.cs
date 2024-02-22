using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace CustomDeathPenalty
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class CustomDeathPenaltyMain : BaseUnityPlugin
    {
        private const string modGUID = "impulse.CustomDeathPenalty";
        private const string modName = "CustomDeathPenalty";
        private const string modVersion = "1.4.0";
        private readonly Harmony harmony = new Harmony(modGUID);
        public ManualLogSource mls;
        public static CustomDeathPenaltyMain instance;
        public static ConfigEntry<float> FineAmount;
        public static ConfigEntry<float> InsuranceReduction;
        public static ConfigEntry<float> CompanyFineAmount;
        public static ConfigEntry<float> CompanyInsuranceReduction;
        public static ConfigEntry<int> QuotaIncreasePercent;
        public static ConfigEntry<bool> DynamicScrapBool;
        public static ConfigEntry<int> ScrapValueOffset;
        public static ConfigEntry<int> EnemyThreshhold;
        public static ConfigEntry<float> MinDiff;
        public static ConfigEntry<float> MaxDiff;
        public static float CurrentFineAmount { get; set; }
        public static float CurrentInsuranceReduction { get; set; }
        void Awake()
        {
            instance = this;

            FineAmount = ((BaseUnityPlugin)this).Config.Bind<float>("Fines",
                "Fine for dead player",
                20f,
                new ConfigDescription("What percent of current credits should be taken for each dead player.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            InsuranceReduction = ((BaseUnityPlugin)this).Config.Bind<float>("Fines",
                "Fine reduction for retrieving the body",
                40f,
                new ConfigDescription("This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            CompanyFineAmount = ((BaseUnityPlugin)this).Config.Bind<float>("Fines",
                "Fine for dead player (on Gordion)",
                0f,
                new ConfigDescription("What percent of current credits should be taken for each dead player.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            CompanyInsuranceReduction = ((BaseUnityPlugin)this).Config.Bind<float>("Fines",
                "Fine reduction for retrieving the body (on Gordion)",
                0f,
                new ConfigDescription("This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            QuotaIncreasePercent = ((BaseUnityPlugin)this).Config.Bind<int>("_Quota_",
                "Quota Increase Percent",
                10,
                new ConfigDescription("This value determines how much the quota should increase per dead player that is not retrived. 0 will not increase the quota, 10 increases the quota by 10% per missing player, 50 by 50%, 100 by 100% and so on.",
                (AcceptableValueBase)(object)new AcceptableValueRange<int>(0, 99999), Array.Empty<object>()));

            DynamicScrapBool = ((BaseUnityPlugin)this).Config.Bind<bool>("Misc",
                "Dynamic Scrap Calculation",
                false,
                new ConfigDescription("Set to true to enable. This setting makes the scrap value scale based on the current quota and the enemy power level of the moon. This generally makes the game harder but can allow for reaching higher quotas than typically possible."));

            ScrapValueOffset = ((BaseUnityPlugin)this).Config.Bind<int>("Misc",
                "Scrap Value Offset",
                100,
                new ConfigDescription("This value determines how much extra scrap value should be added to each moon. This value takes the apparatus into account and therefore must be equal to or higher than its value. (80 is the vanilla apparatus value, if you use FacilityMeltdown to make the apparatus worth 300 for instance. You MUST set this value to 300 or higher.)",
                (AcceptableValueBase)(object)new AcceptableValueRange<int>(0, 99999), Array.Empty<object>()));

            EnemyThreshhold = ((BaseUnityPlugin)this).Config.Bind<int>("Misc",
                "Enemy Power Threshhold",
                8,
                new ConfigDescription("Every time the Interior Enemy Power Count of a moon exceeds this value, 1 will be added to a difficulty multiplier. With the value at 5, a moon with an interior power of 14 will have a difficulty adjustment of 3x. if the value is 10, then the moon will only have a difficulty adjustment of 2x. If it were 3 then it would have a difficulty adjustment of 5x. Etc.",
                (AcceptableValueBase)(object)new AcceptableValueRange<int>(1, 99999), Array.Empty<object>()));

            MinDiff = ((BaseUnityPlugin)this).Config.Bind<float>("Misc",
                "Multiplier for Min Scrap Value",
                50f,
                new ConfigDescription("The percent of the quota the min scrap on a planet should be. Min < Max. The difficulty multiplier and Offset are applied after.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(1f, 99999f), Array.Empty<object>()));

            MaxDiff = ((BaseUnityPlugin)this).Config.Bind<float>("Misc",
                "Multiplier for Max Scrap Value",
                100f,
                new ConfigDescription("The percent of the quota the max scrap on a planet should be. Max > Min. The difficulty multiplier and Offset are applied after.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(1f, 99999f), Array.Empty<object>()));

            CurrentFineAmount = FineAmount.Value;
            CurrentInsuranceReduction = InsuranceReduction.Value;

            harmony.PatchAll(typeof(StartOfRound_Awake_Patch));
            harmony.PatchAll(typeof(CustomDeathPenalty));
            harmony.PatchAll(typeof(ArrivalSwitch));
            harmony.PatchAll(typeof(ChangeFineAmount));
            harmony.PatchAll(typeof(ChangeQuota));
            harmony.PatchAll(typeof(ChangePenaltyText));
            harmony.PatchAll(typeof(HUDManagerPatch));
            harmony.PatchAll(typeof(ShipleaveCalc));
            harmony.PatchAll(typeof(DynamicScrapSpawn));

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }
    }
}