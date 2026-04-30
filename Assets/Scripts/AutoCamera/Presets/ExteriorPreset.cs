namespace AutoCamera
{
    public class ExteriorPreset : IScenePreset
    {
        public ScenePresetMode PresetMode => ScenePresetMode.Exterior;
        public CompositionStyle Style => CompositionStyle.WideScene;
        public SearchQuality Quality => SearchQuality.High;
        public bool LivePreview => true;
        public bool AutoRepair => true;
        public int MaxRepairPasses => 4;
        public float TransitionDuration => 0.9f;

        public MetricThresholds Thresholds => new MetricThresholds
        {
            overall = 0.80f,
            ruleOfThirds = 0.72f,
            fillRatio = 0.74f,
            balance = 0.80f,
            depthLayers = 0.66f
        };

        public CompositionWeights Weights => new CompositionWeights
        {
            ruleOfThirds = 0.20f,
            fillRatio = 0.32f,
            balance = 0.18f,
            depthLayers = 0.30f
        };

        public StyleSettings StyleSettings => new StyleSettings
        {
            baseFill = 0.62f,
            basePadding = 1.42f,
            minElevation = 18f,
            maxElevation = 42f,
            minFov = 22f,
            maxFov = 52f,
            focusOffsetScale = 0.16f,
            preferredElevation = 30f,
            elevationInfluence = 0.28f,
            separationInfluence = 0.30f
        };

        public PresetRuleSettings RuleSettings => new PresetRuleSettings
        {
            focusVisibilityInfluence = 0.10f,
            priorityVisibilityInfluence = 0.42f,
            facadeInfluence = 0.02f,
            layoutInfluence = 0.50f,
            planarDirectionInfluence = 0.04f,
            distanceInfluence = 0.40f,
            preferredDistanceScale = 1.62f,
            preferredElevationBias = 0f,
            minElevationBias = 0f,
            maxElevationBias = 0f,
            enforcedMinElevation = 22f,
            enforcedMaxElevation = 38f,
            topDownPitchInfluence = 0.26f,
            preferredTopDownPitch = 30f,
            elevationCurveExponent = 1.05f
        };
    }
}
