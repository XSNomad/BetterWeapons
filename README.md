Here’s a GitHub-ready **README.md** you can drop in. It’s structured, skimmable, and includes everything we changed—plus clear credit to the original author.

---

# Better Weapons

> A modern revival of **BT-WeaponRealizer** for HBS BATTLETECH.
> Restores and improves weapon behaviors: per-shot variance, distance shaping, jamming, overheat interactions, and more.

**Credits**

* Original concept & code: **Joel Meador** (“Weapon Realizer”)
* Prior public release & maintenance: **janxious** (BT-WeaponRealizer)
* Revival & modernization: **Better Weapons** maintainers

---

## Table of Contents

* [Features](#features)
* [Installation](#installation)
* [Configuration (Settingsjson)](#configuration-settingsjson)
* [Weapon Tags (content authors)](#weapon-tags-content-authors)
* [How It Works (math, order, and design)](#how-it-works-math-order-and-design)
* [Compatibility & Conflicts](#compatibility--conflicts)
* [Troubleshooting](#troubleshooting)
* [Changelog (revival highlights)](#changelog-revival-highlights)
* [Contributing](#contributing)
* [License](#license)

---

## Features

* **Per-shot damage variance** (Gaussian, truncated to safe bounds).
* **Distance-based shaping** of damage:

  * **DBV**: smooth drop-off toward MaxRange (bounded between a floor and 1.0).
  * **RDBV**: smooth increase from MinRange → MaxRange (great for LRMs).
* **Overheat interaction** via `OverheatedDamageMultiplier` (attacker/target aware).
* **Heat as Normal Damage** vs **vehicles / turrets / buildings** (configurable multipliers).
* **Heat-derived bonus damage** (new) via tags (does not change actual heat).
* **Weapon jamming** with per-weapon multipliers, jam removal checks, and UI marker.
* **Clustered impacts**: **LB-X** and **Snub PPC** are coded directly.

  > The old `cluster` tag is **not used** anymore (tags for distance/jam/heat still work).

**Performance:** All per-def data is cached; lazy precompute auto-runs on first use even if the DataManager wasn’t ready at init. Logging is guarded by a `Debug` flag.

---

## Installation

1. **Download** the latest release (BetterWeapons.zip) from this repo’s **Releases** page.
2. **Extract** the `BetterWeapons/` folder into:

   ```
   ...\BATTLETECH\Mods\
   ```
3. Launch the game once. The mod will generate:

   * `Settings.defaults.json` (effective defaults)
   * `Settings.last.json` (snapshot of what loaded)
4. (Optional) Create or edit `Settings.json` to customize behavior.

**Log file:** `Mods\BetterWeapons\BetterWeapons.txt`
Set `"Debug": true` in `Settings.json` for verbose logs.

**Requirements:** ModTek loader, .NET Framework target v4.7.2 (matches the bundled `.csproj`).

---

## Configuration (Settings.json)

All “multiplier” values below are **fractions** from **0.0 ⇒ 1.0** (e.g., `0.10` = 10%).
`StandardDeviationPercentOfSimpleVariance` is also a **fraction** (`0.75` = 75%).

```json
{
  "Debug": true,

  "SimpleVariance": true,
  "DamageVarianceDefault": 0.25,
  "StandardDeviationPercentOfSimpleVariance": 0.75,

  "DistanceBasedVariance": true,
  "DistanceBasedVarianceMaxRangeDamageMultiplier": 0.10,

  "AC_DistanceMultiplierDefault": 0.10,
  "COIL_DistanceMultiplierDefault": 0.10,
  "Flamer_DistanceMultiplierDefault": 0.10,
  "Gauss_DistanceMultiplierDefault": 0.10,
  "Laser_DistanceMultiplierDefault": 0.10,
  "SRM_DistanceMultiplierDefault": 0.10,
  "MG_DistanceMultiplierDefault": 0.10,
  "PPC_DistanceMultiplierDefault": 0.10,
  "LBX_DistanceMultiplierDefault": 0.10,
  "SnubPPC_DistanceMultiplierDefault": 0.10,

  "ReverseDistanceBasedVariance": true,
  "ReverseDistanceBasedVarianceMinRangeDamageMultiplier": 0.10,
  "LRM_DistanceMultiplierDefault": 0.10,

  "OverheatModifier": true,

  "HeatDamageAppliesToVehicleAsNormalDamage": true,
  "HeatDamageAppliesToTurretAsNormalDamage": true,
  "HeatDamageAppliesToBuildingAsNormalDamage": true,
  "HeatDamageApplicationToVehicleMultiplier": 1.5,
  "HeatDamageApplicationToTurretMultiplier": 1.5,
  "HeatDamageApplicationToBuildingMultiplier": 1.5,

  "Jamming": true,
  "JamChanceMultiplier": 1.0
}
```

> The mod also writes `Settings.defaults.json` and `Settings.last.json`. If `GeneratedSettingsFilesReadonly` is enabled, those may be marked read-only to discourage editing—always edit **Settings.json**.

---

## Weapon Tags (content authors)

Place these in a `WeaponDef`’s `ComponentTags`. All are **optional**.

### Distance shaping (DBV / RDBV)

* `wr-variance_by_distance`
  `wr-variance_by_distance-<X>`
  *DBV (drop-off toward max range).*
  `X` is **slope/rate** (float). Higher means the curve reaches 1.0 sooner (less drop-off).
  The **floor** is taken from settings by family (e.g., `Laser_DistanceMultiplierDefault`).

* `wr-reverse_variance_by_distance`
  `wr-reverse_variance_by_distance-<X>`
  *RDBV (reduced near MinRange, grows toward MaxRange—great for LRMs).*
  `X` is slope/rate (float). Floor usually `LRM_DistanceMultiplierDefault`.

> The distance curves are bounded **floor → 1.0** using a normalized atan shape.

### Heat-derived bonus normal damage (new)

Adds **extra normal damage** proportional to `HeatDamagePerShot` (does **not** change actual heat application):

* `wr-heat_damage_mult-<X>` — always
* `wr-heat_damage_mult_attacker-<X>` — only if **attacker** is overheated
* `wr-heat_damage_mult_target-<X>` — only if **target (Mech)** is overheated

`X` is a float. Example: `0.5` adds `0.5 × heatPerShot` as extra normal damage (scaled per-shot like variance).

### Jamming

* `wr-jammable_weapon` — makes **non-AC** weapons eligible to jam (ACs are already jammable).
* `wr-jam_chance_multiplier-<X>` — per-weapon jam multiplier (float). Defaults to `JamChanceMultiplier` if absent.
* `wr-damage_when_jam` — jam **damages/destroys** the weapon instead of only disabling it.

**UI:** Jammed weapons display `" (JAM)"` in their name until cleared.

### Clustered fire

* **No tag needed** for **LB-X** and **Snub PPC**. Cluster behavior is hard-coded.

  > The old `cluster` tag is **deprecated and ignored** in this mod.

---

## How It Works (math, order, and design)

### Damage pipeline (order)

1. **SimpleVariance**: roll a Gaussian inside `[dps−variance, dps+variance]`.
   Apply per-shot scaling **once**: `adjustment = rawDamage / DamagePerShot`.

2. **DistanceBasedVariance (DBV)**:
   Smooth, normalized atan curve from **floor → 1.0** as you move **close → max range** (drop-off).

3. **ReverseDistanceBasedVariance (RDBV)**:
   Smooth, normalized atan curve from **floor → 1.0** as you move **min → max range** (e.g., LRMs).

4. **OverheatMultiplier**: uses `OverheatedDamageMultiplier` on weapon; applies if attacker/target overheated.

5. **HeatDamageModifier (tag-driven, optional)**: adds bonus **normal** damage proportional to heat.

6. **HeatAsNormalDamage**: applies weapon heat as **normal damage** vs buildings/vehicles/turrets, with multipliers.

### Distance shapes (bounded & smooth)

```
shaped = atan( (π/2) * slope * r ) / (π/2)           // normalized 0..1
mult   = Lerp(floor, 1.0, shaped)                    // in [floor, 1.0]
```

* **DBV**: `r = (MaxRange − dist) / MaxRange` (clamped)
* **RDBV**: `r = (dist − MinRange) / (MaxRange − MinRange)` (clamped)

**Floors** are per-family settings (fractions like `0.10`), not percents.

---

## Compatibility & Conflicts

* Built for stock HBS BATTLETECH + **ModTek**.
* **Conflicts** with mods that patch the same damage/jamming paths (e.g., earlier **WeaponRealizer**, certain ammo overhauls).
  Check your load order and ModTek conflict lists.

---

## Troubleshooting

* **No logs?** Ensure `"Debug": true` in `Settings.json`. Check `Mods\BetterWeapons\BetterWeapons.txt`.
* **Settings ignored?** Edit **`Settings.json`** (not `defaults`/`last`).
* **Range effects missing?** Verify distance floors are **fractions** (e.g., `0.10`, not `10`).
* **Jams never clear?** Review `wr-jam_chance_multiplier-X` values and pilot Gunnery.
* **Readonly errors saving settings?** Set `"GeneratedSettingsFilesReadonly": false` or let the mod clear RO and rewrite.

---

## Changelog (revival highlights)

* Rewrote distance math (normalized atan, bounded **floor → 1.0**).
* Fixed double scaling (per-shot adjustment happens **only** in variance).
* LB-X & Snub PPC clustered impacts are **built-in**; no cluster tag needed.
* Added **heat-derived bonus damage** via tags.
* Unity RNG for speed; no crypto RNG overhead.
* Lazy precompute with DataManager-ready checks.
* Streamlined settings (fractions, not percents) and robust file I/O.
* Expanded, guardable logging.

---

## Contributing

Issues and PRs are welcome:

* Keep changes focused and well-commented.
* Include before/after logs where relevant (enable `Debug`).
* For tag or settings additions, update this README and provide sane defaults.

---

## License

This project credits and builds upon the ideas/code of the original **Weapon Realizer** by **Joel Meador** and subsequent work by **janxious**.
See **LICENSE** in this repository for the current license terms of **Better Weapons**. If you are the original author/maintainer and have questions about attribution or licensing, please open an issue.
