using System.Collections.Generic;
using UnityEngine;

namespace AutoCamera
{
    public class CompositionEvaluator
    {
        private readonly List<WeightedScorer> _coreScorers = new List<WeightedScorer>();
        private readonly List<WeightedScorer> _auxiliaryScorers = new List<WeightedScorer>();

        public CompositionEvaluator(
            ScenePresetMode presetMode,
            PresentationBoundsProvider boundsProvider)
        {
            _coreScorers.Add(new WeightedScorer(new RuleOfThirdsScorer(), w => w.ruleOfThirds));
            _coreScorers.Add(new WeightedScorer(new FillRatioScorer(boundsProvider), w => w.fillRatio));
            _coreScorers.Add(new WeightedScorer(new BalanceScorer(), w => w.balance));
            _coreScorers.Add(new WeightedScorer(new DepthLayersScorer(), w => w.depthLayers));

            _auxiliaryScorers.Add(new WeightedScorer(new ScreenSeparationScorer(), v => v.separationInfluence));
            _auxiliaryScorers.Add(new WeightedScorer(new ElevationPreferenceScorer(), v => v.elevationInfluence));
            _auxiliaryScorers.Add(new WeightedScorer(new TopDownPitchScorer(), v => v.topDownPitchInfluence));
            _auxiliaryScorers.Add(new WeightedScorer(new DistanceCompactnessScorer(presetMode, boundsProvider), v => v.distanceInfluence));
            _auxiliaryScorers.Add(new WeightedScorer(new PlanarDirectionMatchScorer(presetMode), v => v.planarDirectionInfluence));
            _auxiliaryScorers.Add(new WeightedScorer(new FocusVisibilityScorer(presetMode), v => v.focusVisibilityInfluence));
            _auxiliaryScorers.Add(new WeightedScorer(new PriorityVisibilityScorer(presetMode), v => v.priorityVisibilityInfluence));
            _auxiliaryScorers.Add(new WeightedScorer(new FacadeProminenceScorer(presetMode), v => v.facadeInfluence));
            _auxiliaryScorers.Add(new WeightedScorer(new ViewportLayoutScorer(presetMode, boundsProvider), v => v.layoutInfluence));
        }

        public float Evaluate(
            SearchContext context,
            Vector3 cameraPosition,
            Vector3 focusPoint,
            float fov,
            SearchVariant variant,
            out CompositionDetail details)
        {
            float thirds = _coreScorers[0].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float fill = _coreScorers[1].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float balance = _coreScorers[2].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float depth = _coreScorers[3].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);

            float separation = _auxiliaryScorers[0].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float elevationPreference = _auxiliaryScorers[1].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float topDownPitch = _auxiliaryScorers[2].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float distanceCompactness = _auxiliaryScorers[3].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float planarDirectionMatch = _auxiliaryScorers[4].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float focusVisibility = _auxiliaryScorers[5].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float priorityVisibility = _auxiliaryScorers[6].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float facadeProminence = _auxiliaryScorers[7].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);
            float viewportLayout = _auxiliaryScorers[8].Scorer.Score(context, cameraPosition, focusPoint, fov, variant);

            details = new CompositionDetail
            {
                ruleOfThirds = thirds,
                fillRatio = fill,
                balance = balance,
                depthLayers = depth,
                screenSeparation = separation,
                elevationPreference = elevationPreference,
                topDownPitch = topDownPitch,
                distanceCompactness = distanceCompactness,
                planarDirectionMatch = planarDirectionMatch,
                focusVisibility = focusVisibility,
                priorityVisibility = priorityVisibility,
                facadeProminence = facadeProminence,
                viewportLayout = viewportLayout,
                minimumMetric = Mathf.Min(Mathf.Min(thirds, fill), Mathf.Min(balance, depth))
            };

            CompositionWeights weights = variant.weights;
            float baseScore =
                thirds * weights.ruleOfThirds +
                fill * weights.fillRatio +
                balance * weights.balance +
                depth * weights.depthLayers;

            return baseScore
                 + separation * variant.separationInfluence
                 + elevationPreference * variant.elevationInfluence
                 + topDownPitch * variant.topDownPitchInfluence
                 + distanceCompactness * variant.distanceInfluence
                 + planarDirectionMatch * variant.planarDirectionInfluence
                 + focusVisibility * variant.focusVisibilityInfluence
                 + priorityVisibility * variant.priorityVisibilityInfluence
                 + facadeProminence * variant.facadeInfluence
                 + viewportLayout * variant.layoutInfluence;
        }

        public struct WeightedScorer
        {
            public ICompositionScorer Scorer;
            public System.Func<CompositionWeights, float> CoreWeightSelector;
            public System.Func<SearchVariant, float> AuxInfluenceSelector;

            public WeightedScorer(ICompositionScorer scorer, System.Func<CompositionWeights, float> weightSelector)
            {
                Scorer = scorer;
                CoreWeightSelector = weightSelector;
                AuxInfluenceSelector = null;
            }

            public WeightedScorer(ICompositionScorer scorer, System.Func<SearchVariant, float> influenceSelector)
            {
                Scorer = scorer;
                CoreWeightSelector = null;
                AuxInfluenceSelector = influenceSelector;
            }
        }
    }
}
