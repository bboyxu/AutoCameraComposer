using System;
using UnityEngine;

namespace AutoCamera
{
    public class RandomAestheticProfileBuilder
    {
        private int _seedVersion;

        public RandomAestheticProfileBuilder()
        {
            _seedVersion = 0;
        }

        public void IncrementSeedVersion()
        {
            _seedVersion++;
        }

        public int SeedVersion => _seedVersion;

        public RandomAestheticProfile Build(SearchContext context)
        {
            float horizontalSpan = Mathf.Max(context.bounds.size.x, context.bounds.size.z);
            float verticalSpan = Mathf.Max(context.bounds.size.y, 0.01f);
            float fullVolume = Mathf.Max(context.bounds.size.x * context.bounds.size.y * context.bounds.size.z, 0.01f);
            float focusVolume = Mathf.Max(context.focusBounds.size.x * context.focusBounds.size.y * context.focusBounds.size.z, 0.01f);
            float focusDominance = Mathf.Clamp01(focusVolume / fullVolume);
            float flatness = 1f - Mathf.Clamp01(verticalSpan / Mathf.Max(horizontalSpan, 0.01f));
            float campusSpread = Mathf.Clamp01(horizontalSpan / Mathf.Max(verticalSpan * 2.2f, 0.01f));
            int targetCount = context.priorityBounds != null ? context.priorityBounds.Count : 1;
            float targetCountFactor = Mathf.Clamp01((targetCount - 1f) / 10f);

            float renderVolumeSum = 0f;
            if (context.renderers != null)
            {
                for (int i = 0; i < context.renderers.Count; i++)
                {
                    Bounds b = context.renderers[i].bounds;
                    renderVolumeSum += Mathf.Max(0.0001f, b.size.x * b.size.y * b.size.z);
                }
            }
            float density = Mathf.Clamp01(renderVolumeSum / Mathf.Max(fullVolume * 2.4f, 0.01f));
            float focusHeightRatio = Mathf.Clamp01(context.focusBounds.size.y / Mathf.Max(context.bounds.size.y, 0.01f));
            float focusFootprintRatio = Mathf.Clamp01(
                (context.focusBounds.size.x * context.focusBounds.size.z) /
                Mathf.Max(context.bounds.size.x * context.bounds.size.z, 0.01f));
            float towerLike = Mathf.Clamp01(focusHeightRatio * 1.20f - focusFootprintRatio * 0.35f);

            float exteriorRaw = Mathf.Max(0.20f, 0.34f + campusSpread * 0.42f + targetCountFactor * 0.28f);
            float buildingRaw = Mathf.Max(0.20f, 0.30f + towerLike * 0.50f + focusDominance * 0.24f);
            float floorRaw = Mathf.Max(0.12f, 0.18f + flatness * 0.35f * (1f - focusDominance * 0.40f));
            float interiorRaw = Mathf.Max(0.10f, 0.16f + flatness * 0.24f + density * 0.26f);
            float equipmentRaw = Mathf.Max(0.10f, 0.14f + (1f - targetCountFactor) * 0.20f + (1f - campusSpread) * 0.18f);

            float rawSum = exteriorRaw + buildingRaw + floorRaw + interiorRaw + equipmentRaw;
            float exteriorWeight = exteriorRaw / rawSum;
            float buildingWeight = buildingRaw / rawSum;
            float floorWeight = floorRaw / rawSum;
            float interiorWeight = interiorRaw / rawSum;
            float equipmentWeight = equipmentRaw / rawSum;

            BlendedParams blended = new BlendedParams();
            AccumulateArchetype(ScenePresetMode.Exterior, CompositionStyle.WideScene, exteriorWeight, ref blended);
            AccumulateArchetype(ScenePresetMode.BuildingBlock, CompositionStyle.Balanced, buildingWeight, ref blended);
            AccumulateArchetype(ScenePresetMode.Floor, CompositionStyle.TopDown, floorWeight, ref blended);
            AccumulateArchetype(ScenePresetMode.InteriorZone, CompositionStyle.TopDown, interiorWeight, ref blended);
            AccumulateArchetype(ScenePresetMode.EquipmentGroup, CompositionStyle.HeroProduct, equipmentWeight, ref blended);

            blended.Weights.Normalize();

            int seed = 17;
            seed = seed * 31 + Mathf.RoundToInt(context.bounds.center.x * 100f);
            seed = seed * 31 + Mathf.RoundToInt(context.bounds.center.y * 100f);
            seed = seed * 31 + Mathf.RoundToInt(context.bounds.center.z * 100f);
            seed = seed * 31 + Mathf.RoundToInt(context.bounds.size.x * 10f);
            seed = seed * 31 + Mathf.RoundToInt(context.bounds.size.y * 10f);
            seed = seed * 31 + Mathf.RoundToInt(context.bounds.size.z * 10f);
            seed = seed * 31 + targetCount;
            seed = seed * 31 + _seedVersion;
            System.Random stableRandom = new System.Random(seed);
            float Jitter(float amplitude) => ((float)stableRandom.NextDouble() * 2f - 1f) * amplitude;

            int styleVariant = Mathf.Abs(_seedVersion) % 5;
            VariantShifts shifts = GetVariantShifts(styleVariant);

            blended.Weights.ruleOfThirds = Mathf.Max(0.01f, blended.Weights.ruleOfThirds + Jitter(0.08f) + shifts.thirdsShift);
            blended.Weights.fillRatio = Mathf.Max(0.01f, blended.Weights.fillRatio + Jitter(0.08f));
            blended.Weights.balance = Mathf.Max(0.01f, blended.Weights.balance + Jitter(0.08f) + shifts.balanceShift);
            blended.Weights.depthLayers = Mathf.Max(0.01f, blended.Weights.depthLayers + Jitter(0.08f));
            blended.Weights.Normalize();

            float blendedPitch = Mathf.Clamp(blended.PreferredTopDownPitch + shifts.pitchShift + Jitter(5.5f), 16f, 66f);
            float pitchWindow = Mathf.Lerp(12f, 18f, flatness);
            float blendedMinElevation = Mathf.Clamp(Mathf.Min(blended.MinElevation, blendedPitch - pitchWindow * 0.45f) + Jitter(2.8f), 8f, 66f);
            float blendedMaxElevation = Mathf.Clamp(Mathf.Max(blended.MaxElevation * 0.55f + blendedPitch * 0.45f, blendedPitch + pitchWindow) + Jitter(2.8f), blendedMinElevation + 8f, 82f);
            float blendedEnforcedMinElevation = Mathf.Clamp(Mathf.Lerp(blendedMinElevation, blended.EnforcedMinElevation, 0.35f), 8f, 66f);
            float blendedEnforcedMaxElevation = Mathf.Clamp(Mathf.Lerp(blendedMaxElevation, blended.EnforcedMaxElevation, 0.35f), blendedEnforcedMinElevation + 8f, 84f);

            return new RandomAestheticProfile
            {
                weights = blended.Weights,
                baseFill = Mathf.Clamp(blended.BaseFill + shifts.fillShift + Jitter(0.07f), 0.52f, 0.88f),
                basePadding = Mathf.Clamp(blended.BasePadding + shifts.paddingShift + Jitter(0.10f), 0.92f, 1.56f),
                minElevation = blendedMinElevation,
                maxElevation = blendedMaxElevation,
                minFov = Mathf.Clamp(blended.MinFov + shifts.fovShift + Jitter(3.2f), 15f, 40f),
                maxFov = Mathf.Clamp(blended.MaxFov + shifts.fovShift + Jitter(4.5f), 32f, 70f),
                focusOffsetScale = Mathf.Clamp(blended.FocusOffsetScale + shifts.focusOffsetShift + Jitter(0.05f), 0.08f, 0.34f),
                preferredElevation = Mathf.Clamp(blended.PreferredElevation + Jitter(4.8f), 14f, 66f),
                elevationInfluence = Mathf.Clamp(blended.ElevationInfluence + Jitter(0.05f), 0.10f, 0.38f),
                separationInfluence = Mathf.Clamp(blended.SeparationInfluence + Jitter(0.05f), 0.12f, 0.42f),
                focusVisibilityInfluence = Mathf.Clamp(blended.FocusVisibilityInfluence + Jitter(0.04f), 0.06f, 0.32f),
                priorityVisibilityInfluence = Mathf.Clamp(blended.PriorityVisibilityInfluence + Jitter(0.05f), 0.08f, 0.46f),
                facadeInfluence = Mathf.Clamp(blended.FacadeInfluence + shifts.facadeShift + Jitter(0.05f), 0.01f, 0.46f),
                layoutInfluence = Mathf.Clamp(blended.LayoutInfluence + shifts.layoutShift + Jitter(0.05f), 0.08f, 0.54f),
                planarDirectionInfluence = Mathf.Clamp(blended.PlanarDirectionInfluence + shifts.planarShift + Jitter(0.05f), 0.04f, 0.44f),
                distanceInfluence = Mathf.Clamp(blended.DistanceInfluence + Jitter(0.05f), 0.12f, 0.48f),
                preferredDistanceScale = Mathf.Clamp(blended.PreferredDistanceScale + shifts.distanceShift + Jitter(0.10f), 0.76f, 1.72f),
                preferredElevationBias = Mathf.Clamp(blended.PreferredElevationBias + Jitter(3.5f), -6f, 28f),
                minElevationBias = Mathf.Clamp(blended.MinElevationBias + Jitter(2.8f), -6f, 22f),
                maxElevationBias = Mathf.Clamp(blended.MaxElevationBias + Jitter(2.8f), -6f, 22f),
                enforcedMinElevation = blendedEnforcedMinElevation,
                enforcedMaxElevation = blendedEnforcedMaxElevation,
                topDownPitchInfluence = Mathf.Clamp(blended.TopDownPitchInfluence + Jitter(0.05f), 0.08f, 0.54f),
                preferredTopDownPitch = blendedPitch,
                elevationCurveExponent = Mathf.Clamp(blended.ElevationCurveExponent + Jitter(0.16f), 0.8f, 2.35f)
            };
        }

