namespace AutoCamera
{
    public class EquipmentGroupPreset : IScenePreset
    {
        public ScenePresetMode PresetMode => ScenePresetMode.EquipmentGroup;
        public CompositionStyle Style => CompositionStyle.HeroProduct;
        public SearchQuality Quality => SearchQuality.High;
        public bool LivePreview => true;
        public bool AutoRepair => true;
        public int MaxRepairPasses => 4;
        public float TransitionDuration => 0.5f;

        public MetricThresholds Thresholds => new MetricThresholds
        {
            overall = 0.84f,
            ruleOfThirds = 0.76f,
            fillRatio = 0.88f,
            balance = 0.76f,
            depthLayers = 0.52f
        };

        public CompositionWeights Weights => new CompositionWeights
        {
            ruleOfThirds = 0.30f,
            fillRatio = 0.26f,
            balance = 0.16f,
            depthLayers = 0.28f
        };

        public StyleSettings StyleSettings => new StyleSettings
        {
            baseFill = 0.76f,
            basePadding = 1.08f,
            minElevation = 8f,
            maxElevation = 34f,
            minFov = 20f,
            maxFov = 46f,
            focusOffsetScale = 0.20f,
            preferredElevation = 22f,
            elevationInfluence = 0.10f,
            separationInfluence = 0.14f
        };

        public PresetRuleSettings RuleSettings => new PresetRuleSettings
        {
            focusVisibilityInfluence = 0.12f,
            priorityVisibilityInfluence = 0.38f,
            facadeInfluence = 0.02f,
            layoutInfluence = 0.08f,
            planarDirectionInfluence = 0.10f,
            distanceInfluence = 0.28f,
            preferredDistanceScale = 0.76f,
            preferredElevationBias = 12f,
            minElevationBias = 10f,
            maxElevationBias = 10f,
            enforcedMinElevation = 34f,
            enforcedMaxElevation = 58f,
            topDownPitchInfluence = 0.40f,
            preferredTopDownPitch = 45f,
            elevationCurveExponent = 1.3f
        };
    }
}
