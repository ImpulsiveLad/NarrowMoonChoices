using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace CustomDeathPenalty
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class CustomDeathPenaltyMain : BaseUnityPlugin
    {
        private const string modGUID = "impulse.CustomDeathPenalty";
        private const string modName = "CustomDeathPenalty";
        private const string modVersion = "0.6.0";
        private readonly Harmony harmony = new Harmony(modGUID);
        public static ConfigEntry<float> FineAmount;
        public static ConfigEntry<float> InsuranceReduction;
        public static ConfigEntry<float> CompanyFineAmount;
        public static ConfigEntry<float> CompanyInsuranceReduction;
        public static float CurrentFineAmount { get; set; }
        public static float CurrentInsuranceReduction { get; set; }
        void Awake()
        {
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

            CurrentFineAmount = FineAmount.Value;
            CurrentInsuranceReduction = InsuranceReduction.Value;

            harmony.PatchAll(typeof(StartOfRound_Awake_Patch));
            harmony.PatchAll(typeof(CustomDeathPenalty));
            harmony.PatchAll(typeof(ArrivalSwitch));
            harmony.PatchAll(typeof(ChangeFineAmount));
        }
    }
}