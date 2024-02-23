using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CustomDeathPenalty
{
    [HarmonyPatch(typeof(StartOfRound), "ArriveAtLevel")]
    public class ArrivalSwitch
    {
        public static SelectableLevel myReferenceToGordionLevel;
        static void Postfix(StartOfRound __instance)
        {
            if (__instance.currentLevel == myReferenceToGordionLevel)
            {
                CustomDeathPenaltyMain.CurrentFineAmount = CustomDeathPenaltyMain.CompanyFineAmount.Value;
                CustomDeathPenaltyMain.CurrentInsuranceReduction = CustomDeathPenaltyMain.CompanyInsuranceReduction.Value;
            }
            else
            {
                CustomDeathPenaltyMain.CurrentFineAmount = CustomDeathPenaltyMain.FineAmount.Value;
                CustomDeathPenaltyMain.CurrentInsuranceReduction = CustomDeathPenaltyMain.InsuranceReduction.Value;
            }
        }
    }
    public static class CustomDeathPenalty
    {
        public static float GetCurrentFineAmount()
        {
            if (StartOfRound.Instance != null && ArrivalSwitch.myReferenceToGordionLevel != null && ArrivalSwitch.myReferenceToGordionLevel == StartOfRound.Instance.currentLevel)
            {
                return CustomDeathPenaltyMain.CompanyFineAmount.Value / 100;
            }
            else
            {
                return CustomDeathPenaltyMain.FineAmount.Value / 100;
            }
        }
        public static float GetCurrentInsuranceReduction()
        {
            if (StartOfRound.Instance != null && ArrivalSwitch.myReferenceToGordionLevel != null && ArrivalSwitch.myReferenceToGordionLevel == StartOfRound.Instance.currentLevel)
            {
                return 1 / (CustomDeathPenaltyMain.CompanyInsuranceReduction.Value / 100);
            }
            else
            {
                return 1 / (CustomDeathPenaltyMain.InsuranceReduction.Value / 100);
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
                int QuotaStep = (100 + CustomDeathPenaltyMain.QuotaIncreasePercent.Value * unrecoveredBodies) * TimeOfDay.Instance.profitQuota;
                TimeOfDay.Instance.profitQuota = QuotaStep / 100;
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
            if (CustomDeathPenaltyMain.QuotaIncreasePercent.Value != 0)
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
            if (CustomDeathPenaltyMain.DynamicScrapBool.Value == true)
            {
                currentLevel.minTotalScrapValue = (int)(TimeOfDay.Instance.profitQuota * (CustomDeathPenaltyMain.MinDiff.Value / 100) * (((int)currentLevel.maxEnemyPowerCount / CustomDeathPenaltyMain.EnemyThreshold.Value) + 1)) + CustomDeathPenaltyMain.ScrapValueOffset.Value;
                currentLevel.maxTotalScrapValue = (int)(TimeOfDay.Instance.profitQuota * (CustomDeathPenaltyMain.MaxDiff.Value / 100) * (((int)currentLevel.maxEnemyPowerCount / CustomDeathPenaltyMain.EnemyThreshold.Value) + 1)) + CustomDeathPenaltyMain.ScrapValueOffset.Value;
                currentLevel.maxScrap = currentLevel.minTotalScrapValue / 20;
                currentLevel.minScrap = (int)Math.Round(currentLevel.maxScrap * (float)3 / 5);
                CustomDeathPenaltyMain.instance.mls.LogInfo($"minScrap: {currentLevel.minScrap}");
                CustomDeathPenaltyMain.instance.mls.LogInfo($"maxScrap: {currentLevel.maxScrap}");
                CustomDeathPenaltyMain.instance.mls.LogInfo($"minTotalScrapValue: {currentLevel.minTotalScrapValue}");
                CustomDeathPenaltyMain.instance.mls.LogInfo($"maxTotalScrapValue: {currentLevel.maxTotalScrapValue}");
            }
            else
            {
                CustomDeathPenaltyMain.instance.mls.LogInfo("Dynamic Scrap is disabled, spawning scrap the vanilla way.");
            }
        }
    }
}