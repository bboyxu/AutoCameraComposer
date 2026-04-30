using UnityEngine;

namespace AutoCamera
{
    public class AcceptanceEvaluator
    {
        private readonly ScenePresetMode _presetMode;
        private readonly MetricThresholds _thresholds;

        public AcceptanceEvaluator(ScenePresetMode presetMode, MetricThresholds thresholds)
        {
            _presetMode = presetMode;
            _thresholds = thresholds;
        }

        public float CalculateAcceptanceRank(float overallScore, CompositionDetail details)
        {
            float positiveMargin = 0f;
            float deficit = 0f;
            int passCount = 0;

            ScoreThreshold(overallScore, _thresholds.overall, ref passCount, ref positiveMargin, ref deficit);
            ScoreThreshold(details.ruleOfThirds, _thresholds.ruleOfThirds, ref passCount, ref positiveMargin, ref deficit);
            ScoreThreshold(details.fillRatio, _thresholds.fillRatio, ref passCount, ref positiveMargin, ref deficit);
            ScoreThreshold(details.balance, _thresholds.balance, ref passCount, ref positiveMargin, ref deficit);
            ScoreThreshold(details.depthLayers, _thresholds.depthLayers, ref passCount, ref positiveMargin, ref deficit);

            ApplyPresetSpecificRules(details, ref positiveMargin, ref deficit);

            return passCount * 100f + positiveMargin * 20f - deficit * 30f + details.minimumMetric * 10f + overallScore;
        }

        public bool PassesAllThresholds(float overallScore, CompositionDetail details)
        {
            return overallScore >= _thresholds.overall
                && details.ruleOfThirds >= _thresholds.ruleOfThirds
                && details.fillRatio >= _thresholds.fillRatio
                && details.balance >= _thresholds.balance
                && details.depthLayers >= _thresholds.depthLayers;
        }

        public EvaluationReport BuildEvaluationReport(float overallScore, CompositionDetail details)
        {
            EvaluationReport report = new EvaluationReport
            {
                overallPassed = overallScore >= _thresholds.overall,
                ruleOfThirdsPassed = details.ruleOfThirds >= _thresholds.ruleOfThirds,
                fillRatioPassed = details.fillRatio >= _thresholds.fillRatio,
                balancePassed = details.balance >= _thresholds.balance,
                depthLayersPassed = details.depthLayers >= _thresholds.depthLayers
            };

            report.passedMetricCount =
                (report.overallPassed ? 1 : 0) +
                (report.ruleOfThirdsPassed ? 1 : 0) +
                (report.fillRatioPassed ? 1 : 0) +
                (report.balancePassed ? 1 : 0) +
                (report.depthLayersPassed ? 1 : 0);

            report.allPassed = report.passedMetricCount == 5;
            return report;
        }

        private void ApplyPresetSpecificRules(CompositionDetail details, ref float positiveMargin, ref float deficit)
        {
            if (_presetMode == ScenePresetMode.Floor || _presetMode == ScenePresetMode.InteriorZone)
            {
                float pitchThreshold = _presetMode == ScenePresetMode.Floor ? 0.92f : 0.88f;
                float pitchPenaltyScale = _presetMode == ScenePresetMode.Floor ? 4.5f : 3.8f;
                if (details.topDownPitch >= pitchThreshold)
                {
                    positiveMargin += (details.topDownPitch - pitchThreshold) * 0.5f;
                }
                else
                {
                    deficit += (pitchThreshold - details.topDownPitch) * pitchPenaltyScale;
                }
            }

            if (_presetMode == ScenePresetMode.EquipmentGroup)
            {
                float visibilityThreshold = 0.94f;
                if (details.priorityVisibility >= visibilityThreshold)
                {
                    positiveMargin += (details.priorityVisibility - visibilityThreshold) * 0.5f;
                }
                else
                {
                    deficit += (visibilityThreshold - details.priorityVisibility) * 5.0f;
                }

                float pitchThreshold = 0.70f;
                if (details.topDownPitch >= pitchThreshold)
                {
                    positiveMargin += (details.topDownPitch - pitchThreshold) * 0.4f;
                }
                else
                {
                    deficit += (pitchThreshold - details.topDownPitch) * 3.2f;
                }
            }

            if (_presetMode == ScenePresetMode.Exterior)
            {
                float visibilityThreshold = 0.92f;
                if (details.priorityVisibility >= visibilityThreshold)
                {
                    positiveMargin += (details.priorityVisibility - visibilityThreshold) * 0.45f;
                }
                else
                {
                    deficit += (visibilityThreshold - details.priorityVisibility) * 5.2f;
                }
            }

            if (_presetMode == ScenePresetMode.BuildingBlock)
            {
                ApplyBuildingBlockRules(details, ref positiveMargin, ref deficit);
            }
        }

        private void ApplyBuildingBlockRules(CompositionDetail details, ref float positiveMargin, ref float deficit)
        {
            float priorityVisibilityThreshold = 0.90f;
            if (details.priorityVisibility >= priorityVisibilityThreshold)
            {
                positiveMargin += (details.priorityVisibility - priorityVisibilityThreshold) * 0.60f;
            }
            else
            {
                deficit += (priorityVisibilityThreshold - details.priorityVisibility) * 5.4f;
            }

            float focusVisibilityThreshold = 0.88f;
            if (details.focusVisibility >= focusVisibilityThreshold)
            {
                positiveMargin += (details.focusVisibility - focusVisibilityThreshold) * 0.45f;
            }
            else
            {
                deficit += (focusVisibilityThreshold - details.focusVisibility) * 4.4f;
            }

            float pitchThreshold = 0.84f;
            if (details.topDownPitch >= pitchThreshold)
            {
                positiveMargin += (details.topDownPitch - pitchThreshold) * 0.45f;
            }
            else
            {
                deficit += (pitchThreshold - details.topDownPitch) * 3.8f;
            }

            float facadeThreshold = 0.82f;
            if (details.facadeProminence >= facadeThreshold)
            {
                positiveMargin += (details.facadeProminence - facadeThreshold) * 0.40f;
            }
            else
            {
                deficit += (facadeThreshold - details.facadeProminence) * 3.5f;
            }

            float layoutThreshold = 0.82f;
            if (details.viewportLayout >= layoutThreshold)
            {
                positiveMargin += (details.viewportLayout - layoutThreshold) * 0.40f;
            }
            else
            {
                deficit += (layoutThreshold - details.viewportLayout) * 3.8f;
            }

            float distanceThreshold = 0.84f;
            if (details.distanceCompactness >= distanceThreshold)
            {
                positiveMargin += (details.distanceCompactness - distanceThreshold) * 0.50f;
            }
            else
            {
                deficit += (distanceThreshold - details.distanceCompactness) * 4.6f;
            }
        }

        private static void ScoreThreshold(float value, float threshold, ref int passCount, ref float positiveMargin, ref float deficit)
        {
            if (value >= threshold)
            {
                passCount++;
                positiveMargin += value - threshold;
            }
            else
            {
                deficit += threshold - value;
            }
        }
    }
}
