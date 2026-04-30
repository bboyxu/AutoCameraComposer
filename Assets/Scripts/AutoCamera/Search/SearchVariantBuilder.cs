using System.Collections.Generic;
using UnityEngine;

namespace AutoCamera
{
    public class SearchVariantBuilder
    {
        private readonly ScenePresetMode _presetMode;
        private readonly CompositionStyle _compositionStyle;
        private readonly SearchQuality _searchQuality;
        private readonly RandomAestheticProfile? _randomProfile;

        public SearchVariantBuilder(
            ScenePresetMode presetMode,
            CompositionStyle compositionStyle,
            SearchQuality searchQuality,
            RandomAestheticProfile? randomProfile = null)
        {
            _presetMode = presetMode;
            _compositionStyle = compositionStyle;
            _searchQuality = searchQuality;
            _randomProfile = randomProfile;
        }

        public List<SearchVariant> Build(CompositionWeights weights, int repairPass)
        {
            float baseFill;
            float basePadding;
            float baseMinElevation;
            float baseMaxElevation;
            float baseMinFov;
            float baseMaxFov;
            float baseFocusOffset;
            float preferredElevation;
            float elevationInfluence;
            float separationInfluence;
            float focusVisibilityInfluence;
            float priorityVisibilityInfluence;
            float facadeInfluence;
            float layoutInfluence;
            float planarDirectionInfluence;
            float distanceInfluence;
            float preferredDistanceScale;
            float preferredElevationBias;
            float minElevationBias;
            float maxElevationBias;
            float enforcedMinElevation;
            float enforcedMaxElevation;
            float topDownPitchInfluence;
            float preferredTopDownPitch;
            float elevationCurveExponent;

            if (_randomProfile.HasValue)
            {
                RandomAestheticProfile profile = _randomProfile.Value;
                baseFill = profile.baseFill;
                basePadding = profile.basePadding;
                baseMinElevation = profile.minElevation;
                baseMaxElevation = profile.maxElevation;
                baseMinFov = profile.minFov;
                baseMaxFov = profile.maxFov;
                baseFocusOffset = profile.focusOffsetScale;
                preferredElevation = profile.preferredElevation;
                elevationInfluence = profile.elevationInfluence;
                separationInfluence = profile.separationInfluence;
                focusVisibilityInfluence = profile.focusVisibilityInfluence;
                priorityVisibilityInfluence = profile.priorityVisibilityInfluence;
                facadeInfluence = profile.facadeInfluence;
                layoutInfluence = profile.layoutInfluence;
                planarDirectionInfluence = profile.planarDirectionInfluence;
                distanceInfluence = profile.distanceInfluence;
                preferredDistanceScale = profile.preferredDistanceScale;
                preferredElevationBias = profile.preferredElevationBias;
                minElevationBias = profile.minElevationBias;
                maxElevationBias = profile.maxElevationBias;
                enforcedMinElevation = profile.enforcedMinElevation;
                enforcedMaxElevation = profile.enforcedMaxElevation;
                topDownPitchInfluence = profile.topDownPitchInfluence;
                preferredTopDownPitch = profile.preferredTopDownPitch;
                elevationCurveExponent = profile.elevationCurveExponent;
            }
            else
            {
                IScenePreset preset = PresetRegistry.GetPreset(_presetMode);
                StyleSettings ss = preset != null ? preset.StyleSettings : GetDefaultStyleSettings();
                PresetRuleSettings rs = preset != null ? preset.RuleSettings : GetDefaultRuleSettings();

                baseFill = ss.baseFill;
                basePadding = ss.basePadding;
                baseMinElevation = ss.minElevation;
                baseMaxElevation = ss.maxElevation;
                baseMinFov = ss.minFov;
                baseMaxFov = ss.maxFov;
                baseFocusOffset = ss.focusOffsetScale;
                preferredElevation = ss.preferredElevation;
                elevationInfluence = ss.elevationInfluence;
                separationInfluence = ss.separationInfluence;
                focusVisibilityInfluence = rs.focusVisibilityInfluence;
                priorityVisibilityInfluence = rs.priorityVisibilityInfluence;
                facadeInfluence = rs.facadeInfluence;
                layoutInfluence = rs.layoutInfluence;
                planarDirectionInfluence = rs.planarDirectionInfluence;
                distanceInfluence = rs.distanceInfluence;
                preferredDistanceScale = rs.preferredDistanceScale;
                preferredElevationBias = rs.preferredElevationBias;
                minElevationBias = rs.minElevationBias;
                maxElevationBias = rs.maxElevationBias;
                enforcedMinElevation = rs.enforcedMinElevation;
                enforcedMaxElevation = rs.enforcedMaxElevation;
                topDownPitchInfluence = rs.topDownPitchInfluence;
                preferredTopDownPitch = rs.preferredTopDownPitch;
                elevationCurveExponent = rs.elevationCurveExponent;
            }

            QualitySampleSettings quality = GetQualitySamples(_searchQuality);

            int horizontalSamples = quality.horizontalSamples + repairPass * 24;
            int verticalRings = quality.verticalRings + repairPass;

            float elevationExpand = repairPass * 8f;
            float fovExpand = repairPass * 6f;
            float fillShift = 0.05f + repairPass * 0.03f;
            float paddingShift = 0.10f + repairPass * 0.05f;
            float focusShift = repairPass * 0.04f;

            float[] fillCandidates =
            {
                Mathf.Clamp(baseFill, 0.48f, 0.88f),
                Mathf.Clamp(baseFill - fillShift, 0.48f, 0.88f),
                Mathf.Clamp(baseFill + fillShift, 0.48f, 0.88f)
            };

            float[] paddingCandidates =
            {
                Mathf.Clamp(basePadding, 0.95f, 1.75f),
                Mathf.Clamp(basePadding - paddingShift, 0.95f, 1.75f),
                Mathf.Clamp(basePadding + paddingShift, 0.95f, 1.75f)
            };

            List<SearchVariant> variants = new List<SearchVariant>();
            for (int i = 0; i < fillCandidates.Length; i++)
            {
                for (int j = 0; j < paddingCandidates.Length; j++)
                {
                    SearchVariant variant = new SearchVariant
                    {
                        minElevation = Mathf.Clamp(baseMinElevation + minElevationBias - elevationExpand, 5f, 85f),
                        maxElevation = Mathf.Clamp(baseMaxElevation + maxElevationBias + elevationExpand, 10f, 85f),
                        minFov = Mathf.Clamp(baseMinFov - fovExpand, 10f, 70f),
                        maxFov = Mathf.Clamp(baseMaxFov + fovExpand, 20f, 90f),
                        fillTarget = fillCandidates[i],
                        padding = paddingCandidates[j],
                        focusOffsetScale = Mathf.Clamp(baseFocusOffset + focusShift, 0.08f, 0.35f),
                        weights = weights,
                        horizontalSamples = horizontalSamples,
                        verticalRings = verticalRings,
                        repairPass = repairPass,
                        preferredElevation = Mathf.Clamp(preferredElevation + preferredElevationBias + repairPass * 4f, 5f, 85f),
                        elevationInfluence = elevationInfluence,
                        separationInfluence = separationInfluence,
                        focusVisibilityInfluence = focusVisibilityInfluence,
                        priorityVisibilityInfluence = priorityVisibilityInfluence,
                        facadeInfluence = facadeInfluence,
                        layoutInfluence = layoutInfluence,
                        planarDirectionInfluence = planarDirectionInfluence,
                        distanceInfluence = distanceInfluence,
                        preferredDistanceScale = preferredDistanceScale,
                        enforcedMinElevation = enforcedMinElevation,
                        enforcedMaxElevation = enforcedMaxElevation,
                        topDownPitchInfluence = topDownPitchInfluence,
                        preferredTopDownPitch = preferredTopDownPitch,
                        elevationCurveExponent = elevationCurveExponent
                    };

                    if (_presetMode == ScenePresetMode.BuildingBlock)
                    {
                        variant.fillTarget = Mathf.Clamp(variant.fillTarget - 0.10f, 0.48f, 0.82f);
                        variant.padding = Mathf.Clamp(variant.padding + 0.14f, 0.95f, 1.90f);
                        variant.maxFov = Mathf.Min(variant.maxFov, 52f);
                    }

                    if (variant.enforcedMinElevation > 0f)
                    {
                        variant.minElevation = Mathf.Max(variant.minElevation, variant.enforcedMinElevation);
                    }

                    if (variant.enforcedMaxElevation > 0f)
                    {
                        variant.maxElevation = Mathf.Min(variant.maxElevation, variant.enforcedMaxElevation);
                    }

                    if (variant.maxElevation < variant.minElevation)
                    {
                        float temp = variant.maxElevation;
                        variant.maxElevation = variant.minElevation;
                        variant.minElevation = temp;
                    }

                    if (variant.maxFov < variant.minFov)
                    {
                        float temp = variant.maxFov;
                        variant.maxFov = variant.minFov;
                        variant.minFov = temp;
                    }

                    variants.Add(variant);
                }
            }

            return variants;
        }

