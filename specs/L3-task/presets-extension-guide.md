```yaml
spec_id: "L3-TASK-001"
title: "预设扩展指南（扩展一个新场景预设）"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "Preset", "Extension", "Task"]
parent_spec: "L2-MOD-001"
```

# L3 任务级 Spec：预设扩展指南

## 1. 任务描述

当需要新增一种场景模式（如"鸟瞰全景"）时，按以下步骤执行。本任务级 Spec 定义了每一步的具体操作和验收标准。

## 2. 执行步骤

### Step 1: 添加枚举值

```
文件: Assets/Scripts/AutoCamera/Enums/ScenePresetMode.cs
操作: 在 ScenePresetMode 枚举中添加新值（如 AerialPanorama）
```

### Step 2: 实现 IScenePreset

```
文件: Assets/Scripts/AutoCamera/Presets/AerialPanoramaPreset.cs
操作: 创建新类，实现 IScenePreset 的全部 10 个属性（返回属性值或子结构）
```

### Step 3: 注册到 PresetRegistry

```
文件: Assets/Scripts/AutoCamera/Presets/PresetRegistry.cs
操作: 在 _presets 静态字典中添加 { ScenePresetMode.AerialPanorama, new AerialPanoramaPreset() }
```

### Step 4: 检查关键分支类

```
□ PresentationBoundsProvider.GetBounds() — 新预设用 focusBounds 还是 subjectBounds？
□ PresentationBoundsProvider.GetRadius() — 是否需要缩放系数？
□ FocusPointBuilder.Build() — 是否需要特殊的 verticalOffsetScale 或 visualBias 系数？
□ AcceptanceEvaluator.ApplyPresetSpecificRules() — 是否需要新的 bonus/penalty 规则？
□ FocusAnchorBuilder.Build() — 是否需要自定义锚点配置？
```

### Step 5: 在 Editor 添加入口

```
文件: Assets/Scripts/AutoCameraComposer.Editor.cs
操作: 在 DrawPresetButtons() 中添加新按钮
```

## 3. 验收标准

### AC-EXT-001: 注册成功

```
GIVEN 新增的预设类已实现 IScenePreset 并注册到 PresetRegistry
WHEN  调用 PresetRegistry.GetPreset(ScenePresetMode.AerialPanorama)
THEN  返回非 null 的 IScenePreset
```

### AC-EXT-002: 参数一致性

```
GIVEN AerialPanoramaPreset 实例
WHEN  检查其属性
THEN  所有阈值在 [0.2, 0.95] 范围
AND   权重和 ≈ 1.0
AND   minElevation < maxElevation
AND   minFov < maxFov
```

### AC-EXT-003: 不影响已有预设

```
GIVEN 新增 AerialPanorama 预设后
WHEN  调用 PresetRegistry.GetPreset(ScenePresetMode.Exterior)
THEN  返回的预设参数与新增前完全一致
```

### AC-EXT-004: 触发结果可用

```
GIVEN 有效的 SearchContext
WHEN  使用新增预设模式调用 ComposeCamera()
THEN  返回有效 CameraPose（score > 0, position 非零）
```

## 4. 上下文与引用

- 父 Spec: [L2-MOD-001](../L2-module/scene-presets.md) — 预设系统详细说明
- 相关 Spec: [L2-MOD-003](../L2-module/search-strategy.md) — SearchVariantBuilder 使用预设参数
- 相关 Spec: [L2-MOD-005](../L2-module/acceptance-evaluation.md) — AcceptanceEvaluator 场景规则
