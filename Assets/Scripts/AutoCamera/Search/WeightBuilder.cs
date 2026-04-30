using UnityEngine;

namespace AutoCamera
{
    public class WeightBuilder
    {
        private readonly ScenePresetMode _presetMode;
        private readonly MetricThresholds _thresholds;
        private readonly RandomAestheticProfile? _randomProfile;

        public WeightBuilder(
            ScenePresetMode presetMode,
            MetricThresholds thresholds,
            RandomAestheticProfile? randomProfile = null)
        {
            _presetMode = presetMode;
            _thresholds = thresholds;
            _randomProfile = randomProfile;
        }

        public CompositionWeights BuildBaseWeights(CompositionStyle style)
        {
            if (_presetMode == ScenePresetMode.RandomAesthetic && _randomProfile.HasValue)
            {
                return _randomProfile.Value.weights;
            }

            IScenePreset preset = PresetRegistry.GetPreset(_presetMode);
            if (preset != null)
            {
                return preset.Weights;
            }

            switch (style)
            {
                case CompositionStyle.HeroProduct:
                    return new CompositionWeights { ruleOfThirds = 0.30f, fillRatio = 0.26f, balance = 0.16f, depthLayers = 0.28f };
                case CompositionStyle.WideScene:
                    return new CompositionWeights { ruleOfThirds = 0.20f, fillRatio = 0.32f, balance = 0.18f, depthLayers = 0.30f };
                case CompositionStyle.TopDown:
                    return new CompositionWeights { ruleOfThirds = 0.20f, fillRatio = 0.24f, balance = 0.24f, depthLayers = 0.32f };
                default:
                    return new CompositionWeights { ruleOfThirds = 0.25f, fillRatio = 0.25f, balance = 0.20f, depthLayers = 0.30f };
            }
        }

        public CompositionWeights BuildAdaptiveWeights(CompositionWeights baseWeights, CompositionDetail previousBest, int repairPass)
        {
            if (repairPass == 0)
            {
                baseWeights.Normalize();
                return baseWeights;
            }

            float boost = 0.45f + repairPass * 0.15f;
            baseWeights.ruleOfThirds += Mathf.Max(0f, _thresholds.ruleOfThirds - previousBest.ruleOfThirds) * boost;
            baseWeights.fillRatio += Mathf.Max(0f, _thresholds.fillRatio - previousBest.fillRatio) * boost;
            baseWeights.balance += Mathf.Max(0f, _thresholds.balance - previousBest.balance) * boost;
            baseWeights.depthLayers += Mathf.Max(0f, _thresholds.depthLayers - previousBest.depthLayers) * boost;
            baseWeights.Normalize();
            return baseWeights;
        }
    }
}
