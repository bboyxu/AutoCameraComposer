using UnityEngine;

namespace AutoCamera
{
    public class TopDownPitchScorer : ICompositionScorer
    {
        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            if (variant.preferredTopDownPitch <= 0f)
            {
                return 1f;
            }

            Vector3 lookForward = (focusPoint - cameraPosition).normalized;
            float pitch = Mathf.Asin(Mathf.Clamp(-lookForward.y, -1f, 1f)) * Mathf.Rad2Deg;
            float difference = Mathf.Abs(pitch - variant.preferredTopDownPitch);
            return Mathf.Clamp01(1f - difference / 22f);
        }
    }
}
