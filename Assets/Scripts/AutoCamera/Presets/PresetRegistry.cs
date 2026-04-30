using System.Collections.Generic;

namespace AutoCamera
{
    public static class PresetRegistry
    {
        private static readonly Dictionary<ScenePresetMode, IScenePreset> _presets = new Dictionary<ScenePresetMode, IScenePreset>
        {
            { ScenePresetMode.Exterior, new ExteriorPreset() },
            { ScenePresetMode.BuildingBlock, new BuildingBlockPreset() },
            { ScenePresetMode.Floor, new FloorPreset() },
            { ScenePresetMode.InteriorZone, new InteriorZonePreset() },
            { ScenePresetMode.EquipmentGroup, new EquipmentGroupPreset() },
            { ScenePresetMode.RandomAesthetic, new RandomAestheticPreset() }
        };

        public static bool TryGetPreset(ScenePresetMode mode, out IScenePreset preset)
        {
            return _presets.TryGetValue(mode, out preset);
        }

        public static IScenePreset GetPreset(ScenePresetMode mode)
        {
            return _presets.TryGetValue(mode, out IScenePreset preset) ? preset : null;
        }

        public static IEnumerable<IScenePreset> GetAllPresets()
        {
            return _presets.Values;
        }
    }
}
