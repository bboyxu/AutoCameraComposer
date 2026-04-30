using UnityEngine;

namespace AutoCamera
{
    public class RuleOfThirdsScorer : ICompositionScorer
    {
        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            Vector3 forward = (focusPoint - cameraPosition).normalized;
            CameraMath.BuildCameraBasis(forward, out Vector3 right, out Vector3 up);

            Vector3 offset = context.weightedVisualCenter - focusPoint;
            float localX = Vector3.Dot(offset, right);
            float localY = Vector3.Dot(offset, up);
            float distance = Vector3.Distance(cameraPosition, focusPoint);
            float halfFovRad = fov * 0.5f * Mathf.Deg2Rad;
            float tanFov = Mathf.Tan(halfFovRad);
            float aspect = context.aspect;

            float screenX = 0.5f + localX / (distance * tanFov * aspect);
            float screenY = 0.5f + localY / (distance * tanFov);
            screenX = Mathf.Clamp01(screenX);
            screenY = Mathf.Clamp01(screenY);

            float[] idealX = { 1f / 3f, 2f / 3f };
            float[] idealY = { 1f / 3f, 2f / 3f };
            float minDistance = float.MaxValue;

            for (int ix = 0; ix < idealX.Length; ix++)
            {
                for (int iy = 0; iy < idealY.Length; iy++)
                {
                    float dx = screenX - idealX[ix];
                    float dy = screenY - idealY[iy];
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    minDistance = Mathf.Min(minDistance, dist);
                }
            }

            return 1f - minDistance / Mathf.Sqrt(2f);
        }
    }
}
