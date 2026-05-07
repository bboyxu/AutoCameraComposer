```yaml
spec_id: "L2-MOD-001"
title: "场景预设系统（Scene Presets）"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "Preset", "Strategy Pattern", "Registry"]
parent_spec: "L1-SYS-001"
sub_specs:
  - "L3-TASK-001"
```

# L2 模块级 Spec：场景预设系统

## 1. 概述

### 1.1 模块定位

场景预设系统定义了 6 种建筑可视化场景模式（外景、楼栋、楼层、层内区域、设备组、随机美学）的参数模板。每个预设将**构图风格、搜索质量、验收阈值、权重分配、风格参数、高级规则**组合为一个不可变配置单元。

### 1.2 为什么需要这个模块

不同场景模式对相机位姿的要求差异巨大：外景需要大纵深和空间层次，楼栋需要立面突出和主体饱满，楼层需要自上而下的平面覆盖。预设系统将这些差异量化为具体参数，使系统可以一键切换模式，无需用户手动调整 30+ 个参数。

### 1.3 范围边界

| 包含 | 不包含 |
|------|--------|
| 6 种预设的参数定义 | 运行时动态修改预设参数 |
| 预设注册表（PresetRegistry） | 用户自定义预设的 GUI |
| IScenePreset 接口定义 | 预设的序列化/反序列化 |
| 参数合理性校验 | 预设之间的自动切换动画 |

## 2. 核心目标

| 编号 | 目标 | 量化指标 |
|------|------|----------|
| G1 | 每种场景模式有独立完整的参数配置 | 6 个预设，每个覆盖 10 属性 + 2 子结构 |
| G2 | 预设通过注册表统一访问 | PresetRegistry 覆盖全部 ScenePresetMode |
| G3 | 预设参数之间逻辑一致 | min < max, 权重和为 1, 阈值在合理范围 |
| G4 | 新增预设只需实现接口 + 注册 | 2 步操作完成扩展 |

## 3. 接口契约

### 3.1 IScenePreset

```csharp
public interface IScenePreset
{
    ScenePresetMode PresetMode { get; }     // 预设标识
    CompositionStyle Style { get; }          // 构图风格
    SearchQuality Quality { get; }           // 搜索质量
    bool LivePreview { get; }                // 是否实时预览
    bool AutoRepair { get; }                 // 是否自动修复
    int MaxRepairPasses { get; }             // 最大修复轮数
    float TransitionDuration { get; }        // 过渡动画时长（秒）
    MetricThresholds Thresholds { get; }      // 5 项验收阈值
    CompositionWeights Weights { get; }       // 4 项核心权重
    StyleSettings StyleSettings { get; }      // 风格基础参数
    PresetRuleSettings RuleSettings { get; }  // 预设高级规则
}
```

### 3.2 PresetRegistry

```csharp
public static class PresetRegistry
{
    // 获取指定模式的预设，不存在返回 null
    public static IScenePreset GetPreset(ScenePresetMode mode);

    // 安全获取
    public static bool TryGetPreset(ScenePresetMode mode, out IScenePreset preset);

    // 遍历所有预设
    public static IEnumerable<IScenePreset> GetAllPresets();
}
```

## 4. 详细行为

### 4.1 预设参数全景

#### 顶层参数

| 参数 | Exterior | BuildingBlock | Floor | InteriorZone | EquipmentGroup | RandomAesthetic |
|------|----------|--------------|-------|-------------|----------------|-----------------|
| Style | WideScene | Balanced | TopDown | TopDown | HeroProduct | Balanced |
| Quality | High | High | High | High | High | High |
| LivePreview | true | true | true | true | true | true |
| AutoRepair | true | true | true | true | true | true |
| MaxRepair | 4 | 4 | 3 | 4 | 4 | 4 |
| Transition | 0.9s | 0.7s | 0.6s | 0.6s | 0.5s | 0.55s |

#### MetricThresholds

| 阈值 | Exterior | BuildingBlock | Floor | InteriorZone | EquipmentGroup | RandomAesthetic |
|------|----------|--------------|-------|-------------|----------------|-----------------|
| overall | 0.80 | 0.80 | 0.81 | 0.80 | 0.84 | 0.82 |
| ruleOfThirds | 0.72 | 0.74 | 0.68 | 0.72 | 0.76 | 0.74 |
| fillRatio | 0.74 | 0.78 | 0.84 | 0.82 | 0.88 | 0.80 |
| balance | 0.80 | 0.80 | 0.84 | 0.78 | 0.76 | 0.76 |
| depthLayers | 0.66 | 0.60 | 0.40 | 0.46 | 0.52 | 0.52 |

