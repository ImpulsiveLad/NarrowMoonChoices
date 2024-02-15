using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CustomDeathPenalty
{
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
                        instruction.operand = CustomDeathPenalty.FineAmount.Value/100;
                    }
                    else if ((float)instruction.operand == 2.5f)
                    {
                        instruction.operand = 1 / (CustomDeathPenalty.InsuranceReduction.Value / 100);
                    }
                }
                yield return instruction;
            }
        }
    }
}
