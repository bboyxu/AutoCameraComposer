using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoCamera
{
    [DisallowMultipleComponent]
    public class UGUIAutoCameraTestEntry : MonoBehaviour
    {
        public enum PresetKind
        {
            Exterior,
            BuildingBlock,
            Floor,
            InteriorZone,
            EquipmentGroup
        }

        [System.Serializable]
        public class EntryBinding
        {
            [Tooltip("仅用于 Inspector 识别")]
            public string displayName;

            [Tooltip("该模式对应的场景节点根对象。点击按钮时只显示该节点，隐藏其它节点。")]
            public GameObject nodeRoot;

            [Tooltip("该节点下 Main Camera 上的 AutoCameraComposer；为空时会尝试自动查找。")]
            public AutoCameraComposer composer;

            [Tooltip("该按钮对应的预设模式")]
            public PresetKind presetKind = PresetKind.Exterior;

            [Tooltip("触发该模式的按钮")]
            public Button button;
        }

        [Header("五个模式配置")]
        [Tooltip("请按界面顺序配置 5 条绑定关系")]
        public List<EntryBinding> entries = new List<EntryBinding>(5);

        [Header("随机美学按钮")]
        [Tooltip("对当前显示节点生效")]
        public Button randomAestheticButton;

        [Header("当前模式显示")]
        [Tooltip("用于显示当前模式的 UI Text（可选）")]
        public TextMeshProUGUI currentModeText;

        [Tooltip("显示前缀")]
        public string modeTextPrefix = "当前模式：";

        [Header("行为设置")]
        public bool autoSelectFirstOnStart = true;

        private int _currentIndex = -1;

        private void Awake()
        {
            BindButtons();
        }

        private void Start()
        {
            if (!autoSelectFirstOnStart)
            {
                UpdateModeText("未选择");
                return;
            }

            int firstValidIndex = GetFirstValidEntryIndex();
            if (firstValidIndex >= 0)
            {
                SelectEntry(firstValidIndex);
            }
            else
            {
                UpdateModeText("未选择");
            }
        }

        private void BindButtons()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                EntryBinding entry = entries[i];
                if (entry == null || entry.button == null)
                {
                    continue;
                }

                int index = i;
                entry.button.onClick.RemoveAllListeners();
                entry.button.onClick.AddListener(() => SelectEntry(index));
            }

            if (randomAestheticButton != null)
            {
                randomAestheticButton.onClick.RemoveAllListeners();
                randomAestheticButton.onClick.AddListener(ApplyRandomAestheticToCurrent);
            }
        }

        public void SelectEntry(int index)
        {
            if (index < 0 || index >= entries.Count)
            {
                Debug.LogWarning($"[UGUIAutoCameraTestEntry] 索引越界: {index}");
                return;
            }

            EntryBinding entry = entries[index];
            if (entry == null || entry.nodeRoot == null)
            {
                Debug.LogWarning($"[UGUIAutoCameraTestEntry] 条目无效或节点为空: {index}");
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                EntryBinding item = entries[i];
                if (item == null || item.nodeRoot == null)
                {
                    continue;
                }

                item.nodeRoot.SetActive(i == index);
            }

            _currentIndex = index;
            UpdateModeText(GetEntryModeName(entry));
            ApplyPresetAndCompose(entry);
        }

        public void ApplyRandomAestheticToCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= entries.Count)
            {
                Debug.LogWarning("[UGUIAutoCameraTestEntry] 还未选择有效节点，无法应用随机美学。");
                return;
            }

            EntryBinding entry = entries[_currentIndex];
            if (entry == null || entry.nodeRoot == null || !entry.nodeRoot.activeInHierarchy)
            {
                Debug.LogWarning("[UGUIAutoCameraTestEntry] 当前节点无效或未显示，无法应用随机美学。");
                return;
            }

            AutoCameraComposer composer = EnsureComposer(entry);
            if (composer == null)
            {
                Debug.LogWarning($"[UGUIAutoCameraTestEntry] 未找到 AutoCameraComposer: {_currentIndex}");
                return;
            }

            composer.ApplyPresetRandomAesthetic();
            composer.ComposeCamera();
            UpdateModeText(GetEntryModeName(entry) + "（随机美学）");
        }

        private void ApplyPresetAndCompose(EntryBinding entry)
        {
            AutoCameraComposer composer = EnsureComposer(entry);
            if (composer == null)
            {
                Debug.LogWarning($"[UGUIAutoCameraTestEntry] 未找到 AutoCameraComposer: {entry.displayName}");
                return;
            }

            composer.ApplyPreset(PresetKindToMode(entry.presetKind));
            composer.ComposeCamera();
        }

        private static ScenePresetMode PresetKindToMode(PresetKind kind)
        {
            switch (kind)
            {
                case PresetKind.Exterior: return ScenePresetMode.Exterior;
                case PresetKind.BuildingBlock: return ScenePresetMode.BuildingBlock;
                case PresetKind.Floor: return ScenePresetMode.Floor;
                case PresetKind.InteriorZone: return ScenePresetMode.InteriorZone;
                case PresetKind.EquipmentGroup: return ScenePresetMode.EquipmentGroup;
                default: return ScenePresetMode.Exterior;
            }
        }

        private AutoCameraComposer EnsureComposer(EntryBinding entry)
        {
            if (entry.composer != null)
            {
                return entry.composer;
            }

            if (entry.nodeRoot == null)
            {
                return null;
            }

            Transform cameraTransform = FindChildByName(entry.nodeRoot.transform, "Main Camera");
            if (cameraTransform == null)
            {
                return null;
            }

            entry.composer = cameraTransform.GetComponent<AutoCameraComposer>();
            return entry.composer;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == childName)
                {
                    return all[i];
                }
            }

            return null;
        }

        private int GetFirstValidEntryIndex()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].nodeRoot != null)
                {
                    return i;
                }
            }

            return -1;
        }

        private string GetEntryModeName(EntryBinding entry)
        {
            if (entry == null)
            {
                return "未选择";
            }

            if (!string.IsNullOrEmpty(entry.displayName))
            {
                return entry.displayName;
            }

            switch (entry.presetKind)
            {
                case PresetKind.Exterior:
                    return "外景构图";
                case PresetKind.BuildingBlock:
                    return "楼栋构图";
                case PresetKind.Floor:
                    return "层构图";
                case PresetKind.InteriorZone:
                    return "层内区域构图";
                case PresetKind.EquipmentGroup:
                    return "设备结构构图";
                default:
                    return entry.presetKind.ToString();
            }
        }

        private void UpdateModeText(string modeName)
        {
            if (currentModeText == null)
            {
                return;
            }

            currentModeText.text = string.IsNullOrEmpty(modeTextPrefix)
                ? modeName
                : modeTextPrefix + modeName;
        }
    }
}