#### CompositionWeights

| 权重 | Exterior | BuildingBlock | Floor | InteriorZone | EquipmentGroup | RandomAesthetic |
|------|----------|--------------|-------|-------------|----------------|-----------------|
| ruleOfThirds | 0.20 | 0.25 | 0.20 | 0.20 | 0.30 | 0.25 |
| fillRatio | 0.32 | 0.25 | 0.24 | 0.24 | 0.26 | 0.25 |
| balance | 0.18 | 0.20 | 0.24 | 0.24 | 0.16 | 0.20 |
| depthLayers | 0.30 | 0.30 | 0.32 | 0.32 | 0.28 | 0.30 |

#### StyleSettings

| 参数 | Exterior | BuildingBlock | Floor | InteriorZone | EquipmentGroup | RandomAesthetic |
|------|----------|--------------|-------|-------------|----------------|-----------------|
| baseFill | 0.62 | 0.70 | 0.68 | 0.68 | 0.76 | 0.70 |
| basePadding | 1.42 | 1.15 | 1.15 | 1.15 | 1.08 | 1.15 |
| minElevation | 18° | 12° | 48° | 48° | 8° | 12° |
| maxElevation | 42° | 60° | 82° | 82° | 34° | 60° |
| minFov | 22° | 24° | 24° | 24° | 20° | 24° |
| maxFov | 52° | 56° | 58° | 58° | 46° | 56° |
| focusOffsetScale | 0.16 | 0.18 | 0.12 | 0.12 | 0.20 | 0.18 |
| preferredElevation | 30° | 34° | 68° | 68° | 22° | 34° |
| elevationInfluence | 0.28 | 0.12 | 0.28 | 0.28 | 0.10 | 0.12 |
| separationInfluence | 0.30 | 0.18 | 0.12 | 0.12 | 0.14 | 0.18 |

#### PresetRuleSettings（场景特定高级规则）

| 参数 | Exterior | BuildingBlock | Floor | InteriorZone | EquipmentGroup | RandomAesthetic |
|------|----------|--------------|-------|-------------|----------------|-----------------|
| focusVisibilityInfluence | 0.10 | 0.34 | 0.08 | 0.10 | 0.12 | 0.10 |
| priorityVisibilityInfluence | 0.42 | 0.44 | 0.08 | 0.08 | 0.38 | 0.06 |
| facadeInfluence | 0.02 | 0.48 | 0.02 | 0.04 | 0.02 | 0.02 |
| layoutInfluence | 0.50 | 0.38 | 0.10 | 0.12 | 0.08 | 0.08 |
| planarDirectionInfluence | 0.04 | 0.06 | 0.30 | 0.34 | 0.10 | 0.04 |
| distanceInfluence | 0.40 | 0.42 | 0.34 | 0.28 | 0.28 | 0.10 |
| preferredDistanceScale | 1.62 | 1.48 | 0.92 | 0.76 | 0.76 | 1.00 |
| preferredElevationBias | 0° | 0° | 22° | 8° | 12° | 0° |
| topDownPitchInfluence | 0.26 | 0.38 | 0.56 | 0.42 | 0.40 | 0.00 |
| preferredTopDownPitch | 30° | 20° | 86° | 72° | 45° | 0° |
| elevationCurveExponent | 1.05 | 1.12 | 2.60 | 2.10 | 1.30 | 1.00 |
| enforcedMinElevation | 22° | 14° | 76° | 62° | 34° | 0° |
| enforcedMaxElevation | 38° | 28° | 89° | 89° | 58° | 0° |

### 4.2 预设语义说明

