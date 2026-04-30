using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Search;
using UnityEngine;

namespace AutoCamera
{
    public class ProfessionalSearchStrategy
    {
        private readonly ScenePresetMode _presetMode;
        private readonly CompositionStyle _compositionStyle;
        private readonly SearchQuality _searchQuality;
        private readonly MetricThresholds _thresholds;
        private readonly bool _autoRepair;
        private readonly int _maxRepairPasses;
        private readonly RandomAestheticProfile? _randomProfile;

        private readonly WeightBuilder _weightBuilder;
        private readonly SearchVariantBuilder _variantBuilder;
        private readonly CompositionEvaluator _evaluator;
        private readonly AcceptanceEvaluator _acceptanceEvaluator;
        private readonly PresentationBoundsProvider _boundsProvider;
        private readonly FocusPointBuilder _focusPointBuilder;

        public ProfessionalSearchStrategy(
            ScenePresetMode presetMode,
            CompositionStyle compositionStyle,
            SearchQuality searchQuality,
            MetricThresholds thresholds,
            bool autoRepair,
            int maxRepairPasses,
            RandomAestheticProfile? randomProfile = null)
        {
            _presetMode = presetMode;
            _compositionStyle = compositionStyle;
            _searchQuality = searchQuality;
            _thresholds = thresholds;
            _autoRepair = autoRepair;
            _maxRepairPasses = maxRepairPasses;
            _randomProfile = randomProfile;

            _boundsProvider = new PresentationBoundsProvider(presetMode);
            _weightBuilder = new WeightBuilder(presetMode, thresholds, randomProfile);
            _variantBuilder = new SearchVariantBuilder(presetMode, compositionStyle, searchQuality, randomProfile);
            _evaluator = new CompositionEvaluator(presetMode, _boundsProvider);
            _acceptanceEvaluator = new AcceptanceEvaluator(presetMode, thresholds);
            _focusPointBuilder = new FocusPointBuilder(presetMode, _boundsProvider);
        }

        public CameraPose Search(SearchContext context, CancellationToken token)
        {
            CompositionWeights baseWeights = _weightBuilder.BuildBaseWeights(_compositionStyle);
            baseWeights.Normalize();

            CameraPose globalBest = default;
            globalBest.acceptanceRank = float.MinValue;

            CompositionDetail previousBestDetail = default;
            int repairPassCount = _autoRepair ? Mathf.Max(1, _maxRepairPasses + 1) : 1;

            for (int repairPass = 0; repairPass < repairPassCount; repairPass++)
            {
                token.ThrowIfCancellationRequested();
                CompositionWeights adaptiveWeights = _weightBuilder.BuildAdaptiveWeights(baseWeights, previousBestDetail, repairPass);
                List<SearchVariant> variants = _variantBuilder.Build(adaptiveWeights, repairPass);

                CameraPose passBest = default;
                passBest.acceptanceRank = float.MinValue;

                for (int i = 0; i < variants.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    CameraPose variantBest = SearchVariantCandidates(context, variants[i], token);
                    if (variantBest.acceptanceRank > passBest.acceptanceRank)
                    {
                        passBest = variantBest;
                    }
                }

                if (passBest.acceptanceRank > globalBest.acceptanceRank)
                {
                    globalBest = passBest;
                }

                previousBestDetail = globalBest.details;

                if (_acceptanceEvaluator.PassesAllThresholds(globalBest.score, globalBest.details))
                {
                    break;
                }
            }

            return globalBest;
        }

        private CameraPose SearchVariantCandidates(SearchContext context, SearchVariant variant, CancellationToken token)
        {
            CameraPose bestPose = default;
            bestPose.acceptanceRank = float.MinValue;

            Vector2[] anchors = FocusAnchorBuilder.Build(variant.repairPass, _presetMode);
            Vector3 orbitCenter = _boundsProvider.GetCenter(context);
            Bounds framingBounds = _boundsProvider.GetBounds(context);
            float orbitRadius = _boundsProvider.GetRadius(context);

            ParallelOptions parallelOptions = new ParallelOptions
            {
                CancellationToken = token
            };
            CameraPose[] perHorizontalBest = new CameraPose[variant.horizontalSamples];
            for (int i = 0; i < perHorizontalBest.Length; i++)
            {
                perHorizontalBest[i].acceptanceRank = float.MinValue;
            }

            Parallel.For(0, variant.horizontalSamples, parallelOptions, h =>
            {
                CameraPose localBest = default;
                localBest.acceptanceRank = float.MinValue;

                float theta = (h / (float)variant.horizontalSamples) * Mathf.PI * 2f;

                for (int v = 0; v < variant.verticalRings; v++)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    float phiNorm = variant.verticalRings == 1 ? 0.5f : v / (float)(variant.verticalRings - 1);
                    phiNorm = Mathf.Pow(phiNorm, Mathf.Max(0.5f, variant.elevationCurveExponent));
                    float phi = Mathf.Lerp(variant.minElevation, variant.maxElevation, phiNorm) * Mathf.Deg2Rad;

                    Vector3 direction = new Vector3(
                        Mathf.Cos(phi) * Mathf.Cos(theta),
                        Mathf.Sin(phi),
                        Mathf.Cos(phi) * Mathf.Sin(theta)
                    ).normalized;

                    float distance = CameraMath.EstimateCameraDistance(orbitRadius, variant.padding, variant.minFov, variant.maxFov);
                    Vector3 cameraPosition = orbitCenter + direction * distance;

                    for (int a = 0; a < anchors.Length; a++)
                    {
                        Vector3 focusPoint = _focusPointBuilder.Build(context, direction, anchors[a], variant.focusOffsetScale);
                        float fov = CameraMath.CalculateOptimalFOV(context, framingBounds, cameraPosition, focusPoint, variant.fillTarget);
                        fov = Mathf.Clamp(fov, variant.minFov, variant.maxFov);

                        float score = _evaluator.Evaluate(
                            context,
                            cameraPosition,
                            focusPoint,
                            fov,
                            variant,
                            out CompositionDetail details);

                        float acceptanceRank = _acceptanceEvaluator.CalculateAcceptanceRank(score, details);
                        if (acceptanceRank > localBest.acceptanceRank ||
                            (Mathf.Abs(acceptanceRank - localBest.acceptanceRank) < 0.00001f && score > localBest.score))
                        {
                            localBest.position = cameraPosition;
                            localBest.lookAt = focusPoint;
                            localBest.fov = fov;
                            localBest.score = score;
                            localBest.acceptanceRank = acceptanceRank;
                            localBest.details = details;
                            localBest.weights = variant.weights;
                            localBest.fillTarget = variant.fillTarget;
                            localBest.padding = variant.padding;
                            localBest.repairPass = variant.repairPass;
                        }
                    }
                }

                perHorizontalBest[h] = localBest;
            });

            for (int h = 0; h < perHorizontalBest.Length; h++)
            {
                CameraPose candidate = perHorizontalBest[h];
                if (candidate.acceptanceRank > bestPose.acceptanceRank ||
                    (Mathf.Abs(candidate.acceptanceRank - bestPose.acceptanceRank) < 0.00001f && candidate.score > bestPose.score))
                {
                    bestPose = candidate;
                }
            }

            return bestPose;
        }
    }
}
