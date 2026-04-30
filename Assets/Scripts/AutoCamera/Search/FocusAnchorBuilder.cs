using UnityEngine;

namespace AutoCamera
{
    public static class FocusAnchorBuilder
    {
        public static Vector2[] Build(int repairPass, ScenePresetMode presetMode)
        {
            if (presetMode == ScenePresetMode.BuildingBlock)
            {
                return repairPass <= 0
                    ? new[] { Vector2.zero, new Vector2(-0.40f, 0f), new Vector2(0.40f, 0f), new Vector2(0f, 0.24f), new Vector2(0f, -0.20f) }
                    : new[] { Vector2.zero, new Vector2(-0.40f, 0f), new Vector2(0.40f, 0f), new Vector2(0f, 0.24f), new Vector2(0f, -0.20f),
                        new Vector2(-0.32f, 0.18f), new Vector2(0.32f, 0.18f), new Vector2(-0.32f, -0.16f), new Vector2(0.32f, -0.16f) };
            }

            if (presetMode == ScenePresetMode.Exterior)
            {
                return repairPass <= 0
                    ? new[] { Vector2.zero, new Vector2(-0.55f, 0f), new Vector2(0.55f, 0f), new Vector2(0f, 0.30f), new Vector2(0f, -0.25f) }
                    : new[] { Vector2.zero, new Vector2(-0.55f, 0f), new Vector2(0.55f, 0f), new Vector2(0f, 0.30f), new Vector2(0f, -0.25f),
                        new Vector2(-0.45f, 0.22f), new Vector2(0.45f, 0.22f), new Vector2(-0.45f, -0.20f), new Vector2(0.45f, -0.20f) };
            }

            if (presetMode == ScenePresetMode.Floor || presetMode == ScenePresetMode.InteriorZone)
            {
                return repairPass <= 0
                    ? new[] { Vector2.zero, new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(-0.6f, 0f), new Vector2(0.6f, 0f) }
                    : new[] { Vector2.zero, new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(-0.6f, 0f), new Vector2(0.6f, 0f),
                        new Vector2(-1f, -0.2f), new Vector2(1f, -0.2f) };
            }

            return repairPass <= 0
                ? new[] { Vector2.zero, new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(-1f, 0.6f), new Vector2(1f, 0.6f), new Vector2(-1f, -0.6f), new Vector2(1f, -0.6f) }
                : new[] { Vector2.zero, new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0.8f), new Vector2(0f, -0.8f),
                    new Vector2(-1f, 0.6f), new Vector2(1f, 0.6f), new Vector2(-1f, -0.6f), new Vector2(1f, -0.6f) };
        }
    }
}