| 预设 | 构图意图 | 关键特征 |
|------|---------|---------|
| **Exterior**（外景） | 建筑外部全景展示 | 大纵深(depthLayers=0.66)、宽视野(WideScene)、高距离影响(0.40)、大留白(1.42) |
| **BuildingBlock**（楼栋） | 单体建筑主体展示 | 高立面影响(0.48)、高主体可见性影响(0.34)、高优先级可见性(0.44) |
| **Floor**（楼层） | 楼层平面覆盖 | 俯视偏好(86°)、高平衡要求(0.84)、低纵深要求(0.40)、平面方向匹配(0.30) |
| **InteriorZone**（层内区域） | 室内区域展示 | 俯视(72°)、区域主体、空间关系、平面方向匹配(0.34) |
| **EquipmentGroup**（设备组） | 设备群完整展示 | 45°侧俯视、高填充要求(0.88)、设备可见性优先(0.38)、低立面影响 |
| **RandomAesthetic**（随机美学） | 多样化自动探索 | 忽略场景类型，基于目标列表随机生成拍照式偏好、5 种风格变体 |

## 5. 技术约束

| 约束 | 说明 |
|------|------|
| 预设类必须是 class（引用类型），实现 IScenePreset | 因为需要存储在 Dictionary 中 |
| 预设的子结构全部是 struct（值类型） | StyleSettings, PresetRuleSettings, MetricThresholds, CompositionWeights |
| 所有预设参数硬编码在预设类中 | 不使用 ScriptableObject 或 JSON，保证编译时类型安全 |
| PresetRegistry 使用静态 Dictionary | 应用启动时注册一次，运行时只读 |
| 新增预设必须同时更新 ScenePresetMode 枚举 | 保持枚举与注册表一致 |

## 6. 验收标准

### AC-PRE-001: 注册表完整性

```
GIVEN PresetRegistry 已初始化
WHEN  遍历 ScenePresetMode 的所有枚举值（除 Custom）
THEN  每个枚举值都能从 PresetRegistry.GetPreset() 获取非 null 的预设
```

### AC-PRE-002: 预设参数合法性

```
GIVEN 任意一个 IScenePreset 实现
WHEN  检查其属性
THEN  Thresholds 的 5 个值均在 [0.2, 0.95] 范围内
AND   Weights 的 4 个值之和约等于 1.0（容差 0.001）
AND   StyleSettings.minElevation < StyleSettings.maxElevation
AND   StyleSettings.minFov < StyleSettings.maxFov
AND   MaxRepairPasses ≥ 0
AND   TransitionDuration > 0
```

### AC-PRE-003: 模式切换行为

```
GIVEN AutoCameraComposer 实例
WHEN  依次调用 ApplyPreset() 传入 6 种 ScenePresetMode
THEN  每次调用后，composer.scenePresetMode 等于传入的 mode
AND   compositionStyle、searchQuality、targetThresholds 等字段均更新为对应预设值
```

### AC-PRE-004: 预设扩展性

```
GIVEN 新增一个实现了 IScenePreset 的类
WHEN  将其注册到 PresetRegistry 的静态字典中
AND   在 ScenePresetMode 枚举中添加对应值
THEN  调用 PresetRegistry.GetPreset(newMode) 返回新预设
AND   不影响已有预设的行为
```

## 7. 上下文与引用

### 7.1 相关 Spec
- 父 Spec: [L1-SYS-001](../L1-system/auto-camera-composer.md) — 系统总览
- 关联模块: [L2-MOD-003](search-strategy.md) — SearchVariantBuilder 使用预设参数生成搜索变体

### 7.2 源代码文件
- `Assets/Scripts/AutoCamera/Presets/IScenePreset.cs`
- `Assets/Scripts/AutoCamera/Presets/PresetRegistry.cs`
- `Assets/Scripts/AutoCamera/Presets/ExteriorPreset.cs`
- `Assets/Scripts/AutoCamera/Presets/BuildingBlockPreset.cs`
- `Assets/Scripts/AutoCamera/Presets/FloorPreset.cs`
- `Assets/Scripts/AutoCamera/Presets/InteriorZonePreset.cs`
- `Assets/Scripts/AutoCamera/Presets/EquipmentGroupPreset.cs`
- `Assets/Scripts/AutoCamera/Presets/RandomAestheticPreset.cs`

### 7.3 相关枚举
- `Assets/Scripts/AutoCamera/Enums/ScenePresetMode.cs`
- `Assets/Scripts/AutoCamera/Enums/CompositionStyle.cs`
- `Assets/Scripts/AutoCamera/Enums/SearchQuality.cs`
