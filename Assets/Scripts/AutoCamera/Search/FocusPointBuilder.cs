using UnityEngine;

namespace AutoCamera
{
    public class FocusPointBuilder
    {
        private readonly ScenePresetMode _presetMode;
        private readonly PresentationBoundsProvider _boundsProvider;

        public FocusPointBuilder(ScenePresetMode presetMode, PresentationBoundsProvider boundsProvider)
        {
            _presetMode = presetMode;
            _boundsProvider = boundsProvider;
        }

        public Vector3 Build(SearchContext context, Vector3 direction, Vector2 anchor, float offsetScale)
        {
            CameraMath.BuildCameraBasis(-direction, out Vector3 right, out Vector3 up);
            Bounds presentationBounds = _boundsProvider.GetBounds(context);
            Vector3 baseCenter = _boundsProvider.GetCenter(context);
            bool isExteriorMode = _presetMode == ScenePresetMode.Exterior;
            bool isBuildingBlockMode = _presetMode == ScenePresetMode.BuildingBlock;
            bool isFloorMode = _presetMode == ScenePresetMode.Floor;
            bool isInteriorZoneMode = _presetMode == ScenePresetMode.InteriorZone;
            bool isPlanarAreaMode = isFloorMode || isInteriorZoneMode;
            float verticalOffsetScale = isFloorMode ? 0.03f : (isInteriorZoneMode ? 0.08f : 1f);
            Vector3 offset =
                right * presentationBounds.extents.x * offsetScale * anchor.x +
                up * presentationBounds.extents.y * offsetScale * anchor.y * verticalOffsetScale;

            Vector3 visualBias = (context.weightedVisualCenter - baseCenter) * (isFloorMode ? 0.06f : (isInteriorZoneMode ? 0.10f : (isExteriorMode ? 0.08f : (isBuildingBlockMode ? 0.12f : 0.30f))));

            if (isPlanarAreaMode)
            {
                float floorY = presentationBounds.center.y - presentationBounds.extents.y * (isFloorMode ? 0.58f : 0.50f);
                float loweredY = Mathf.Lerp(baseCenter.y + visualBias.y + offset.y, floorY, isFloorMode ? 0.91f : 0.84f);
                return new Vector3(baseCenter.x + visualBias.x + offset.x, loweredY, baseCenter.z + visualBias.z + offset.z);
            }

            return baseCenter + visualBias + offset;
        }
    }
}
