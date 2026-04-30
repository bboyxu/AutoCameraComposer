namespace AutoCamera
{
    public class BuildingBlockPreset : IScenePreset
    {
        public ScenePresetMode PresetMode => ScenePresetMode.BuildingBlock;
        public CompositionStyle Style => CompositionStyle.Balanced;
        public SearchQuality Quality => SearchQuality.High;
        public bool LivePreview => true;
        public bool AutoRepair => true;
        public int MaxRepairPasses => 4;
        public float TransitionDuration => 0.7f;

        public MetricThresholds Thresholds => new MetricThresholds
        {
            overall = 0.80f,
            ruleOfThirds = 0.74f,
            fillRatio = 0.78f,
            balance = 0.80f,
            depthLayers = 0.60f
        };

        public CompositionWeights Weights => new CompositionWeights
        {
            ruleOfThirds = 0.25f,
            fillRatio = 0.25f,
            balance = 0.20f,
            depthLayers = 0.30f
        };

        public StyleSettings StyleSettings => new StyleSettings
        {
            baseFill = 0.70f,
            basePadding = 1.15f,
            minElevation = 12f,
            maxElevation = 60f,
            minFov = 24f,
            maxFov = 56f,
            focusOffsetScale = 0.18f,
            preferredElevation = 34f,
            elevationInfluence = 0.12f,
            separationInfluence = 0.18f
        };

        public PresetRuleSettings RuleSettings => new PresetRuleSettings
        {
            focusVisibilityInfluence = 0.34f,
            priorityVisibilityInfluence = 0.44f,
            facadeInfluence = 0.48f,
            layoutInfluence = 0.38f,
            planarDirectionInfluence = 0.06f,
            distanceInfluence = 0.42f,
            preferredDistanceScale = 1.48f,
            preferredElevationBias = 0f,
            minElevationBias = 0f,
            maxElevationBias = 0f,
            enforcedMinElevation = 14f,
            enforcedMaxElevation = 28f,
            topDownPitchInfluence = 0.38f,
            preferredTopDownPitch = 20f,
            elevationCurveExponent = 1.12f
        };
    }
}
