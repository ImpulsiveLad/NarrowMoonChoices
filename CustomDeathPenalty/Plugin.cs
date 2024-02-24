using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Lib;
using CSync.Util;
using HarmonyLib;
using System.Runtime.Serialization;

namespace CustomDeathPenalty
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class CustomDeathPenaltyMain : BaseUnityPlugin
    {
        private const string modGUID = "impulse.CustomDeathPenalty";
        private const string modName = "CustomDeathPenalty";
        private const string modVersion = "1.5.0";
        private readonly Harmony harmony = new Harmony(modGUID);

        public ManualLogSource mls;

        public static CustomDeathPenaltyMain instance;
        public new static SyncConfig Config;
        void Awake()
        {
            instance = this;

            Config = new SyncConfig(base.Config);

            harmony.PatchAll(typeof(StartOfRound_Awake_Patch));
            harmony.PatchAll(typeof(CustomDeathPenalty));
            harmony.PatchAll(typeof(ArrivalSwitch));
            harmony.PatchAll(typeof(ChangeFineAmount));
            harmony.PatchAll(typeof(ChangeQuota));
            harmony.PatchAll(typeof(ChangePenaltyText));
            harmony.PatchAll(typeof(HUDManagerPatch));
            harmony.PatchAll(typeof(ShipleaveCalc));
            harmony.PatchAll(typeof(DynamicScrapSpawn));
            harmony.PatchAll(typeof(SyncConfig));

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }
    }
    [DataContract]
    public class SyncConfig : SyncedInstance<SyncConfig>
    {
        [DataMember] public SyncedEntry<float> FineAmount { get; private set; }
        [DataMember] public SyncedEntry<float> InsuranceReduction { get; private set; }
        [DataMember] public SyncedEntry<float> CompanyFineAmount { get; private set; }
        [DataMember] public SyncedEntry<float> CompanyInsuranceReduction { get; private set; }
        [DataMember] public SyncedEntry<int> QuotaIncreasePercent { get; private set; }
        [DataMember] public SyncedEntry<bool> DynamicScrapBool { get; private set; }
        [DataMember] public SyncedEntry<int> ScrapValueOffset { get; private set; }
        [DataMember] public SyncedEntry<int> EnemyThreshold { get; private set; }
        [DataMember] public SyncedEntry<float> MinDiff { get; private set; }
        [DataMember] public SyncedEntry<float> MaxDiff { get; private set; }
        public static float CurrentFineAmount { get; set; }
        public static float CurrentInsuranceReduction { get; set; }
        public SyncConfig(ConfigFile cfg)
        {

            EasySync.SyncManager.RegisterForSyncing(this, "impulse.CustomDeathPenalty");

            FineAmount = cfg.BindSyncedEntry("Fines", "Fine for dead player", 20f,
    "What percent of current credits should be taken for each dead player. Range 0 to 100");

            InsuranceReduction = cfg.BindSyncedEntry("Fines", "Fine reduction for retrieving the body", 40f,
    "This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not. Range 0 to 100");

            CompanyFineAmount = cfg.BindSyncedEntry("Fines", "Fine for dead player (on Gordion)", 0f,
    "What percent of current credits should be taken for each dead player. Range 0 to 100");

            CompanyInsuranceReduction = cfg.BindSyncedEntry("Fines", "Fine reduction for retrieving the body (on Gordion)", 0f,
    "This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not. Range 0 to 100");

            QuotaIncreasePercent = cfg.BindSyncedEntry("_Quota_", "Quota Increase Percent", 10,
    "This value determines how much the quota should increase per dead player that is not retrived. 0 will not increase the quota, 10 increases the quota by 10% per missing player, 50 by 50%, 100 by 100% and so on. Range 0 to \u221E");

            DynamicScrapBool = cfg.BindSyncedEntry("Misc", "Dynamic Scrap Calculation", false,
    "Set to true to enable. This setting makes the scrap value scale based on the current quota and the enemy power level of the moon. This generally makes the game harder but can allow for reaching higher quotas than typically possible.");

            ScrapValueOffset = cfg.BindSyncedEntry("Misc", "Scrap Value Offset", 100,
    "This value determines how much extra scrap value should be added to each moon. This value takes the apparatus into account and therefore must be equal to or higher than its value. (80 is the vanilla apparatus value, if you use FacilityMeltdown to make the apparatus worth 300 for instance. You MUST set this value to 300 or higher.) Range 0 to \u221E");

            EnemyThreshold = cfg.BindSyncedEntry("Misc", "Enemy Power Threshold", 8,
    "Every time the Interior Enemy Power Count of a moon exceeds this value, 1 will be added to a difficulty multiplier. With the value at 5, a moon with an interior power of 14 will have a difficulty adjustment of 3x. if the value is 10, then the moon will only have a difficulty adjustment of 2x. If it were 3 then it would have a difficulty adjustment of 5x. Etc. Range 1 to \u221E");

            MinDiff = cfg.BindSyncedEntry("Misc", "Multiplier for Min Scrap Value", 50f,
    "The percent of the quota the min scrap on a planet should be. Min < Max. The difficulty multiplier and Offset are applied after. Range 1 to \u221E");

            MaxDiff = cfg.BindSyncedEntry("Misc", "Multiplier for Max Scrap Value", 100f,
    "The percent of the quota the max scrap on a planet should be. Max > Min. The difficulty multiplier and Offset are applied after. Range 1 to \u221E");

            CurrentFineAmount = FineAmount.Value;
            CurrentInsuranceReduction = InsuranceReduction.Value;

            AcceptableRanges();
        }
        public void AcceptableRanges()
        {
            float fineAmount = FineAmount.Value;
            if (fineAmount < 0f) fineAmount = 0f;
            if (fineAmount > 100f) fineAmount = 100f;
            FineAmount.Value = fineAmount;

            float insuranceReduction = InsuranceReduction.Value;
            if (insuranceReduction < 0f) insuranceReduction = 0f;
            if (insuranceReduction > 100f) insuranceReduction = 100f;
            InsuranceReduction.Value = insuranceReduction;

            float companyFineAmount = CompanyFineAmount.Value;
            if (companyFineAmount < 0f) companyFineAmount = 0f;
            if (companyFineAmount > 100f) companyFineAmount = 100f;
            CompanyFineAmount.Value = companyFineAmount;

            float companyInsuranceReduction = CompanyInsuranceReduction.Value;
            if (companyInsuranceReduction < 0f) companyInsuranceReduction = 0f;
            if (companyInsuranceReduction > 100f) companyInsuranceReduction = 100f;
            CompanyInsuranceReduction.Value = companyInsuranceReduction;

            int quotaIncreasePercent = QuotaIncreasePercent.Value;
            if (quotaIncreasePercent < 0) quotaIncreasePercent = 0;
            QuotaIncreasePercent.Value = quotaIncreasePercent;

            int scrapValueOffset = ScrapValueOffset.Value;
            if (scrapValueOffset < 0) scrapValueOffset = 0;
            ScrapValueOffset.Value = scrapValueOffset;

            int enemyThreshold = EnemyThreshold.Value;
            if (enemyThreshold < 1) enemyThreshold = 1;
            EnemyThreshold.Value = enemyThreshold;

            float minDiff = MinDiff.Value;
            if (minDiff < 1) minDiff = 1;
            MinDiff.Value = minDiff;

            float maxDiff = MaxDiff.Value;
            if (maxDiff < 1) maxDiff = 1;
            MaxDiff.Value = maxDiff;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave()
        {
            RevertSync();
        }
    }
}
