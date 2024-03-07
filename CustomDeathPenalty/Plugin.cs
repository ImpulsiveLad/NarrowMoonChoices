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
        private const string modVersion = "1.7.7";
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
            harmony.PatchAll(typeof(DynamicInteriorSize));
            harmony.PatchAll(typeof(SyncConfig));

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }
    }
    [DataContract]
    public class SyncConfig : SyncedConfig<SyncConfig>
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
        [DataMember] public SyncedEntry<bool> DynamicSizeBool { get; private set; }
        [DataMember] public SyncedEntry<int> SizeScrapThreshold { get; private set; }
        [DataMember] public SyncedEntry<float> SizeOffset { get; private set; }
        [DataMember] public SyncedEntry<float> MinSizeClamp { get; private set; }
        [DataMember] public SyncedEntry<float> MaxSizeClamp { get; private set; }
        [DataMember] public SyncedEntry<bool> PlayerCountBasedPenalty { get; private set; }
        [DataMember] public SyncedEntry<int> DynamicQuotaPercent { get; private set; }
        [DataMember] public SyncedEntry<float> EnemyThresholdWeight { get; private set; }
        public static float CurrentFineAmount { get; set; }
        public static float CurrentInsuranceReduction { get; set; }
        public SyncConfig(ConfigFile cfg) : base("CustomDeathPenalty")
        {
            ConfigManager.Register(this);

            FineAmount = cfg.BindSyncedEntry("2. Fines", "Fine for each dead player", 20f, "What percent of current credits should be taken for each dead player. Range 0 to 100");

            InsuranceReduction = cfg.BindSyncedEntry("2. Fines", "Fine reduction for retrieving the players body", 40f, "This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not. Range 0 to 100");

            CompanyFineAmount = cfg.BindSyncedEntry("2. Fines", "Fine for each dead player (on Gordion)", 0f, "What percent of current credits should be taken for each dead player. Range 0 to 100");

            CompanyInsuranceReduction = cfg.BindSyncedEntry("2. Fines", "Fine reduction for retrieving the players body (on Gordion)", 0f, "This value decides how much the penalty should be reduced if the dead player's body is retrieved. For example: 0 results in no fine for recovered bodies, 50 results in being fined half for retriving the body, and 100 results in no difference whether the body is recovered or not. Range 0 to 100");

            QuotaIncreasePercent = cfg.BindSyncedEntry("1. Quota", "Quota Increase Percent", 10, "This value determines how much the quota should increase per dead player that is not retrived. 0 will not increase the quota, 10 increases the quota by 10% per missing player, 50 by 50%, 100 by 100% and so on. Range 0 to \u221E");

            PlayerCountBasedPenalty = cfg.BindSyncedEntry("1. Quota", "Dynamic Quota Increase", false, "If set to true, the quota increase will instead be a set value times the ratio of unrecovered bodies over the total players.");

            DynamicQuotaPercent = cfg.BindSyncedEntry("1. Quota", "Dynamic Quota Percent", 100, "If all players are dead, the quota will increase by this amount. If only some of the players are unrecovered then the quota will increase based on the fraction of the lobby that is dead and unrecovered. Range 0 to \u221E");

            DynamicScrapBool = cfg.BindSyncedEntry("3. Dynamic Scrap", "Dynamic Scrap Calculation", false, "Set to true to enable. This setting makes the scrap value scale based on the current quota and the enemy power level of the moon. This generally makes the game harder but can allow for reaching higher quotas than typically possible.");

            ScrapValueOffset = cfg.BindSyncedEntry("3. Dynamic Scrap", "Scrap Value Offset", 100, "This value determines how much extra scrap value should be added to each moon. This value takes the apparatus into account and therefore must be equal to or higher than its value. (80 is the vanilla apparatus value, if you use FacilityMeltdown to make the apparatus worth 300 for instance. You MUST set this value to 300 or higher.) Range 0 to \u221E");

            EnemyThreshold = cfg.BindSyncedEntry("3. Dynamic Scrap", "Enemy Power Threshold", 8, "Every time the Interior Enemy Power Count of a moon exceeds this value, 0.XX (see next setting) will be added to 1, this multiplier is then applied to the scrap value calculation. With the value at 3, a moon with an interior power of 14 will have a difficulty adjustment of 4 * 0.XX + 1. if the value is 5, then the moon will only have a difficulty adjustment of 2 * 0.XX + 1. Etc. Range 1 to \u221E");

            EnemyThresholdWeight = cfg.BindSyncedEntry("3. Dynamic Scrap", "Enemy Power Threshold Weight", 25f, "This setting controls the % increase of the enemy factor in the dynamic scrap calculation. 25% will make it so that each time that the current moons interior enemy power count exceeds the setting able, 0.25 will be added to the 1 in the multipler. If it is exceeded 3 times for instance then the scrap will be multiplied by 1.75. Range 0 to \u221E");

            MinDiff = cfg.BindSyncedEntry("3. Dynamic Scrap", "Multiplier for Min Scrap Value", 50f, "The percent of the quota the min scrap on a planet should be. Min < Max. The difficulty multiplier and Offset are applied after. Range 1 to \u221E");

            MaxDiff = cfg.BindSyncedEntry("3. Dynamic Scrap", "Multiplier for Max Scrap Value", 100f, "The percent of the quota the max scrap on a planet should be. Max > Min. The difficulty multiplier and Offset are applied after. Range 1 to \u221E");

            DynamicSizeBool = cfg.BindSyncedEntry("4. Dynamic Interior Size", "Dynamic Interior Size Calculation", false, "Set to true to enable. This setting makes the interior size be based on the scrap value. IE every x worth of scrap increases the interior size.");

            SizeScrapThreshold = cfg.BindSyncedEntry("4. Dynamic Interior Size", "Size Increase Threshold", 50, "Everytime the minimum scrap on a moon exceeds this value, 0.01 will be added to the size multiplier. At 50, the multiplier increases by 1x every 5000 scrap on the moon. Range 1 to \u221E");

            SizeOffset = cfg.BindSyncedEntry("4. Dynamic Interior Size", "Size Offset", 1f, "This controls the intial multiplier the threshold adds onto. Keep in mind that if it is below or above the clamps, it will be corrected as such until it enters an acceptable range.");

            MinSizeClamp = cfg.BindSyncedEntry("4. Dynamic Interior Size", "Min Size Clamp", 1f, "If the calculated value of the interior is below this, it will correct to this value. I cannot verify how low this can safely get. It varies by interior.");

            MaxSizeClamp = cfg.BindSyncedEntry("4. Dynamic Interior Size", "Max Size Clamp", 3f, "If the calculated value of the interior is above this, it will correct to this value. I cannot verify how high this can safely get. It varies by interior.");

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

            int sizeScrapThreshold = SizeScrapThreshold.Value;
            if (sizeScrapThreshold < 1) sizeScrapThreshold = 1;
            SizeScrapThreshold.Value = sizeScrapThreshold;

            float sizeOffset = SizeOffset.Value;
            if (sizeOffset < 0) sizeOffset = 0;
            SizeOffset.Value = sizeOffset;

            float minSizeClamp = MinSizeClamp.Value;
            if (minSizeClamp < 0) minSizeClamp = 0;
            MinSizeClamp.Value = minSizeClamp;

            float maxSizeClamp = MaxSizeClamp.Value;
            if (maxSizeClamp < 0) maxSizeClamp = 0;
            MaxSizeClamp.Value = maxSizeClamp;

            int dynamicQuotaPercent = DynamicQuotaPercent.Value;
            if (dynamicQuotaPercent < 0)  dynamicQuotaPercent = 0;
            DynamicQuotaPercent.Value = dynamicQuotaPercent;

            float enemyThresholdWeight = EnemyThresholdWeight.Value;
            if (enemyThresholdWeight < 0) enemyThresholdWeight = 0;
            EnemyThresholdWeight.Value = enemyThresholdWeight;
        }
    }
}
