```yaml
spec_id: "L2-MOD-002"
title: "构图评分系统（Composition Scoring）"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "Scoring", "Strategy Pattern", "Pipeline"]
parent_spec: "L1-SYS-001"
sub_specs:
  - "L3-TASK-002"
```

# L2 模块级 Spec：构图评分系统

## 1. 概述

### 1.1 模块定位

构图评分系统将摄影构图原则量化为 13 个独立的可计算评分函数。每个评分器评估相机位姿的一个维度（如三分法、填充率、平衡性），输出 [0, 1] 的归一化分数。CompositionEvaluator 将这些分维度评分按权重和影响力聚合为最终的综合分。

### 1.2 为什么需要这个模块

摄影构图的好坏本质上是多维度的权衡。将每个维度独立建模为评分器，使得：(1) 每个维度的算法可以独立优化，(2) 新增评分维度无需修改现有评分器，(3) 权重可以按场景模式灵活调整。

### 1.3 范围边界

| 包含 | 不包含 |
|------|--------|
| 13 个 ICompositionScorer 实现 | 评分的可视化呈现（在 Editor Spec 中） |
| CompositionEvaluator（聚合器） | 机器学习模型替代规则评分 |
| 核心/辅助双层评分架构 | 动态加载评分器（插件系统） |

## 2. 核心目标

| 编号 | 目标 | 量化指标 |
|------|------|----------|
| G1 | 覆盖摄影构图的关键维度 | 4 核心 + 9 辅助 = 13 个评分器 |
| G2 | 每个评分器输出归一化分数 | 返回值 ∈ [0, 1] |
| G3 | 评分器可独立测试 | 每个实现 ICompositionScorer，构造器注入依赖 |
| G4 | 权重和影响力可独立配置 | 核心用归一化权重, 辅助用独立影响力系数 |

## 3. 接口契约

### 3.1 ICompositionScorer

```csharp
public interface ICompositionScorer
{
    // 评估给定相机位姿的构图质量
    // 参数: context - 场景上下文
    //       cameraPosition - 相机世界坐标
    //       focusPoint - 注视点世界坐标
    //       fov - 视场角（度）
    //       variant - 搜索变体（含权重和影响力）
    // 返回值: [0, 1]，1 表示该维度完美
    float Score(SearchContext context, Vector3 cameraPosition,
                Vector3 focusPoint, float fov, SearchVariant variant);
}
```

### 3.2 CompositionEvaluator

```csharp
public class CompositionEvaluator
{
    // 构造函数接收 presetMode 和 boundsProvider
    // 内部构建 13 个评分器列表（4 核心 + 9 辅助）

    // 返回综合评分 + 填充 CompositionDetail
    public float Evaluate(SearchContext context, Vector3 cameraPosition,
                          Vector3 focusPoint, float fov, SearchVariant variant,
                          out CompositionDetail details);
}
```

## 4. 评分器清单

### 4.1 核心评分器（4 个 — 权重归一化到 1.0）

| # | 评分器 | 评估内容 | 算法原理 |
|---|--------|---------|---------|
| 1 | **RuleOfThirdsScorer** | 视觉重心是否接近三分线交点 | 计算 weightedVisualCenter 在屏幕空间的投影到最近三分交点的距离 |
| 2 | **FillRatioScorer** | 主体在画面中的占比是否接近目标 | 包围盒在视口投影的尺寸 / 视口总尺寸 vs targetFill |
| 3 | **BalanceScorer** | 画面左右/上下的视觉重量是否平衡 | 以 focusPoint 为界统计各象限的渲染体权重 |
| 4 | **DepthLayersScorer** | 场景是否有足够的前后纵深 | 最远最近渲染体的深度差 / 相机距离 |

### 4.2 辅助评分器（9 个 — 影响力独立可调）

| # | 评分器 | 评估内容 | 场景依赖性 |
|---|--------|---------|-----------|
| 5 | **ScreenSeparationScorer** | 不同物体在屏幕上的分离度 | 无（通用） |
| 6 | **ElevationPreferenceScorer** | 相机仰角是否接近偏好值 | 无（通用） |
| 7 | **TopDownPitchScorer** | 相机俯仰角是否接近目标 | 仅 preferredTopDownPitch > 0 时生效 |
| 8 | **DistanceCompactnessScorer** | 相机距离是否接近理想距离 | Exterior 和 BuildingBlock 有特殊放大系数 |
| 9 | **PlanarDirectionMatchScorer** | 相机方向是否对齐场景平面轴 | 仅 Floor/InteriorZone 且 hasFocusOrientation 时生效 |
| 10 | **FocusVisibilityScorer** | 焦点主体是否被前景遮挡 | 仅 BuildingBlock/EquipmentGroup 时生效 |
| 11 | **PriorityVisibilityScorer** | 各优先级目标的可见性 | Exterior(前5)/BuildingBlock(前2+其余)/EquipmentGroup(全部) |
| 12 | **FacadeProminenceScorer** | 相机是否面对建筑立面 | 仅 BuildingBlock 且 hasFocusOrientation 时生效 |
| 13 | **ViewportLayoutScorer** | 主体在视口中的位置/边距是否合理 | 仅 Exterior/BuildingBlock 时生效 |