        private void AccumulateArchetype(ScenePresetMode presetMode, CompositionStyle style, float influence, ref BlendedParams blended)
        {
            IScenePreset preset = PresetRegistry.GetPreset(presetMode);
            if (preset == null) return;

            StyleSettings ss = preset.StyleSettings;
            PresetRuleSettings rs = preset.RuleSettings;

            CompositionWeights styleWeights = preset.Weights;

            blended.Weights.ruleOfThirds += styleWeights.ruleOfThirds * influence;
            blended.Weights.fillRatio += styleWeights.fillRatio * influence;
            blended.Weights.balance += styleWeights.balance * influence;
            blended.Weights.depthLayers += styleWeights.depthLayers * influence;

            blended.BaseFill += ss.baseFill * influence;
            blended.BasePadding += ss.basePadding * influence;
            blended.MinElevation += ss.minElevation * influence;
            blended.MaxElevation += ss.maxElevation * influence;
            blended.MinFov += ss.minFov * influence;
            blended.MaxFov += ss.maxFov * influence;
            blended.FocusOffsetScale += ss.focusOffsetScale * influence;
            blended.PreferredElevation += ss.preferredElevation * influence;
            blended.ElevationInfluence += ss.elevationInfluence * influence;
            blended.SeparationInfluence += ss.separationInfluence * influence;

            blended.FocusVisibilityInfluence += rs.focusVisibilityInfluence * influence;
            blended.PriorityVisibilityInfluence += rs.priorityVisibilityInfluence * influence;
            blended.FacadeInfluence += rs.facadeInfluence * influence;
            blended.LayoutInfluence += rs.layoutInfluence * influence;
            blended.PlanarDirectionInfluence += rs.planarDirectionInfluence * influence;
            blended.DistanceInfluence += rs.distanceInfluence * influence;
            blended.PreferredDistanceScale += rs.preferredDistanceScale * influence;
            blended.PreferredElevationBias += rs.preferredElevationBias * influence;
            blended.MinElevationBias += rs.minElevationBias * influence;
            blended.MaxElevationBias += rs.maxElevationBias * influence;
            blended.EnforcedMinElevation += rs.enforcedMinElevation * influence;
            blended.EnforcedMaxElevation += rs.enforcedMaxElevation * influence;
            blended.TopDownPitchInfluence += rs.topDownPitchInfluence * influence;
            blended.PreferredTopDownPitch += rs.preferredTopDownPitch * influence;
            blended.ElevationCurveExponent += rs.elevationCurveExponent * influence;
        }

