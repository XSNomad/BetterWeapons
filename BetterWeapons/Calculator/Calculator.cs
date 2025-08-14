using System;
using BattleTech;

namespace BetterWeapons
{
    static partial class Calculator
    {
        internal const float Epsilon = 0.0001f;

        public static void Log(string msg)
        {
            if (Main.settings.Debug)
                Main.Logger?.Info.Log(msg);
        }

        internal static float ApplyDamageModifiers(AbstractActor attacker, ICombatant target, Weapon weapon, float rawDamage)
        {
            // Early outs and sanitization
            if (weapon == null || target == null || rawDamage <= 0f || float.IsNaN(rawDamage))
                return Math.Max(0f, rawDamage);

            return ApplyAllDamageModifiers(attacker, target, weapon, rawDamage);
        }

        internal static float ApplyAllDamageModifiers(AbstractActor attacker, ICombatant target, Weapon weapon, float rawDamage)
        {
            float damage = rawDamage;

            // 1) Simple variance
            if (Main.settings.SimpleVariance && Main.settings.DamageVarianceDefault > 0f)
            {
                Log("SimpleVariance.Calculate");
                damage = SimpleVariance.Calculate(weapon, damage);
            }

            // 2) Distance-based variance
            if (Main.settings.DistanceBasedVariance && DistanceBasedVariance.IsApplicable(weapon))
            {
                Log("DistanceBasedVariance.Calculate");
                damage = DistanceBasedVariance.Calculate(attacker, target, weapon, damage, rawDamage);
            }

            // 3) Reverse distance-based variance (e.g., LRMs)
            if (Main.settings.ReverseDistanceBasedVariance && ReverseDistanceBasedVariance.IsApplicable(weapon))
            {
                Log("ReverseDistanceBasedVariance.Calculate");
                damage = ReverseDistanceBasedVariance.Calculate(attacker, target, weapon, damage, rawDamage);
            }

            // 4) Overheat multiplier
            if (Main.settings.OverheatModifier && OverheatMultiplier.IsApplicable(weapon))
            {
                Log("OverheatMultiplier.Calculate");
                damage = OverheatMultiplier.Calculate(attacker, target, weapon, damage);
            }

            // 5) Heat as normal damage to non-’Mech targets
            if (HeatAsNormalDamage.IsApplicable(weapon))
            {
                Log("HeatAsNormalDamage.Calculate");
                damage = HeatAsNormalDamage.Calculate(target, weapon, damage, rawDamage);
            }

            // Final sanitize
            if (float.IsNaN(damage) || damage < 0f)
                damage = 0f;

            // Snap tiny values to zero to avoid floating dust
            if (damage < Epsilon)
                damage = 0f;

            return damage;
        }
    }
}
