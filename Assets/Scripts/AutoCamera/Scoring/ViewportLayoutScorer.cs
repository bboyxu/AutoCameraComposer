using UnityEngine;

namespace AutoCamera
{
    public class ViewportLayoutScorer : ICompositionScorer
    {
        private readonly ScenePresetMode _presetMode;
        private readonly PresentationBoundsProvider _boundsProvider;

        public ViewportLayoutScorer(ScenePresetMode presetMode, PresentationBoundsProvider boundsProvider)
        {
            _presetMode = presetMode;
            _boundsProvider = boundsProvider;
        }

        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            bool isExteriorMode = _presetMode == ScenePresetMode.Exterior;
            bool isBuildingBlockMode = _presetMode == ScenePresetMode.BuildingBlock;
            if (!isExteriorMode && !isBuildingBlockMode)
            {
                return 1f;
            }

            Bounds presentationBounds = isBuildingBlockMode ? context.focusBounds : _boundsProvider.GetBounds(context);
            if (!CameraMath.TryGetViewportProjectionMetrics(context, presentationBounds, cameraPosition, focusPoint, fov, out ViewportProjectionMetrics metrics))
            {
                return 0f;
            }

            float centerX = (metrics.minX + metrics.maxX) * 0.5f;
            float centerY = (metrics.minY + metrics.maxY) * 0.5f;
            float centerPenalty = Mathf.Sqrt(
                (centerX - 0.5f) * (centerX - 0.5f) +
                (centerY - 0.5f) * (centerY - 0.5f));

            float leftMargin = metrics.minX;
            float rightMargin = 1f - metrics.maxX;
            float bottomMargin = metrics.minY;
            float topMargin = 1f - metrics.maxY;

            float horizontalBalancePenalty = Mathf.Abs(leftMargin - rightMargin);
            float verticalBalancePenalty = Mathf.Abs(topMargin - bottomMargin);

            float marginMean = (leftMargin + rightMargin + topMargin + bottomMargin) * 0.25f;
            float marginSpread =
                (Mathf.Abs(leftMargin - marginMean) +
                 Mathf.Abs(rightMargin - marginMean) +
                 Mathf.Abs(topMargin - marginMean) +
                 Mathf.Abs(bottomMargin - marginMean)) * 0.25f;

            float safeMargin = isBuildingBlockMode ? 0.04f : 0.05f;
            float edgePressure =
                Mathf.Max(0f, safeMargin - leftMargin) +
                Mathf.Max(0f, safeMargin - rightMargin) +
                Mathf.Max(0f, safeMargin - bottomMargin) +
                Mathf.Max(0f, safeMargin - topMargin);

            float leftOverflow = Mathf.Max(0f, -metrics.minX);
            float rightOverflow = Mathf.Max(0f, metrics.maxX - 1f);
            float bottomOverflow = Mathf.Max(0f, -metrics.minY);
            float topOverflow = Mathf.Max(0f, metrics.maxY - 1f);

            float centerWeight = isBuildingBlockMode ? 2.75f : 2.15f;
            float horizontalBalanceWeight = isBuildingBlockMode ? 1.85f : 1.30f;
            float verticalBalanceWeight = isBuildingBlockMode ? 1.85f : 1.30f;
            float marginSpreadWeight = isBuildingBlockMode ? 2.20f : 1.50f;
            float edgePressureWeight = isBuildingBlockMode ? 1.55f : 1.15f;
            float overflowWeight = isBuildingBlockMode ? 5.80f : 4.20f;

            float penalty =
                centerPenalty * centerWeight +
                horizontalBalancePenalty * horizontalBalanceWeight +
                verticalBalancePenalty * verticalBalanceWeight +
                marginSpread * marginSpreadWeight +
                edgePressure * edgePressureWeight +
                (leftOverflow + rightOverflow + bottomOverflow + topOverflow) * overflowWeight;

            return Mathf.Clamp01(1f - penalty);
        }
    }
}