### 4.3 评分器依赖关系

```
CompositionEvaluator
  ├─ 核心:
  │   ├─ RuleOfThirdsScorer         (零依赖)
  │   ├─ FillRatioScorer            (依赖 PresentationBoundsProvider)
  │   ├─ BalanceScorer              (零依赖)
  │   └─ DepthLayersScorer          (零依赖)
  └─ 辅助:
      ├─ ScreenSeparationScorer     (零依赖)
      ├─ ElevationPreferenceScorer  (零依赖)
      ├─ TopDownPitchScorer         (零依赖)
      ├─ DistanceCompactnessScorer  (依赖 ScenePresetMode + PresentationBoundsProvider)
      ├─ PlanarDirectionMatchScorer (依赖 ScenePresetMode)
      ├─ FocusVisibilityScorer      (依赖 ScenePresetMode)
      ├─ PriorityVisibilityScorer   (依赖 ScenePresetMode)
      ├─ FacadeProminenceScorer     (依赖 ScenePresetMode)
      └─ ViewportLayoutScorer       (依赖 ScenePresetMode + PresentationBoundsProvider)
```

## 5. 技术约束

| 约束 | 说明 |
|------|------|
| 评分器通过构造器注入依赖 | 不依赖全局状态或 Singleton |
| 每个评分器只做一件事 | 符合单一职责，禁止一个 Scorer 计算多个维度 |
| CompositionDetail 与评分器一一对应 | 新增评分器 → 更新 CompositionDetail |
| CompositionEvaluator 在构造时构建完整评分器列表 | 不在 Evaluate() 热路径中创建对象 |
| 辅助评分器中值 1.0 表示"不适用" | 如 PlanarDirectionMatch 在非 Floor 模式返回 1.0 |

## 6. 验收标准

### AC-SCO-001: 评分器返回值范围

```
GIVEN 任意合法的 SearchContext、CameraPosition、FocusPoint、FOV、SearchVariant
WHEN  调用任意 ICompositionScorer.Score()
THEN  返回值 ∈ [0, 1]
AND   不抛出异常
```

### AC-SCO-002: CompositionEvaluator 评分聚合

```
GIVEN CompositionEvaluator 已构造
WHEN  调用 Evaluate() 传入合法参数
THEN  返回的 score > 0
AND   out details 的 13 个字段均已填充
AND   details.minimumMetric = min(ruleOfThirds, fillRatio, balance, depthLayers)
```

### AC-SCO-003: 核心权重影响评分

```
GIVEN 固定的 Camera Pose
WHEN  使用不同的 CompositionWeights 调用 Evaluate()
THEN  总评分随权重变化而变化
AND   权重更高的核心指标对总评分的贡献更大
```

### AC-SCO-004: 场景不适用时辅助评分器返回 1.0

```
GIVEN PlanarDirectionMatchScorer (仅 Floor/InteriorZone 有效)
WHEN  presetMode = Exterior
THEN  Score() 返回 1.0（不影响总分）
```

### AC-SCO-005: 新增评分器零侵入

```
GIVEN 需要新增一个评分维度
WHEN  实现 ICompositionScorer 并在 CompositionEvaluator 构造器中注册
AND   在 CompositionDetail 中添加对应字段
THEN  不影响已有 13 个评分器的行为
AND   已有测试用例仍然通过
```

## 7. 上下文与引用

### 7.1 相关 Spec
- 父 Spec: [L1-SYS-001](../L1-system/auto-camera-composer.md) — 评分公式见 4.4
- 关联模块: [L2-MOD-003](search-strategy.md) — SearchVariant 提供权重和影响力
- 关联模块: [L2-MOD-004](search-context.md) — SearchContext 为评分器提供场景数据

### 7.2 源代码文件
- `Assets/Scripts/AutoCamera/Scoring/ICompositionScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/CompositionEvaluator.cs`
- `Assets/Scripts/AutoCamera/Scoring/RuleOfThirdsScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/FillRatioScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/BalanceScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/DepthLayersScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/ScreenSeparationScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/ElevationPreferenceScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/TopDownPitchScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/DistanceCompactnessScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/PlanarDirectionMatchScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/FocusVisibilityScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/PriorityVisibilityScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/FacadeProminenceScorer.cs`
- `Assets/Scripts/AutoCamera/Scoring/ViewportLayoutScorer.cs`

### 7.3 相关数据结构
- `Assets/Scripts/AutoCamera/Structs/CompositionDetail.cs`
- `Assets/Scripts/AutoCamera/Structs/CompositionWeights.cs`
