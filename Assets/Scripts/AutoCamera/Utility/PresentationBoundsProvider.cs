using UnityEngine;

namespace AutoCamera
{
    public class PresentationBoundsProvider
    {
        private readonly ScenePresetMode _presetMode;

        public PresentationBoundsProvider(ScenePresetMode presetMode)
        {
            _presetMode = presetMode;
        }

        public Bounds GetBounds(SearchContext context)
        {
            if (_presetMode == ScenePresetMode.BuildingBlock
                || _presetMode == ScenePresetMode.Floor
                || _presetMode == ScenePresetMode.InteriorZone)
            {
                return context.focusBounds;
            }

            return context.subjectBounds;
        }

        public Vector3 GetCenter(SearchContext context)
        {
            if (_presetMode == ScenePresetMode.BuildingBlock
                || _presetMode == ScenePresetMode.Floor
                || _presetMode == ScenePresetMode.InteriorZone)
            {
                return context.focusCenter;
            }

            return context.subjectCenter;
        }

        public float GetRadius(SearchContext context)
        {
            Bounds presentationBounds = GetBounds(context);
            float radius = Mathf.Max(presentationBounds.extents.magnitude, 0.01f);

            if (_presetMode == ScenePresetMode.Floor)
            {
                return radius * 0.92f;
            }

            if (_presetMode == ScenePresetMode.InteriorZone)
            {
                return radius * 0.76f;
            }

            return radius;
        }
    }
}
