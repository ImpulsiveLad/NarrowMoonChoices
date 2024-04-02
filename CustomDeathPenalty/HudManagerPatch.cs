using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CustomDeathPenalty
{
    [HarmonyPatch(typeof(StartOfRound), "ArriveAtLevel")]
    public class ArrivalSwitch
    {
        public static float CurrentFineAmount { get; set; }
        public static float CurrentInsuranceReduction { get; set; }

        public static SelectableLevel myReferenceToGordionLevel;
        static void Postfix(StartOfRound __instance)
        {
            CurrentFineAmount = SyncConfigCDP.Instance.FineAmount.Value;
            CurrentInsuranceReduction = SyncConfigCDP.Instance.InsuranceReduction.Value;
            if (__instance.currentLevel == myReferenceToGordionLevel)
            {
                CurrentFineAmount = SyncConfigCDP.Instance.CompanyFineAmount.Value;
                CurrentInsuranceReduction = SyncConfigCDP.Instance.CompanyInsuranceReduction.Value;
            }
            else
            {
                CurrentFineAmount = SyncConfigCDP.Instance.FineAmount.Value;
                CurrentInsuranceReduction = SyncConfigCDP.Instance.InsuranceReduction.Value;
            }
        }
    }
    public static class CustomDeathPenalty
    {
        public static float GetCurrentFineAmount()
        {
            if (StartOfRound.Instance != null && ArrivalSwitch.myReferenceToGordionLevel != null && ArrivalSwitch.myReferenceToGordionLevel == StartOfRound.Instance.currentLevel)
            {
                return SyncConfigCDP.Instance.CompanyFineAmount.Value / 100;
            }
            else
            {
                return SyncConfigCDP.Instance.FineAmount.Value / 100;
            }
        }
        public static float GetCurrentInsuranceReduction()
        {
            if (StartOfRound.Instance != null && ArrivalSwitch.myReferenceToGordionLevel != null && ArrivalSwitch.myReferenceToGordionLevel == StartOfRound.Instance.currentLevel)
            {
                return 1 / (SyncConfigCDP.Instance.CompanyInsuranceReduction.Value / 100);
            }
            else
            {
                return 1 / (SyncConfigCDP.Instance.InsuranceReduction.Value / 100);
            }
        }
    }
    [HarmonyPatch(typeof(HUDManager), "ApplyPenalty")]
    public class ChangeFineAmount
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4)
                {
                    if ((float)instruction.operand == 0.2f)
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = typeof(CustomDeathPenalty).GetMethod("GetCurrentFineAmount");
                    }
                    else if ((float)instruction.operand == 2.5f)
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = typeof(CustomDeathPenalty).GetMethod("GetCurrentInsuranceReduction");
                    }
                }
                yield return instruction;
            }
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
    public class ChangeQuota
    {
        public static int unrecoveredBodies;
        public static int oldQuota;
        public static int newQuota;
        static void Postfix(StartOfRound __instance)
        {
            int playersDead = (StartOfRound.Instance.connectedPlayersAmount + 1) - __instance.livingPlayers;
            int bodiesInsured = GetBodiesInShip();
            unrecoveredBodies = playersDead - bodiesInsured;
            if (unrecoveredBodies != 0 && ArrivalSwitch.myReferenceToGordionLevel != StartOfRound.Instance.currentLevel)
            {
                oldQuota = TimeOfDay.Instance.profitQuota;
                if (SyncConfigCDP.Instance.PlayerCountBasedPenalty.Value == false)
                {
                    int QuotaStep = (100 + SyncConfigCDP.Instance.QuotaIncreasePercent.Value * unrecoveredBodies) * TimeOfDay.Instance.profitQuota;
                    TimeOfDay.Instance.profitQuota = QuotaStep / 100;
                }
                else
                {
                    int QuotaStep2 = (int)(((float)(unrecoveredBodies / (StartOfRound.Instance.connectedPlayersAmount + 1)) * (SyncConfigCDP.Instance.DynamicQuotaPercent.Value / 100) + 1) * 100);
                    TimeOfDay.Instance.profitQuota *= QuotaStep2 / 100;
                }
                newQuota = TimeOfDay.Instance.profitQuota;
                CustomDeathPenaltyMain.instance.mls.LogInfo("Old Quota: " + oldQuota);
                CustomDeathPenaltyMain.instance.mls.LogInfo("New Quota: " + newQuota);
            }
            else
            {
                CustomDeathPenaltyMain.instance.mls.LogInfo("On Company moon, no quota multiplier applied");
            }
        }
        private static int GetBodiesInShip()
        {
            int num = 0;
            DeadBodyInfo[] array = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].isInShip)
                {
                    num++;
                }
            }
            return num;
        }
    }
    [HarmonyPatch(typeof(HUDManager), "ApplyPenalty")]
    public class ChangePenaltyText
    {
        static void Postfix(HUDManager __instance, int playersDead, int bodiesInsured)
        {
            if (SyncConfigCDP.Instance.QuotaIncreasePercent.Value != 0)
            {
                __instance.statsUIElements.penaltyAddition.text += $"\nUnrecovered bodies: {ChangeQuota.unrecoveredBodies}\nQuota has increased from {ChangeQuota.oldQuota} to {ChangeQuota.newQuota}";
            }
        }
    }
    [HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
    public static class DynamicScrapSpawn
    {
        [HarmonyPrefix]
        public static void Postfix(RoundManager __instance)
        {
            SelectableLevel currentLevel = __instance.currentLevel;
            float EnemyFactor = ((currentLevel.maxEnemyPowerCount / SyncConfigCDP.Instance.EnemyThreshold.Value) / (1 / (SyncConfigCDP.Instance.EnemyThresholdWeight.Value / 100))) + 1;
            CustomDeathPenaltyMain.instance.mls.LogInfo($"EnemyFactor: {EnemyFactor}");
            if (SyncConfigCDP.Instance.DynamicScrapBool.Value == true && ArrivalSwitch.myReferenceToGordionLevel != StartOfRound.Instance.currentLevel)
            {
                currentLevel.minTotalScrapValue = (int)(TimeOfDay.Instance.profitQuota * (SyncConfigCDP.Instance.MinDiff.Value / 100) * EnemyFactor) + SyncConfigCDP.Instance.ScrapValueOffset.Value;
                currentLevel.maxTotalScrapValue = (int)(TimeOfDay.Instance.profitQuota * (SyncConfigCDP.Instance.MaxDiff.Value / 100) * EnemyFactor) + SyncConfigCDP.Instance.ScrapValueOffset.Value;
                currentLevel.maxScrap = currentLevel.minTotalScrapValue / 25;
                if ((float)currentLevel.minTotalScrapValue / 333 < 1)
                {
                    currentLevel.minScrap = (int)Math.Round(currentLevel.maxScrap * (float)4 / 5);
                }
                else if ((float)currentLevel.minTotalScrapValue / 333 < 2)
                {
                    currentLevel.minScrap = (int)Math.Round(currentLevel.maxScrap * (float)3 / 5);
                }
                else if ((float)currentLevel.minTotalScrapValue / 333 < 3)
                {
                    currentLevel.minScrap = (int)Math.Round(currentLevel.maxScrap * (float)2 / 5);
                }
                else
                {
                    currentLevel.minScrap = (int)Math.Round(currentLevel.maxScrap * (float)1 / 5);
                }
                CustomDeathPenaltyMain.instance.mls.LogInfo($"minScrap: {currentLevel.minScrap}");
                CustomDeathPenaltyMain.instance.mls.LogInfo($"maxScrap: {currentLevel.maxScrap}");
                CustomDeathPenaltyMain.instance.mls.LogInfo($"minTotalScrapValue: {currentLevel.minTotalScrapValue}");
                CustomDeathPenaltyMain.instance.mls.LogInfo($"maxTotalScrapValue: {currentLevel.maxTotalScrapValue}");
            }
            else
            {
                CustomDeathPenaltyMain.instance.mls.LogInfo("Dynamic Scrap is either disabled or you are on the Company Moon.");
            }
        }
    }
    [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
    public static class DynamicInteriorSize
    {
        [HarmonyPrefix]
        public static void Postfix(RoundManager __instance)
        {
            SelectableLevel currentLevel = __instance.currentLevel;
            if (SyncConfigCDP.Instance.DynamicSizeBool.Value == true && ArrivalSwitch.myReferenceToGordionLevel != StartOfRound.Instance.currentLevel)
            {
                int MinScrapPrediction = (int)(TimeOfDay.Instance.profitQuota * (SyncConfigCDP.Instance.MinDiff.Value / 100) * (((int)currentLevel.maxEnemyPowerCount / SyncConfigCDP.Instance.EnemyThreshold.Value) + 1)) + SyncConfigCDP.Instance.ScrapValueOffset.Value;
                currentLevel.factorySizeMultiplier = ((float)(int)(MinScrapPrediction / SyncConfigCDP.Instance.SizeScrapThreshold.Value) / 100) + SyncConfigCDP.Instance.SizeOffset.Value;
                currentLevel.factorySizeMultiplier = ((float)(int)Math.Round(MinScrapPrediction / SyncConfigCDP.Instance.SizeScrapThreshold.Value) / 100) + SyncConfigCDP.Instance.SizeOffset.Value;
                CustomDeathPenaltyMain.instance.mls.LogInfo($"factorySizeMultiplier: {currentLevel.factorySizeMultiplier}");
            }
            else
            {
                CustomDeathPenaltyMain.instance.mls.LogInfo("Dynamic Size is disabled.");
            }
        }
    }
}