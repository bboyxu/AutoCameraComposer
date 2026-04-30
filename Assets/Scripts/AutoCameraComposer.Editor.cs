#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AutoCamera
{
    [CustomEditor(typeof(AutoCameraComposer))]
    public class AutoCameraComposerEditor : Editor
    {
        private SerializedProperty _targetGroups;
        private SerializedProperty _compositionStyle;
        private SerializedProperty _searchQuality;
        private SerializedProperty _livePreview;
        private SerializedProperty _autoRepair;
        private SerializedProperty _maxRepairPasses;
        private SerializedProperty _targetThresholds;
        private SerializedProperty _transitionDuration;

        private static readonly Color SectionColor = new Color(0.94f, 0.96f, 1f, 1f);

        private void OnEnable()
        {
            _targetGroups = serializedObject.FindProperty("targetGroups");
            _compositionStyle = serializedObject.FindProperty("compositionStyle");
            _searchQuality = serializedObject.FindProperty("searchQuality");
            _livePreview = serializedObject.FindProperty("livePreview");
            _autoRepair = serializedObject.FindProperty("autoRepair");
            _maxRepairPasses = serializedObject.FindProperty("maxRepairPasses");
            _targetThresholds = serializedObject.FindProperty("targetThresholds");
            _transitionDuration = serializedObject.FindProperty("transitionDuration");
        }

        public override void OnInspectorGUI()
        {
            AutoCameraComposer composer = (AutoCameraComposer)target;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawTitle();

            DrawSection("目标对象", "只保留少量高层参数。底层 FOV、留白、填充、仰角、权重由自动优化器自己搜索。");
            EditorGUILayout.PropertyField(_targetGroups, new GUIContent("目标列表"));

            DrawSection("建筑场景预设", "按建筑可视化的真实使用场景划分。点一下就会套用对应的构图策略、搜索强度和专业验收阈值。");
            DrawPresetButtons(composer);

            DrawSection("专业模式", "这里是你真正需要关心的几个参数。大多数情况下不需要再手工调底层构图参数。");
            EditorGUILayout.PropertyField(_compositionStyle, new GUIContent("构图风格"));
            EditorGUILayout.PropertyField(_searchQuality, new GUIContent("搜索质量"));
            EditorGUILayout.PropertyField(_livePreview, new GUIContent("实时预览"));
            EditorGUILayout.PropertyField(_autoRepair, new GUIContent("自动修复"));
            if (_autoRepair.boolValue)
            {
                EditorGUILayout.PropertyField(_maxRepairPasses, new GUIContent("最大修复轮数"));
            }
            EditorGUILayout.PropertyField(_transitionDuration, new GUIContent("运行时过渡时间"));

            DrawSection("专业阈值", "脚本会尽量让 5 个指标都达到这些目标。若某项拖后腿，系统会自动优先修复它。");
            DrawThresholdField(_targetThresholds.FindPropertyRelative("overall"), "综合分");
            DrawThresholdField(_targetThresholds.FindPropertyRelative("ruleOfThirds"), "三分法");
            DrawThresholdField(_targetThresholds.FindPropertyRelative("fillRatio"), "填充率");
            DrawThresholdField(_targetThresholds.FindPropertyRelative("balance"), "平衡性");
            DrawThresholdField(_targetThresholds.FindPropertyRelative("depthLayers"), "层次感");

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("一键专业构图", GUILayout.Height(36f)))
            {
                composer.ComposeCamera();
                SceneView.RepaintAll();
            }

            bool changed = EditorGUI.EndChangeCheck();
            if (changed && composer.livePreview && HasTargets(composer))
            {
                composer.ComposeForEditorPreview();
                SceneView.RepaintAll();
            }

            DrawSection("结果反馈", "这里直接告诉你有没有达标，最弱项是什么，系统修复到了哪一轮。");
            DrawScoreBar("综合分", composer.resultScore, composer.targetThresholds.overall, new Color(0.23f, 0.70f, 0.24f));
            DrawScoreBar("三分法", composer.resultDetails.ruleOfThirds, composer.targetThresholds.ruleOfThirds, new Color(0.25f, 0.50f, 0.90f));
            DrawScoreBar("填充率", composer.resultDetails.fillRatio, composer.targetThresholds.fillRatio, new Color(0.88f, 0.63f, 0.13f));
            DrawScoreBar("平衡性", composer.resultDetails.balance, composer.targetThresholds.balance, new Color(0.67f, 0.30f, 0.78f));
            DrawScoreBar("层次感", composer.resultDetails.depthLayers, composer.targetThresholds.depthLayers, new Color(0.83f, 0.31f, 0.31f));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(composer.resultFeedback, composer.resultEvaluation.allPassed ? MessageType.Info : MessageType.Warning);

            GUI.enabled = false;
            EditorGUILayout.IntField("通过项数", composer.resultEvaluation.passedMetricCount);
            EditorGUILayout.IntField("修复轮次", composer.resultRepairPass);
            EditorGUILayout.FloatField("最短板指标", composer.resultMinimumMetric);
            EditorGUILayout.FloatField("有效填充率", composer.resultFillTarget);
            EditorGUILayout.FloatField("有效留白", composer.resultPadding);
            EditorGUILayout.Vector3Field("相机位置", composer.resultPosition);
            EditorGUILayout.Vector3Field("注视点", composer.resultLookAt);
            EditorGUILayout.FloatField("FOV", composer.resultFOV);
            GUI.enabled = true;
        }

        private static bool HasTargets(AutoCameraComposer composer)
        {
            return composer.targetGroups != null && composer.targetGroups.Count > 0;
        }

        private static void DrawTitle()
        {
            EditorGUILayout.Space(6f);
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Auto Camera Composer", title);
            EditorGUILayout.LabelField("专业构图目标驱动版", new GUIStyle(EditorStyles.centeredGreyMiniLabel));
            EditorGUILayout.Space(4f);
        }

        private static void DrawSection(string title, string description)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 38f);
            EditorGUI.DrawRect(rect, SectionColor);
            EditorGUI.LabelField(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, 18f), title, EditorStyles.boldLabel);

            GUIStyle descriptionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
            EditorGUI.LabelField(new Rect(rect.x + 6f, rect.y + 18f, rect.width - 12f, 18f), description, descriptionStyle);
            EditorGUILayout.Space(2f);
        }

        private void DrawPresetButtons(AutoCameraComposer composer)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("外景", GUILayout.Height(28f)))
            {
                ApplyPresetAndRefresh(composer, () => composer.ApplyPreset(ScenePresetMode.Exterior));
            }
            if (GUILayout.Button("楼栋", GUILayout.Height(28f)))
            {
                ApplyPresetAndRefresh(composer, () => composer.ApplyPreset(ScenePresetMode.BuildingBlock));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("楼层", GUILayout.Height(28f)))
            {
                ApplyPresetAndRefresh(composer, () => composer.ApplyPreset(ScenePresetMode.Floor));
            }
            if (GUILayout.Button("层内区域", GUILayout.Height(28f)))
            {
                ApplyPresetAndRefresh(composer, () => composer.ApplyPreset(ScenePresetMode.InteriorZone));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("设备组", GUILayout.Height(28f)))
            {
                ApplyPresetAndRefresh(composer, () => composer.ApplyPreset(ScenePresetMode.EquipmentGroup));
            }
            if (GUILayout.Button("随机美学", GUILayout.Height(28f)))
            {
                ApplyPresetAndRefresh(composer, composer.ApplyPresetRandomAesthetic);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "预设说明：\n" +
                "外景：强调空间层次、整体稳定和建筑主体完整展示\n" +
                "楼栋：强调主体突出、比例饱满和立面观感\n" +
                "楼层：强调布局完整、画面平衡和信息覆盖\n" +
                "层内区域：强调空间关系、区域主体和内部纵深\n" +
                "设备组：强调设备群完整可见、45度俯视和整体饱满\n" +
                "随机美学：忽略场景类型，基于目标列表随机生成一套拍照式构图偏好，再自动求解最优结果",
                MessageType.None);
        }

        private void ApplyPresetAndRefresh(AutoCameraComposer composer, System.Action applyPreset)
        {
            Undo.RecordObject(composer, "Apply Camera Preset");
            applyPreset();
            EditorUtility.SetDirty(composer);
            serializedObject.Update();
            Repaint();

            if (HasTargets(composer))
            {
                composer.ComposeForEditorPreview();
                SceneView.RepaintAll();
            }
        }

        private static void DrawThresholdField(SerializedProperty property, string label)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }

        private static void DrawScoreBar(string label, float value, float target, Color color)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, 18f);

            Rect labelRect = new Rect(rowRect.x, rowRect.y, 48f, rowRect.height);
            Rect barRect = new Rect(rowRect.x + 52f, rowRect.y + 2f, rowRect.width - 116f, rowRect.height - 4f);
            Rect valueRect = new Rect(rowRect.x + rowRect.width - 58f, rowRect.y, 58f, rowRect.height);

            EditorGUI.LabelField(labelRect, label);
            EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f, 0.22f));

            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(value), barRect.height);
            EditorGUI.DrawRect(fillRect, color);

            float targetX = barRect.x + barRect.width * Mathf.Clamp01(target);
            EditorGUI.DrawRect(new Rect(targetX - 1f, barRect.y - 1f, 2f, barRect.height + 2f), new Color(1f, 1f, 1f, 0.95f));

            string text = $"{value:F3} / {target:F2}";
            GUIStyle valueStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };
            EditorGUI.LabelField(valueRect, text, valueStyle);
        }
    }
}
#endif
