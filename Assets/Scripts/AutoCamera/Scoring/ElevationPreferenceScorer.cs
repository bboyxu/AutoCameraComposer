using UnityEngine;

namespace AutoCamera
{
    public class ElevationPreferenceScorer : ICompositionScorer
    {
        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            Vector3 direction = (cameraPosition - focusPoint).normalized;
            float elevation = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
            float difference = Mathf.Abs(elevation - variant.preferredElevation);
            return Mathf.Clamp01(1f - difference / 45f);
        }
    }
}
