using UnityEngine;

namespace AutoCamera
{
    public class FacadeProminenceScorer : ICompositionScorer
    {
        private readonly ScenePresetMode _presetMode;

        public FacadeProminenceScorer(ScenePresetMode presetMode)
        {
            _presetMode = presetMode;
        }

        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            if (_presetMode != ScenePresetMode.BuildingBlock || !context.hasFocusOrientation)
            {
                return 1f;
            }

            Vector3 viewDir = (context.focusCenter - cameraPosition).normalized;
            Vector3 horizontalView = Vector3.ProjectOnPlane(viewDir, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(context.focusForward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(context.focusRight, Vector3.up).normalized;

            if (horizontalView.sqrMagnitude <= 0.0001f || forward.sqrMagnitude <= 0.0001f || right.sqrMagnitude <= 0.0001f)
            {
                return 1f;
            }

            float facingForward = Mathf.Abs(Vector3.Dot(horizontalView, forward));
            float facingRight = Mathf.Abs(Vector3.Dot(horizontalView, right));
            float bestFacadeFacing = Mathf.Max(facingForward, facingRight);

            return Mathf.Clamp01(Mathf.Lerp(bestFacadeFacing, bestFacadeFacing * bestFacadeFacing, 0.35f));
        }
    }
}
