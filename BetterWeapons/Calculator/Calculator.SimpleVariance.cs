using BattleTech;
using System.Collections.Generic;

namespace BetterWeapons
{
    static partial class Calculator
    {
        public static class SimpleVariance
        {
            public static readonly Dictionary<string, VarianceBounds> BoundsCache = new();
            public static bool _precomputed;

            public static void PrecomputeIfReady()
            {
                if (_precomputed) return;

                var dm = UnityGameInstance.BattleTechGame?.DataManager;
                var defs = dm?.WeaponDefs;
                if (defs == null)
                {
                    if (Main.settings.Debug) Main.Logger?.Warning.Log("[SimpleVariance] DataManager not ready; skipping precompute.");
                    return; // try again later
                }

                // inside Calculator.SimpleVariance.PrecomputeIfReady()
                BoundsCache.Clear();

                float sdFactor = Main.settings.StandardDeviationPercentOfSimpleVariance; // e.g., 0.75f

                foreach (var kvp in defs)
                {
                    var defId = kvp.Key;
                    var def = kvp.Value;

                    float dps = def.Damage;
                    if (dps <= Epsilon) continue;

                    float variance = (def.DamageVariance > 0f)
                        ? def.DamageVariance
                        : dps * Main.settings.DamageVarianceDefault;

                    // clamp bounds and guarantee a non-zero range
                    float min = UnityEngine.Mathf.Max(0f, dps - variance);
                    float max = dps + variance;
                    if (max - min < Epsilon) max = min + Epsilon;

                    float stdDev = sdFactor * variance;

                    BoundsCache[defId] = new VarianceBounds(min, max, stdDev);
                }

                _precomputed = true;

                if (Main.settings.Debug)
                    Main.Logger?.Info.Log($"[SimpleVariance] Precomputed bounds for {BoundsCache.Count} weapons (σ factor={sdFactor:0.###}).");
            }

            public static float Calculate(Weapon weapon, float rawDamage)
            {
                if (weapon == null || weapon.DamagePerShot <= Epsilon) return rawDamage;

                // Try to precompute if not done yet
                PrecomputeIfReady();

                // Fast path: cached?
                if (!BoundsCache.TryGetValue(weapon.defId, out var bounds))
                {
                    // Fallback: compute just for this weapon (e.g., late-loaded mod defs)
                    float dps = weapon.DamagePerShot;
                    if (dps <= Calculator.Epsilon)
                    {
                        // No meaningful variance — fall back to rawDamage via caller logic
                        bounds = new VarianceBounds(0f, dps, Calculator.Epsilon);
                        BoundsCache[weapon.defId] = bounds;
                    }
                    else
                    {
                        float variance = (weapon.weaponDef.DamageVariance > 0f)
                            ? weapon.weaponDef.DamageVariance
                            : dps * Main.settings.DamageVarianceDefault;

                        // Clamp and ensure non-zero range
                        float min = UnityEngine.Mathf.Max(0f, dps - variance);
                        float max = dps + variance;
                        if (max - min < Calculator.Epsilon) max = min + Calculator.Epsilon;

                        // SD is a FRACTION of variance (e.g., 0.75f)
                        float stdDev = Main.settings.StandardDeviationPercentOfSimpleVariance * variance;

                        bounds = new VarianceBounds(min, max, stdDev);
                        BoundsCache[weapon.defId] = bounds;
                    }
                }

                float adjustment = rawDamage / weapon.DamagePerShot;
                float roll = NormalDistribution.Random(bounds);
                float variantDamage = roll * adjustment;

                if (Main.settings.Debug)
                    Main.Logger?.Info.Log($"[SimpleVariance] defId={weapon.defId}, roll={roll}, final={variantDamage}");

                return variantDamage;
            }
        }
    }
}
