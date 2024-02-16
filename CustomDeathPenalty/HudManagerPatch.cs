using HarmonyLib;
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

}
