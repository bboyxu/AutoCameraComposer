using UnityEngine;

namespace AutoCamera
{
    public class DistanceCompactnessScorer : ICompositionScorer
    {
        private readonly ScenePresetMode _presetMode;
        private readonly PresentationBoundsProvider _boundsProvider;

        public DistanceCompactnessScorer(ScenePresetMode presetMode, PresentationBoundsProvider boundsProvider)
        {
            _presetMode = presetMode;
            _boundsProvider = boundsProvider;
        }

        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            float distance = Vector3.Distance(cameraPosition, focusPoint);
            if (distance <= Mathf.Epsilon)
            {
                return 0f;
            }

            Bounds presentationBounds = _boundsProvider.GetBounds(context);
            float presentationRadius = Mathf.Max(presentationBounds.extents.magnitude, 0.01f);
            float idealDistance = presentationRadius * Mathf.Max(0.55f, variant.preferredDistanceScale) * 2.1f;
            float diffRatio = Mathf.Abs(distance - idealDistance) / Mathf.Max(idealDistance, 0.01f);

            if (_presetMode == ScenePresetMode.Exterior)
            {
                if (distance < idealDistance)
                {
                    diffRatio *= 1.95f;
                }
                else
                {
                    diffRatio *= 0.95f;
                }
            }
            else if (distance > idealDistance)
            {
                diffRatio *= 1.25f;
            }

            return Mathf.Clamp01(1f - diffRatio);
        }
    }
}
