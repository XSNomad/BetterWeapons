using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using UnityEngine;

namespace BetterWeapons
{
    internal static partial class Calculator
    {
        public static void DLog(string msg)
        {
            if (Main.settings.Debug)
                Main.Logger?.Info.Log(msg);
        }

        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        private static class DistanceBasedVariance
        {
            private const double Pi2 = Math.PI / 2.0;
            private const string TagPrefix = "wr-variance_by_distance";
            private static readonly char[] SplitDash = { '-' };

            // Cached per-def params
            private static readonly Dictionary<string, float> SlopeByDef = new(); // tag X (rate), default 1.0
            private static readonly Dictionary<string, float> FloorByDef = new(); // floor (0..1) from settings per weapon family

            public static bool IsApplicable(Weapon weapon)
            {
                return Main.settings.DistanceBasedVariance && EnsureCached(weapon);
            }

            public static float Calculate(ICombatant attacker, ICombatant target, Weapon weapon, float damage, float rawDamage)
            {
                var dist = Vector3.Distance(attacker.TargetPosition, target.TargetPosition);
                if (dist >= weapon.MaxRange) return damage; // out of range → leave unchanged

                // r = 1 at point-blank, 0 at max range
                float r = Mathf.Clamp01((weapon.MaxRange - dist) / Mathf.Max(weapon.MaxRange, 1f));

                float slope = SlopeByDef[weapon.defId];              // rate
                float floor = Mathf.Clamp01(FloorByDef[weapon.defId]); // 0..1

                // Normalize atan shape to 0..1
                float shaped = (float)(Math.Atan(Pi2 * slope * r) / Pi2); // 0..1 as r goes 0..1
                float mult = Mathf.Lerp(floor, 1f, shaped); // floor..1

                float result = damage * mult; // IMPORTANT: no '* adjustment' here

                if (Main.settings.Debug)
                    Main.Logger?.Info.Log($"[DBV] def={weapon.defId} dist={dist:F1} r={r:F3} slope={slope:F2} floor={floor:F2} shaped={shaped:F3} mult={mult:F3} dmg={result:F2}");

                return result;
            }

            private static bool EnsureCached(Weapon weapon)
            {
                if (SlopeByDef.ContainsKey(weapon.defId)) return true;

                // Floor from settings by family (fractions 0..1)
                float floor =
                    (weapon.Type == WeaponType.Autocannon && weapon.weaponDef.WeaponSubType.ToString().IndexOf("LB", StringComparison.OrdinalIgnoreCase) >= 0) ? Main.settings.LBX_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.Autocannon) ? Main.settings.AC_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.COIL) ? Main.settings.COIL_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.Flamer) ? Main.settings.Flamer_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.Gauss) ? Main.settings.Gauss_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.Laser) ? Main.settings.Laser_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.SRM) ? Main.settings.SRM_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.MachineGun) ? Main.settings.MG_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.PPC && weapon.weaponDef.WeaponSubType == WeaponSubType.PPCSnub) ? Main.settings.SnubPPC_DistanceMultiplierDefault :
                    (weapon.Type == WeaponType.PPC) ? Main.settings.PPC_DistanceMultiplierDefault :
                                                             Main.settings.DistanceBasedVarianceMaxRangeDamageMultiplier; // generic fallback

                // Slope from tag (default 1.0 if absent)
                float slope = 1.0f;
                var tags = weapon.weaponDef.ComponentTags;
                var tag = tags.FirstOrDefault(t => t.StartsWith(TagPrefix, StringComparison.InvariantCultureIgnoreCase));
                if (!string.IsNullOrEmpty(tag) && !tag.Equals(TagPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    var parts = tag.Split(SplitDash, 3);
                    var last = parts[parts.Length - 1];
                    if (float.TryParse(last, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) && parsed > 0f)
                        slope = parsed;
                }

                FloorByDef[weapon.defId] = floor;
                SlopeByDef[weapon.defId] = slope;
                return true;
            }
        }

        private static class ReverseDistanceBasedVariance
        {
            private const double Pi2 = Math.PI / 2.0;
            private const string TagPrefix = "wr-reverse_variance_by_distance";
            private static readonly char[] SplitDash = { '-' };

            // Cached per-def params
            private static readonly Dictionary<string, float> SlopeByDef = new(); // rate
            private static readonly Dictionary<string, float> FloorByDef = new(); // floor at MinRange (0..1)

            public static bool IsApplicable(Weapon weapon)
            {
                return Main.settings.ReverseDistanceBasedVariance && EnsureCached(weapon);
            }

            public static float Calculate(ICombatant attacker, ICombatant target, Weapon weapon, float damage, float rawDamage)
            {
                var dist = Vector3.Distance(attacker.TargetPosition, target.TargetPosition);

                if (dist < weapon.MinRange) return 0f;      // inside min range → no effect (matches prior behavior)
                if (dist > weapon.MaxRange) return damage;  // out of max → unchanged

                float span = Mathf.Max(weapon.MaxRange - weapon.MinRange, 1f);
                // r = 0 at MinRange, 1 at MaxRange
                float r = Mathf.Clamp01((dist - weapon.MinRange) / span);

                float slope = SlopeByDef[weapon.defId];
                float floor = Mathf.Clamp01(FloorByDef[weapon.defId]); // floor at MinRange

                float shaped = (float)(Math.Atan(Pi2 * slope * r) / Pi2); // 0..1
                float mult = Mathf.Lerp(floor, 1f, shaped); // floor..1

                float result = damage * mult; // IMPORTANT: no '* adjustment' here

                if (Main.settings.Debug)
                    Main.Logger?.Info.Log($"[RDBV] def={weapon.defId} dist={dist:F1} r={r:F3} slope={slope:F2} floor={floor:F2} shaped={shaped:F3} mult={mult:F3} dmg={result:F2}");

                return result;
            }

            private static bool EnsureCached(Weapon weapon)
            {
                if (SlopeByDef.ContainsKey(weapon.defId)) return true;

                // Floor: by default only LRMs use reverse shaping; otherwise use global min-range floor
                bool isLRM = (weapon.Type == WeaponType.LRM);
                float floor = isLRM
                    ? Main.settings.LRM_DistanceMultiplierDefault
                    : Main.settings.ReverseDistanceBasedVarianceMinRangeDamageMultiplier;

                // Slope from tag (default 1.0 if absent)
                float slope = 1.0f;
                var tags = weapon.weaponDef.ComponentTags;
                var tag = tags.FirstOrDefault(t => t.StartsWith(TagPrefix, StringComparison.InvariantCultureIgnoreCase));
                if (!string.IsNullOrEmpty(tag) && !tag.Equals(TagPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    var parts = tag.Split(SplitDash, 3);
                    var last = parts[parts.Length - 1];
                    if (float.TryParse(last, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) && parsed > 0f)
                        slope = parsed;
                }

                FloorByDef[weapon.defId] = floor;
                SlopeByDef[weapon.defId] = slope;
                return true;
            }
        }
    }
}