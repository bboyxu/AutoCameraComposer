namespace AutoCamera
{
    public interface IScenePreset
    {
        ScenePresetMode PresetMode { get; }
        CompositionStyle Style { get; }
        SearchQuality Quality { get; }
        bool LivePreview { get; }
        bool AutoRepair { get; }
        int MaxRepairPasses { get; }
        float TransitionDuration { get; }
        MetricThresholds Thresholds { get; }
        CompositionWeights Weights { get; }
        StyleSettings StyleSettings { get; }
        PresetRuleSettings RuleSettings { get; }
    }
}
