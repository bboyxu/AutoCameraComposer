using UnityEngine;

namespace AutoCamera
{
    public class PriorityVisibilityScorer : ICompositionScorer
    {
        private readonly ScenePresetMode _presetMode;

        public PriorityVisibilityScorer(ScenePresetMode presetMode)
        {
            _presetMode = presetMode;
        }

        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            if (context.priorityBounds == null || context.priorityBounds.Count == 0)
            {
                return 1f;
            }

            if (_presetMode == ScenePresetMode.Exterior)
            {
                return ScoreExterior(context, cameraPosition, focusPoint, fov);
            }

            if ((_presetMode != ScenePresetMode.BuildingBlock && _presetMode != ScenePresetMode.EquipmentGroup)
                || context.priorityBounds.Count <= 1)
            {
                return 1f;
            }

            if (_presetMode == ScenePresetMode.EquipmentGroup)
            {
                return ScoreEquipmentGroup(context, cameraPosition, focusPoint, fov);
            }

            if (_presetMode == ScenePresetMode.BuildingBlock)
            {
                return ScoreBuildingBlock(context, cameraPosition, focusPoint, fov);
            }

            return ScoreWeighted(context, cameraPosition, focusPoint, fov);
        }

        private float ScoreExterior(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov)
        {
            int exteriorCount = Mathf.Min(5, context.priorityBounds.Count);
            float visibilitySum = 0f;
            float minVisibility = 1f;

            for (int i = 0; i < exteriorCount; i++)
            {
                float subjectVisibility = ScoreSingleSubjectVisibility(context, context.priorityBounds[i], cameraPosition, focusPoint, fov);
                visibilitySum += subjectVisibility;
                minVisibility = Mathf.Min(minVisibility, subjectVisibility);
            }

            float averageVisibility = visibilitySum / Mathf.Max(1, exteriorCount);
            return minVisibility * 0.82f + averageVisibility * 0.18f;
        }

        private float ScoreEquipmentGroup(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov)
        {
            float visibilitySum = 0f;
            float minVisibility = 1f;

            for (int i = 0; i < context.priorityBounds.Count; i++)
            {
                float subjectVisibility = ScoreSingleSubjectVisibility(context, context.priorityBounds[i], cameraPosition, focusPoint, fov);
                visibilitySum += subjectVisibility;
                minVisibility = Mathf.Min(minVisibility, subjectVisibility);
            }

            float averageVisibility = visibilitySum / context.priorityBounds.Count;
            return minVisibility * 0.72f + averageVisibility * 0.28f;
        }

        private float ScoreBuildingBlock(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov)
        {
            int highValueCount = Mathf.Min(2, context.priorityBounds.Count);
            float highVisibilitySum = 0f;
            float highMinVisibility = 1f;

            for (int i = 0; i < highValueCount; i++)
            {
                float subjectVisibility = ScoreSingleSubjectVisibility(context, context.priorityBounds[i], cameraPosition, focusPoint, fov);
                highVisibilitySum += subjectVisibility;
                highMinVisibility = Mathf.Min(highMinVisibility, subjectVisibility);
            }

            float highAverageVisibility = highVisibilitySum / Mathf.Max(1, highValueCount);

            if (context.priorityBounds.Count <= 2)
            {
                return highMinVisibility * 0.62f + highAverageVisibility * 0.38f;
            }

            float remainingVisibilitySum = 0f;
            int remainingCount = 0;
            for (int i = 2; i < context.priorityBounds.Count; i++)
            {
                remainingVisibilitySum += ScoreSingleSubjectVisibility(context, context.priorityBounds[i], cameraPosition, focusPoint, fov);
                remainingCount++;
            }

            float remainingAverageVisibility = remainingVisibilitySum / Mathf.Max(1, remainingCount);
            return highMinVisibility * 0.55f + highAverageVisibility * 0.30f + remainingAverageVisibility * 0.15f;
        }

        private float ScoreWeighted(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov)
        {
            float weightedScore = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < context.priorityBounds.Count; i++)
            {
                float weight = 1f / (i + 1f);
                float subjectVisibility = ScoreSingleSubjectVisibility(context, context.priorityBounds[i], cameraPosition, focusPoint, fov);
                weightedScore += subjectVisibility * weight;
                totalWeight += weight;
            }

            if (totalWeight <= Mathf.Epsilon)
            {
                return 1f;
            }

            return weightedScore / totalWeight;
        }

        private float ScoreSingleSubjectVisibility(SearchContext context, Bounds subjectBounds, Vector3 cameraPosition, Vector3 focusPoint, float fov)
        {
            if (!CameraMath.TryProjectBoundsToViewport(context, subjectBounds, cameraPosition, focusPoint, fov, out Rect subjectRect))
            {
                return 0f;
            }

            Vector3 forward = (focusPoint - cameraPosition).normalized;
            float subjectDepth = Vector3.Dot(subjectBounds.center - cameraPosition, forward);
            float overlapArea = 0f;
            float subjectArea = Mathf.Max(0.0001f, subjectRect.width * subjectRect.height);

            for (int i = 0; i < context.priorityBounds.Count; i++)
            {
                Bounds candidate = context.priorityBounds[i];
                if (CameraMath.ApproximatelySameBounds(candidate, subjectBounds))
                {
                    continue;
                }

                float candidateDepth = Vector3.Dot(candidate.center - cameraPosition, forward);
                if (candidateDepth >= subjectDepth)
                {
                    continue;
                }

                if (!CameraMath.TryProjectBoundsToViewport(context, candidate, cameraPosition, focusPoint, fov, out Rect candidateRect))
                {
                    continue;
                }

                overlapArea += CameraMath.RectOverlapArea(subjectRect, candidateRect);
            }

            float overlapRatio = overlapArea / subjectArea;
            return Mathf.Clamp01(1f - overlapRatio * 2.0f);
        }
    }
}
