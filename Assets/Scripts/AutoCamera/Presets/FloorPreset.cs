namespace AutoCamera
{
    public class FloorPreset : IScenePreset
    {
        public ScenePresetMode PresetMode => ScenePresetMode.Floor;
        public CompositionStyle Style => CompositionStyle.TopDown;
        public SearchQuality Quality => SearchQuality.High;
        public bool LivePreview => true;
        public bool AutoRepair => true;
        public int MaxRepairPasses => 3;
        public float TransitionDuration => 0.6f;

        public MetricThresholds Thresholds => new MetricThresholds
        {
            overall = 0.81f,
            ruleOfThirds = 0.68f,
            fillRatio = 0.84f,
            balance = 0.84f,
            depthLayers = 0.40f
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
            focusVisibilityInfluence = 0.08f,
            priorityVisibilityInfluence = 0.08f,
            facadeInfluence = 0.02f,
            layoutInfluence = 0.10f,
            planarDirectionInfluence = 0.30f,
            distanceInfluence = 0.34f,
            preferredDistanceScale = 0.92f,
            preferredElevationBias = 22f,
            minElevationBias = 14f,
            maxElevationBias = 8f,
            enforcedMinElevation = 76f,
            enforcedMaxElevation = 89f,
            topDownPitchInfluence = 0.56f,
            preferredTopDownPitch = 86f,
            elevationCurveExponent = 2.6f
        };
    }
}
