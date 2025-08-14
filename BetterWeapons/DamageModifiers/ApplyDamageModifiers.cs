using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterWeapons
{
    [HarmonyPatch(typeof(AttackDirector.AttackSequence), nameof(AttackDirector.AttackSequence.OnAttackSequenceImpact))]
    public static class ApplyDamageModifiers
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var attackerField = AccessTools.Field(typeof(AttackDirector.AttackSequence), "attacker");
            var targetField = AccessTools.Field(typeof(AttackDirector.AttackSequence), "chosenTarget");
            var calcMethod = AccessTools.Method(typeof(Calculator), nameof(Calculator.ApplyDamageModifiers),
                new Type[] { typeof(AbstractActor), typeof(ICombatant), typeof(Weapon), typeof(float) });

            var newInstructions = new List<CodeInstruction>(instructions);

            for (int i = 0; i < newInstructions.Count; i++)
            {
                if (newInstructions[i].opcode == OpCodes.Ldfld && Equals(newInstructions[i].operand, targetField))
                {
                    // Insert before this point
                    newInstructions.InsertRange(i, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),                // this
                        new CodeInstruction(OpCodes.Ldfld, attackerField),   // load attacker
                        new CodeInstruction(OpCodes.Ldarg_0),                // this
                        new CodeInstruction(OpCodes.Ldfld, targetField),     // load target
                        new CodeInstruction(OpCodes.Ldloc_S, 4),             // weapon
                        new CodeInstruction(OpCodes.Ldloc_S, 8),             // rawDamage
                        new CodeInstruction(OpCodes.Call, calcMethod),       // call Calculator.ApplyDamageModifiers
                        new CodeInstruction(OpCodes.Stloc_S, 8)              // store result
                    });

                    if (Main.settings.Debug)
                        Main.Logger?.Info.Log("[ApplyDamageModifiers] Patch inserted into OnAttackSequenceImpact");

                    break;
                }
            }

            return newInstructions;
        }
    }
}
