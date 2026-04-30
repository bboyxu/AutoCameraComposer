namespace AutoCamera
{
    public class InteriorZonePreset : IScenePreset
    {
        public ScenePresetMode PresetMode => ScenePresetMode.InteriorZone;
        public CompositionStyle Style => CompositionStyle.TopDown;
        public SearchQuality Quality => SearchQuality.High;
        public bool LivePreview => true;
        public bool AutoRepair => true;
        public int MaxRepairPasses => 4;
        public float TransitionDuration => 0.6f;

        public MetricThresholds Thresholds => new MetricThresholds
        {
            overall = 0.80f,
            ruleOfThirds = 0.72f,
            fillRatio = 0.82f,
            balance = 0.78f,
            depthLayers = 0.46f
        };

        public CompositionWeights Weights => new CompositionWeights
        {
            ruleOfThirds = 0.20f,
            fillRatio = 0.24f,
            balance = 0.24f,
            depthLayers = 0.32f
        };

        public StyleSettings StyleSettings => new StyleSettings
        {
            baseFill = 0.68f,
            basePadding = 1.15f,
            minElevation = 48f,
            maxElevation = 82f,
            minFov = 24f,
            maxFov = 58f,
            focusOffsetScale = 0.12f,
            preferredElevation = 68f,
            elevationInfluence = 0.28f,
            separationInfluence = 0.12f
        };

        public PresetRuleSettings RuleSettings => new PresetRuleSettings
        {
            focusVisibilityInfluence = 0.10f,
            priorityVisibilityInfluence = 0.08f,
            facadeInfluence = 0.04f,
            layoutInfluence = 0.12f,
            planarDirectionInfluence = 0.34f,
            distanceInfluence = 0.28f,
            preferredDistanceScale = 0.76f,
            preferredElevationBias = 8f,
            minElevationBias = 4f,
            maxElevationBias = 6f,
            enforcedMinElevation = 62f,
            enforcedMaxElevation = 89f,
            topDownPitchInfluence = 0.42f,
            preferredTopDownPitch = 72f,
            elevationCurveExponent = 2.1f
        };
    }
}
