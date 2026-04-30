using UnityEngine;

namespace AutoCamera
{
    public class FillRatioScorer : ICompositionScorer
    {
        private readonly PresentationBoundsProvider _boundsProvider;

        public FillRatioScorer(PresentationBoundsProvider boundsProvider)
        {
            _boundsProvider = boundsProvider;
        }

        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            float distance = Vector3.Distance(cameraPosition, focusPoint);
            float halfFovRad = fov * 0.5f * Mathf.Deg2Rad;
            float tanFov = Mathf.Tan(halfFovRad);
            float aspect = context.aspect;
            Bounds presentationBounds = _boundsProvider.GetBounds(context);

            float viewHeight = 2f * distance * tanFov;
            float viewWidth = viewHeight * aspect;
            float fillH = presentationBounds.size.y / viewHeight;
            float fillW = presentationBounds.size.x / viewWidth;
            float actualFill = Mathf.Max(fillH, fillW);
            float diff = Mathf.Abs(actualFill - variant.fillTarget);
            return Mathf.Clamp01(1f - diff * 2f);
        }
    }
}
