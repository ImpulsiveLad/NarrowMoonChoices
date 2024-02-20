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
        private const string modVersion = "1.2.0";
        private readonly Harmony harmony = new Harmony(modGUID);
        public ManualLogSource mls;
        public static CustomDeathPenaltyMain instance;
        public static ConfigEntry<float> FineAmount;
        public static ConfigEntry<float> InsuranceReduction;
        public static ConfigEntry<float> CompanyFineAmount;
        public static ConfigEntry<float> CompanyInsuranceReduction;
        public static ConfigEntry<int> QuotaIncreasePercent;
        public static float CurrentFineAmount { get; set; }
        public static float CurrentInsuranceReduction { get; set; }
        void Awake()
        {
            instance = this;

            FineAmount = ((BaseUnityPlugin)this).Config.Bind<float>("Penalty",
                "Fine for dead player",
                20f,
                new ConfigDescription("What percent of current credits should be taken for each dead player.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            InsuranceReduction = ((BaseUnityPlugin)this).Config.Bind<float>("Penalty",
                "Fine reduction for retrieving the body",
                40f,
                new ConfigDescription("This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            CompanyFineAmount = ((BaseUnityPlugin)this).Config.Bind<float>("Company",
                "Fine for dead player (on Gordion)",
                0f,
                new ConfigDescription("What percent of current credits should be taken for each dead player.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            CompanyInsuranceReduction = ((BaseUnityPlugin)this).Config.Bind<float>("Company",
                "Fine reduction for retrieving the body (on Gordion)",
                0f,
                new ConfigDescription("This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            QuotaIncreasePercent = ((BaseUnityPlugin)this).Config.Bind<int>("Penalty",
                "Quota Increase Percent",
                10,
                new ConfigDescription("This value determines how much the quota should increase per dead player that is not retrived. 0 will not increase the quota, 10 increases the quota by 10% per missing player, 50 by 50%, 100 by 100% and so on.",
                (AcceptableValueBase)(object)new AcceptableValueRange<int>(0, 99999), Array.Empty<object>()));


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

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }
    }
}