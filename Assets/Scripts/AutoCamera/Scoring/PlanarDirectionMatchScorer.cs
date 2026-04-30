using UnityEngine;

namespace AutoCamera
{
    public class PlanarDirectionMatchScorer : ICompositionScorer
    {
        private readonly ScenePresetMode _presetMode;

        public PlanarDirectionMatchScorer(ScenePresetMode presetMode)
        {
            _presetMode = presetMode;
        }

        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            if ((_presetMode != ScenePresetMode.InteriorZone && _presetMode != ScenePresetMode.Floor) || !context.hasFocusOrientation)
            {
                return 1f;
            }

            Vector3 viewForward = (focusPoint - cameraPosition).normalized;
            CameraMath.BuildCameraBasis(viewForward, out Vector3 screenRight, out Vector3 screenUp);

            float forwardScore = ScoreSinglePlanarAxis(context.focusForward, viewForward, screenRight, screenUp);
            float rightScore = ScoreSinglePlanarAxis(context.focusRight, viewForward, screenRight, screenUp);

            return (forwardScore + rightScore) * 0.5f;
        }

        private static float ScoreSinglePlanarAxis(Vector3 axis, Vector3 viewForward, Vector3 screenRight, Vector3 screenUp)
        {
            Vector3 planarAxis = Vector3.ProjectOnPlane(axis, Vector3.up).normalized;
            if (planarAxis.sqrMagnitude <= 0.0001f)
            {
                return 1f;
            }

            float intoScreenPenalty = Mathf.Abs(Vector3.Dot(planarAxis, Vector3.ProjectOnPlane(viewForward, Vector3.up).normalized));
            float stayOnScreen = 1f - intoScreenPenalty;

            Vector3 projected = Vector3.ProjectOnPlane(planarAxis, viewForward);
            float projectedLength = projected.magnitude;
            if (projectedLength <= 0.0001f)
            {
                return 0f;
            }

            projected /= projectedLength;
            float alignX = Mathf.Abs(Vector3.Dot(projected, screenRight));
            float alignY = Mathf.Abs(Vector3.Dot(projected, screenUp));
            float screenAlignment = Mathf.Max(alignX, alignY);

            return Mathf.Clamp01(stayOnScreen * 0.6f + screenAlignment * 0.4f);
        }
    }
}
