using BattleTech;
using Harmony;
using Localize;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BetterWeapons
{
    // ---------- Small helper ----------
    internal static class StatisticHelper
    {
        internal static Statistic GetOrCreateStatisic<T>(StatCollection collection, string statName, T defaultValue)
        {
            var s = collection.GetStatistic(statName);
            return s ?? collection.AddStatistic(statName, defaultValue);
        }
    }

    // ---------- UI: add " (JAM)" to weapon name when jammed ----------
    [HarmonyPatch(typeof(MechComponent), "UIName", MethodType.Getter)]
    static class JammedWeaponDisplayChanger
    {
        public static bool Prepare() => Main.settings.Jamming;

        public static void Postfix(MechComponent __instance, ref Text __result)
        {
            if (!__instance.IsFunctional) return;
            if (__instance is not Weapon) return;

            if (!StatisticHelper
                    .GetOrCreateStatisic(__instance.StatCollection, JammingEnabler.JammedWeaponStatisticName, false)
                    .Value<bool>())
                return;

            // Avoid extra allocs
            __result.Append(" (JAM)", Array.Empty<object>());
        }
    }

    // ---------- Core jamming system ----------
    [HarmonyPatch(typeof(AbstractActor), "OnActivationEnd", MethodType.Normal)]
    internal static class JammingEnabler
    {
        // Constants / tags
        private const float Epsilon = 0.0001f;

        private const string TemporarilyDisabledStatisticName = "TemporarilyDisabled";
        internal const string JammedWeaponStatisticName = "WR-JammedWeapon";

        private const string JammableWeaponTag = "wr-jammable_weapon";
        private const string DamageableWeaponTag = "wr-damage_when_jam";
        private const string JamMultiplierTagPrefix = "wr-jam_chance_multiplier";
        public static readonly char[] TagDelimiter = { '-' };

        // Caches (read-mostly after Precompute)
        public static readonly Dictionary<string, bool> JammableWeapons = new(512);
        public static readonly Dictionary<string, bool> DamageableWeapons = new(512);
        public static readonly Dictionary<string, float> JamMultipliers = new(512);

        /// Call this once after settings load when DataManager is available.
        public static void EnsurePrecomputed()
        {
            if (JammableWeapons.Count == 0)
            {
                if (Main.settings.Debug)
                    Main.Logger?.Info.Log("[Jamming] Cache empty — attempting Precompute() now.");

                Precompute();
            }
        }

        public static void Precompute()
        {
            JammableWeapons.Clear();
            DamageableWeapons.Clear();
            JamMultipliers.Clear();

            var dm = UnityGameInstance.BattleTechGame?.DataManager;
            var defs = dm?.WeaponDefs;
            if (defs == null)
            {
                if (Main.settings.Debug) Main.Logger?.Warning.Log("[Jamming] DataManager not ready; skipping precompute.");
                return;
            }

            foreach (var kvp in defs)
            {
                string defId = kvp.Key ?? string.Empty;
                var def = kvp.Value;
                var tags = def.ComponentTags;

                bool jammable = def.Type == WeaponType.Autocannon || tags.Contains(JammableWeaponTag);
                bool damageable = tags.Contains(DamageableWeaponTag);

                // Default multiplier; allow tag override like: "wr-jam_chance_multiplier-1.5"
                float mult = Main.settings.JamChanceMultiplier;

                var overrideTag = tags.FirstOrDefault(t =>
                    t.StartsWith(JamMultiplierTagPrefix, StringComparison.InvariantCultureIgnoreCase));

                if (!string.IsNullOrEmpty(overrideTag) &&
                    !overrideTag.Equals(JamMultiplierTagPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    var parts = overrideTag.Split(TagDelimiter, 3);
                    var last = parts[parts.Length - 1];
                    if (float.TryParse(last, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                        mult = parsed;
                    else if (Main.settings.Debug)
                        Main.Logger?.Warning.Log($"[Jamming] Bad multiplier tag '{overrideTag}' on '{defId}', using {mult}");
                }

                bool effectivelyJammable = jammable && mult > Epsilon;

                JammableWeapons[defId] = effectivelyJammable;
                DamageableWeapons[defId] = damageable;
                JamMultipliers[defId] = mult;
            }

            if (Main.settings.Debug)
                Main.Logger?.Info.Log($"[Jamming] Precomputed: {JammableWeapons.Count} weapons");
        }

        public static bool Prepare() => Main.settings.Jamming;

        // Runs at end of actor activation
        public static void Prefix(AbstractActor __instance)
        {
            EnsurePrecomputed();
            var actor = __instance;
            if (actor == null || actor.IsShutDown) return;

            var weapons = actor.Weapons;
            for (int i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];
                if (weapon == null || !IsJammable(weapon)) continue;

                if (IsJammed(weapon))
                {
                    bool removed = AttemptToRemoveJam(actor, weapon);
                    if (Main.settings.Debug)
                        Main.Logger?.Info.Log($"[Jamming] Removed? {removed} ({weapon.defId})");
                }
                else if (weapon.roundsSinceLastFire == 0) // fired this round
                {
                    bool added = AttemptToAddJam(actor, weapon);
                    if (Main.settings.Debug)
                        Main.Logger?.Info.Log($"[Jamming] Added? {added} ({weapon.defId})");
                }
            }
        }

        // Jam logic
        public static float GetRefireModifier(Weapon weapon) =>
            (weapon.RefireModifier > 0f && weapon.roundsSinceLastFire < 2) ? weapon.RefireModifier : 0f;

        public static bool AttemptToAddJam(AbstractActor actor, Weapon weapon)
        {
            float refire = GetRefireModifier(weapon);
            float mult = GetJamMultiplier(weapon);

            // Use UnityEngine.Random with intended bounds:
            // Want 1..100 → Range(1, 101); want 2..10 → Range(2, 11)
            int roll = UnityEngine.Random.Range(1, 101);  // 1..100
            int mitigation = UnityEngine.Random.Range(2, 11);   // 2..10
            int gunnery = actor.SkillGunnery;

            if (Main.settings.Debug)
                Main.Logger?.Info.Log($"[JamAdd] def={weapon.defId} dmg={weapon.DamageLevel} refire={refire} roll={roll} mult={mult} gunnery={gunnery} mit={mitigation}");

            // No jam if roll beats threshold or pilot mitigates
            if (roll >= refire * mult) return false;
            if (gunnery >= mitigation) return false;

            AddJam(actor, weapon);
            return true;
        }

        public static bool AttemptToRemoveJam(AbstractActor actor, Weapon weapon)
        {
            // Want 1..9 as before → Range(1, 10)
            int mitigation = UnityEngine.Random.Range(1, 10);   // 1..9
            int gunnery = actor.SkillGunnery;

            if (Main.settings.Debug)
                Main.Logger?.Info.Log($"[JamRemove] def={weapon.defId} gunnery={gunnery} mit={mitigation}");

            if (gunnery >= mitigation)
            {
                RemoveJam(actor, weapon);
                return true;
            }
            return false;
        }

        // State helpers
        public static bool IsJammed(Weapon weapon)
        {
            var s = StatisticHelper.GetOrCreateStatisic(weapon.StatCollection, JammedWeaponStatisticName, false);
            return s.Value<bool>();
        }

        public static bool IsJammable(Weapon weapon)
        {
            EnsurePrecomputed();

            return weapon != null &&
                   !string.IsNullOrEmpty(weapon.defId) &&
                   JammableWeapons.TryGetValue(weapon.defId, out var v) && v;
        }


        public static bool DamagesWhenJams(Weapon weapon) =>
            weapon != null &&
            !string.IsNullOrEmpty(weapon.defId) &&
            DamageableWeapons.TryGetValue(weapon.defId, out var v) && v;

        public static float GetJamMultiplier(Weapon weapon) =>
            (weapon != null && !string.IsNullOrEmpty(weapon.defId) && JamMultipliers.TryGetValue(weapon.defId, out var m))
                ? m : 0f;

        // Effects
        public static void AddJam(AbstractActor actor, Weapon weapon)
        {
            if (!DamagesWhenJams(weapon))
            {
                weapon.StatCollection.Set(JammedWeaponStatisticName, true);
                weapon.StatCollection.Set(TemporarilyDisabledStatisticName, true);

                actor.Combat.MessageCenter.PublishMessage(
                    new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(actor, $"{weapon.Name} Jammed!",
                            FloatieMessage.MessageNature.Debuff, true)));
            }
            else
            {
                bool destroying = weapon.DamageLevel != ComponentDamageLevel.Functional;
                var level = destroying ? ComponentDamageLevel.Destroyed : ComponentDamageLevel.Penalized;

                var fakeHit = new WeaponHitInfo(
                    -1, -1, -1, -1,
                    weapon.parent.GUID, weapon.parent.GUID, 1,
                    null, null, null, null, null, null, null, null, null, null, null);

                weapon.DamageComponent(fakeHit, level, true);

                string msg = destroying ? $"{weapon.Name} misfire: Destroyed!"
                                        : $"{weapon.Name} misfire: Damaged!";
                actor.Combat.MessageCenter.PublishMessage(
                    new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(actor, msg, FloatieMessage.MessageNature.Debuff, true)));
            }
        }

        public static void RemoveJam(AbstractActor actor, Weapon weapon)
        {
            weapon.StatCollection.Set(JammedWeaponStatisticName, false);
            weapon.StatCollection.Set(TemporarilyDisabledStatisticName, false);

            actor.Combat.MessageCenter.PublishMessage(
                new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(actor, $"{weapon.Name} Unjammed!",
                        FloatieMessage.MessageNature.Buff, true)));
        }
    }
}
