using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace CustomDeathPenalty
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class CustomDeathPenalty : BaseUnityPlugin
    {
        private const string modGUID = "impulse.CustomDeathPenalty";
        private const string modName = "CustomDeathPenalty";
        private const string modVersion = "0.5.0";
        private readonly Harmony harmony = new Harmony(modGUID);
        public static ConfigEntry<float> FineAmount;
        public static ConfigEntry<float> InsuranceReduction;
        void Awake()
        {
            FineAmount = ((BaseUnityPlugin)this).Config.Bind<float>("Penalty",
                "FineAmount",
                20f,
                new ConfigDescription("What percent of current credits should be taken for each dead player.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            InsuranceReduction = ((BaseUnityPlugin)this).Config.Bind<float>("Penalty",
                "InsuranceReduction",
                40f,
                new ConfigDescription("This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not.",
                (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 100f), Array.Empty<object>()));

            harmony.PatchAll(typeof(ChangeFineAmount));
        }
    }
}