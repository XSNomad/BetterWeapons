using BattleTech;
using Harmony;
using System;
using System.Text;

namespace BetterWeapons.Patches
{
    [HarmonyPatch(typeof(AttackDirector.AttackSequence), nameof(AttackDirector.AttackSequence.GetIndividualHits))]
    public static class ClusteredShotEnabler
    {
        public static bool Prefix(ref WeaponHitInfo hitInfo, int groupIdx, int weaponIdx, Weapon weapon, float toHitChance,
            float prevDodgedDamage, AttackDirector.AttackSequence __instance)
        {
            if (weapon == null || weapon.weaponDef == null)
                return true;

            // Cache subtype string to avoid repeated allocations
            string subTypeStr = weapon.weaponDef.WeaponSubType.ToString();

            bool isLbx = weapon.Type == WeaponType.Autocannon &&
                         subTypeStr.IndexOf("LB", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isSnubPpc = weapon.weaponDef.WeaponSubType == WeaponSubType.PPCSnub;

            if (isLbx || isSnubPpc)
            {
                __instance.GetClusteredHits(ref hitInfo, groupIdx, weaponIdx, weapon, toHitChance, prevDodgedDamage);

                if (Main.settings.Debug) // Use Debug flag consistently
                    PrintHitLocations(hitInfo);

                return false; // Skip original method
            }

            return true;
        }

        public static void PrintHitLocations(WeaponHitInfo hitInfo)
        {
            try
            {
                var sb = new StringBuilder()
                    .AppendLine($"clustered hits: {hitInfo.hitLocations.Length}");

                for (int i = 0; i < hitInfo.hitLocations.Length; i++)
                {
                    int location = hitInfo.hitLocations[i];

                    if (location == 0 || location == 65536)
                    {
                        sb.AppendLine($"hitLocation {i}: NONE/INVALID");
                        continue;
                    }

                    ChassisLocations chassisLoc =
                        MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)location);
                    sb.AppendLine($"hitLocation {i}: {chassisLoc} ({location})");
                }

                Main.Logger?.Info.Log(sb.ToString());
            }
            catch (Exception e)
            {
                Main.Logger?.Error.Log(e);
            }
        }
    }
}