        private static QualitySampleSettings GetQualitySamples(SearchQuality quality)
        {
            switch (quality)
            {
                case SearchQuality.Draft:
                    return new QualitySampleSettings { horizontalSamples = 36, verticalRings = 3 };
                case SearchQuality.High:
                    return new QualitySampleSettings { horizontalSamples = 120, verticalRings = 6 };
                default:
                    return new QualitySampleSettings { horizontalSamples = 72, verticalRings = 4 };
            }
        }

        private static StyleSettings GetDefaultStyleSettings()
        {
            return new StyleSettings
            {
                baseFill = 0.70f, basePadding = 1.15f, minElevation = 12f, maxElevation = 60f,
                minFov = 24f, maxFov = 56f, focusOffsetScale = 0.18f, preferredElevation = 34f,
                elevationInfluence = 0.12f, separationInfluence = 0.18f
            };
        }

        private static PresetRuleSettings GetDefaultRuleSettings()
        {
            return new PresetRuleSettings
            {
                focusVisibilityInfluence = 0.10f, priorityVisibilityInfluence = 0.06f, facadeInfluence = 0.02f,
                layoutInfluence = 0.08f, planarDirectionInfluence = 0.04f, distanceInfluence = 0.10f,
                preferredDistanceScale = 1.00f, preferredElevationBias = 0f, minElevationBias = 0f,
                maxElevationBias = 0f, enforcedMinElevation = 0f, enforcedMaxElevation = 0f,
                topDownPitchInfluence = 0f, preferredTopDownPitch = 0f, elevationCurveExponent = 1f
            };
        }
    }
}
