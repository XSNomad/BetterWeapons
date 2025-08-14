namespace BetterWeapons
{
    public class Settings
    {
        // Core / meta
        public bool Enabled { get; set; } = true;
        public string Description => "Core configuration for BetterWeapons. Controls damage variance, distance-based falloff, heat behavior, jamming, and logging.";
        public bool GeneratedSettingsFilesReadonly { get; set; } = true;
        public string GeneratedSettingsFilesReadonlyDescription => "If true, generated last and default settings files are set to readonly, to indicate that those are not intended to be edited.";
        public BetterLogSettings BetterLog { get; set; } = new();

        // --- Simple variance ---
        public const string SimpleVariance_Comment = "If true, enables per-shot damage variance using a simple model.";
        public bool SimpleVariance { get; set; } = true;

        public const string DamageVarianceDefault_Comment = "Base damage variance as a fraction. Example: 0.25 = ±25% band before distribution shaping.";
        public float DamageVarianceDefault { get; set; } = 0.25f;

        // IMPORTANT: this is a FRACTION (0.0–1.0). 0.75 = 75%.
        public const string StandardDeviationPercentOfSimpleVariance_Comment = "Standard deviation as a FRACTION of the simple variance band. Example: 0.75 means σ = 0.75 × DamageVarianceDefault.";
        public float StandardDeviationPercentOfSimpleVariance { get; set; } = 0.75f;

        // --- Optional clustering tag (not used by the current clustered patch, but kept for compatibility) ---
        public const string ClusterTag_Comment = "WeaponDef tag that marks a weapon as clustered for variance handling.";
        public string ClusterTag { get; set; } = "cluster_weapon";

        // --- Logging ---
        public const string Debug_Comment = "Enable verbose debug logging.";
        public bool Debug { get; set; } = true;

        // --- Distance-based variance (drop-off toward max range) ---
        public const string DistanceBasedVariance_Comment = "If true, applies distance-based damage falloff for supported weapon families.";
        public bool DistanceBasedVariance { get; set; } = true;

        // Generic floor fallback at/beyond max range (FRACTION 0..1). Used if weapon family not matched.
        public const string DistanceBasedVarianceMaxRangeDamageMultiplier_Comment = "Fallback floor multiplier at max range (FRACTION). Example: 0.10 = 10% of base.";
        public float DistanceBasedVarianceMaxRangeDamageMultiplier { get; set; } = 0.10f;

        // Family floors at max range (FRACTIONS 0..1)
        public const string AC_DistanceMultiplierDefault_Comment = "Default floor for Autocannons at max range (FRACTION).";
        public float AC_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string COIL_DistanceMultiplierDefault_Comment = "Default floor for COIL at max range (FRACTION).";
        public float COIL_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string Flamer_DistanceMultiplierDefault_Comment = "Default floor for Flamer at max range (FRACTION).";
        public float Flamer_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string Gauss_DistanceMultiplierDefault_Comment = "Default floor for Gauss at max range (FRACTION).";
        public float Gauss_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string Laser_DistanceMultiplierDefault_Comment = "Default floor for Laser at max range (FRACTION).";
        public float Laser_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string SRM_DistanceMultiplierDefault_Comment = "Default floor for SRM at max range (FRACTION).";
        public float SRM_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string MG_DistanceMultiplierDefault_Comment = "Default floor for Machine Gun at max range (FRACTION).";
        public float MG_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string PPC_DistanceMultiplierDefault_Comment = "Default floor for PPC at max range (FRACTION).";
        public float PPC_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string LBX_DistanceMultiplierDefault_Comment = "Default floor for LB-X AC at max range (FRACTION).";
        public float LBX_DistanceMultiplierDefault { get; set; } = 0.10f;

        public const string SnubPPC_DistanceMultiplierDefault_Comment = "Default floor for Snub PPC at max range (FRACTION).";
        public float SnubPPC_DistanceMultiplierDefault { get; set; } = 0.10f;

        // --- Reverse distance (e.g., LRMs: weak at MinRange, stronger toward MaxRange) ---
        public const string ReverseDistanceBasedVariance_Comment = "If true, reduces damage near min range and scales up toward max range (e.g., for LRMs).";
        public bool ReverseDistanceBasedVariance { get; set; } = true;

        public const string ReverseDistanceBasedVarianceMinRangeDamageMultiplier_Comment = "Floor multiplier near MinRange for reverse variance (FRACTION).";
        public float ReverseDistanceBasedVarianceMinRangeDamageMultiplier { get; set; } = 0.10f;

        public const string LRM_DistanceMultiplierDefault_Comment = "Default floor for LRMs (reverse variance) (FRACTION).";
        public float LRM_DistanceMultiplierDefault { get; set; } = 0.10f;

        // --- Heat as normal damage to non-’Mech targets ---
        public const string HeatDamageAppliesToBuildingAsNormalDamage_Comment = "If true, heat damage also applies as normal damage to buildings.";
        public bool HeatDamageAppliesToBuildingAsNormalDamage { get; set; } = true;

        public const string HeatDamageAppliesToVehicleAsNormalDamage_Comment = "If true, heat damage also applies as normal damage to vehicles.";
        public bool HeatDamageAppliesToVehicleAsNormalDamage { get; set; } = true;

        public const string HeatDamageAppliesToTurretAsNormalDamage_Comment = "If true, heat damage also applies as normal damage to turrets.";
        public bool HeatDamageAppliesToTurretAsNormalDamage { get; set; } = true;

        public const string HeatDamageApplicationToBuildingMultiplier_Comment = "Scalar for converting heat into normal damage for buildings.";
        public float HeatDamageApplicationToBuildingMultiplier { get; set; } = 1.5f;

        public const string HeatDamageApplicationToVehicleMultiplier_Comment = "Scalar for converting heat into normal damage for vehicles.";
        public float HeatDamageApplicationToVehicleMultiplier { get; set; } = 1.5f;

        public const string HeatDamageApplicationToTurretMultiplier_Comment = "Scalar for converting heat into normal damage for turrets.";
        public float HeatDamageApplicationToTurretMultiplier { get; set; } = 1.5f;

        // --- Overheat modifier ---
        public const string OverheatModifier_Comment = "If true, weapon overheat modifiers are applied.";
        public bool OverheatModifier { get; set; } = true;

        // --- Jamming ---
        public const string Jamming_Comment = "Enable weapon jamming mechanics.";
        public bool Jamming { get; set; } = true;

        public const string JamChanceMultiplier_Comment = "Global jam chance multiplier. 1.0 keeps authored rates; >1.0 increases jam chance; <1.0 reduces.";
        public float JamChanceMultiplier { get; set; } = 1.0f;
    }
}