        private static VariantShifts GetVariantShifts(int styleVariant)
        {
            switch (styleVariant)
            {
                case 0:
                    return new VariantShifts
                    {
                        pitchShift = 5.5f, distanceShift = 0.20f, fillShift = -0.08f,
                        paddingShift = 0.14f, fovShift = 2.0f, layoutShift = 0.05f
                    };
                case 1:
                    return new VariantShifts
                    {
                        pitchShift = -4.5f, distanceShift = -0.12f, fillShift = 0.09f,
                        paddingShift = -0.08f, fovShift = -2.5f, facadeShift = 0.08f
                    };
                case 2:
                    return new VariantShifts
                    {
                        pitchShift = 8.0f, distanceShift = 0.10f, fillShift = -0.03f,
                        paddingShift = 0.08f, fovShift = 1.0f, planarShift = 0.08f, balanceShift = 0.06f
                    };
                case 3:
                    return new VariantShifts
                    {
                        pitchShift = -1.5f, distanceShift = -0.06f, fillShift = 0.04f,
                        paddingShift = -0.03f, fovShift = 2.8f, focusOffsetShift = 0.06f, thirdsShift = 0.10f
                    };
                default:
                    return new VariantShifts
                    {
                        pitchShift = 2.5f, distanceShift = 0.04f, fillShift = 0.01f,
                        paddingShift = 0.04f, fovShift = 0.6f, layoutShift = 0.03f, balanceShift = 0.04f
                    };
            }
        }

        private struct BlendedParams
        {
            public CompositionWeights Weights;
            public float BaseFill;
            public float BasePadding;
            public float MinElevation;
            public float MaxElevation;
            public float MinFov;
            public float MaxFov;
            public float FocusOffsetScale;
            public float PreferredElevation;
            public float ElevationInfluence;
            public float SeparationInfluence;
            public float FocusVisibilityInfluence;
            public float PriorityVisibilityInfluence;
            public float FacadeInfluence;
            public float LayoutInfluence;
            public float PlanarDirectionInfluence;
            public float DistanceInfluence;
            public float PreferredDistanceScale;
            public float PreferredElevationBias;
            public float MinElevationBias;
            public float MaxElevationBias;
            public float EnforcedMinElevation;
            public float EnforcedMaxElevation;
            public float TopDownPitchInfluence;
            public float PreferredTopDownPitch;
            public float ElevationCurveExponent;
        }

        private struct VariantShifts
        {
            public float pitchShift;
            public float distanceShift;
            public float fillShift;
            public float paddingShift;
            public float fovShift;
            public float focusOffsetShift;
            public float facadeShift;
            public float layoutShift;
            public float planarShift;
            public float thirdsShift;
            public float balanceShift;
        }
    }
}
