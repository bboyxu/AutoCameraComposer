namespace AutoCamera
{
    public class RandomAestheticPreset : IScenePreset
    {
        public ScenePresetMode PresetMode => ScenePresetMode.RandomAesthetic;
        public CompositionStyle Style => CompositionStyle.Balanced;
        public SearchQuality Quality => SearchQuality.High;
        public bool LivePreview => true;
        public bool AutoRepair => true;
        public int MaxRepairPasses => 4;
        public float TransitionDuration => 0.55f;

        public MetricThresholds Thresholds => new MetricThresholds
        {
            overall = 0.82f,
            ruleOfThirds = 0.74f,
            fillRatio = 0.80f,
            balance = 0.76f,
            depthLayers = 0.52f
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
            focusVisibilityInfluence = 0.10f,
            priorityVisibilityInfluence = 0.06f,
            facadeInfluence = 0.02f,
            layoutInfluence = 0.08f,
            planarDirectionInfluence = 0.04f,
            distanceInfluence = 0.10f,
            preferredDistanceScale = 1.00f,
            preferredElevationBias = 0f,
            minElevationBias = 0f,
            maxElevationBias = 0f,
            enforcedMinElevation = 0f,
            enforcedMaxElevation = 0f,
            topDownPitchInfluence = 0f,
            preferredTopDownPitch = 0f,
            elevationCurveExponent = 1f
        };
    }
}
