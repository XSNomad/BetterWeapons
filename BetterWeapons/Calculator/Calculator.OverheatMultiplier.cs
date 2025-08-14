using BattleTech;
using UnityEngine;

namespace BetterWeapons
{
    static partial class Calculator
    {
        public static class OverheatMultiplier
        {
            public static bool IsApplicable(Weapon weapon)
            {
                return weapon != null &&
                       Main.settings.OverheatModifier &&
                       Mathf.Abs(weapon.weaponDef?.OverheatedDamageMultiplier ?? 0f) > Epsilon;
            }

            public static float Calculate(AbstractActor attacker, ICombatant target, Weapon weapon, float rawDamage)
            {
                // Skip entirely if not applicable
                if (!IsApplicable(weapon))
                    return rawDamage;

                float rawMultiplier = weapon.weaponDef.OverheatedDamageMultiplier;
                bool appliesToAttacker = rawMultiplier < 0f;
                float multiplier = Mathf.Abs(rawMultiplier);

                var effectActor = appliesToAttacker ? attacker : target;
                float damage = (effectActor is Mech mech && mech.IsOverheated)
                    ? rawDamage * multiplier
                    : rawDamage;

                if (Main.settings.Debug)
                {
                    Main.Logger?.Info.Log(
                        $"[OverheatMultiplier] rawMultiplier={rawMultiplier}, " +
                        $"effectActor={(appliesToAttacker ? "attacker" : "target")}, " +
                        $"multiplier={multiplier}, rawDamage={rawDamage}, finalDamage={damage}"
                    );
                }

                return damage;
            }
        }
    }
}
