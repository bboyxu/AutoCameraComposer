using UnityEngine;

namespace AutoCamera
{
    public class FocusVisibilityScorer : ICompositionScorer
    {
        private readonly ScenePresetMode _presetMode;

        public FocusVisibilityScorer(ScenePresetMode presetMode)
        {
            _presetMode = presetMode;
        }

        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            if (_presetMode != ScenePresetMode.BuildingBlock && _presetMode != ScenePresetMode.EquipmentGroup)
            {
                return 1f;
            }

            if (!CameraMath.TryProjectBoundsToViewport(context, context.focusBounds, cameraPosition, focusPoint, fov, out Rect focusRect))
            {
                return 0f;
            }

            Vector3 forward = (focusPoint - cameraPosition).normalized;
            float focusDepth = Vector3.Dot(context.focusCenter - cameraPosition, forward);
            float overlapArea = 0f;
            float focusArea = Mathf.Max(0.0001f, focusRect.width * focusRect.height);

            for (int i = 0; i < context.separationBounds.Count; i++)
            {
                Bounds candidate = context.separationBounds[i];

                if (CameraMath.IsLikelyPartOfFocusSubject(candidate, context.focusBounds))
                {
                    continue;
                }

                float candidateDepth = Vector3.Dot(candidate.center - cameraPosition, forward);
                if (candidateDepth >= focusDepth)
                {
                    continue;
                }

                if (!CameraMath.TryProjectBoundsToViewport(context, candidate, cameraPosition, focusPoint, fov, out Rect candidateRect))
                {
                    continue;
                }

                overlapArea += CameraMath.RectOverlapArea(focusRect, candidateRect);
            }

            float overlapRatio = overlapArea / focusArea;
            return Mathf.Clamp01(1f - overlapRatio * 2.2f);
        }
    }
}
