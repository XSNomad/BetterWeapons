using BattleTech;

namespace BetterWeapons
{
    static partial class Calculator
    {
        public static class HeatAsNormalDamage
        {
            public static bool IsApplicable(Weapon weapon)
            {
                // Only run if at least one target class is enabled AND weapon actually does heat
                return (Main.settings.HeatDamageAppliesToBuildingAsNormalDamage ||
                        Main.settings.HeatDamageAppliesToVehicleAsNormalDamage ||
                        Main.settings.HeatDamageAppliesToTurretAsNormalDamage)
                       && weapon != null
                       && weapon.HeatDamagePerShot > Epsilon;
            }

            public static float Calculate(ICombatant target, Weapon weapon, float currentDamage, float rawDamage)
            {
                // Determine which multiplier applies based on target type and settings
                float? targetMult = null;

                if (target is Building && Main.settings.HeatDamageAppliesToBuildingAsNormalDamage)
                    targetMult = Main.settings.HeatDamageApplicationToBuildingMultiplier;
                else if (target is Vehicle && Main.settings.HeatDamageAppliesToVehicleAsNormalDamage)
                    targetMult = Main.settings.HeatDamageApplicationToVehicleMultiplier;
                else if (target is Turret && Main.settings.HeatDamageAppliesToTurretAsNormalDamage)
                    targetMult = Main.settings.HeatDamageApplicationToTurretMultiplier;

                if (targetMult == null)
                    return currentDamage; // not a target class we convert heat for

                // Guard for odd weapon defs
                var dps = weapon.DamagePerShot;
                var adjustment = (dps > 0f) ? (rawDamage / dps) : 1f;

                var extra = adjustment * targetMult.Value * weapon.HeatDamagePerShot;
                var finalDamage = currentDamage + extra;

                if (Main.settings.Debug)
                {
                    Main.Logger?.Info.Log(
                        $"[HeatAsNormal] target={target?.GetType().Name} defId={weapon.defId} " +
                        $"dps={dps} heatPerShot={weapon.HeatDamagePerShot} adj={adjustment} mult={targetMult} " +
                        $"extra={extra} -> total={finalDamage}"
                    );
                }

                return finalDamage;
            }
        }
    }
}
