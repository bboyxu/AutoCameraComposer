```yaml
spec_id: "L2-MOD-003"
title: "搜索策略引擎（Search Strategy）"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "Search", "Optimization", "Parallel", "Adaptive"]
parent_spec: "L1-SYS-001"
sub_specs: []
```

# L2 模块级 Spec：搜索策略引擎

## 1. 概述

### 1.1 模块定位

搜索策略引擎是系统的核心编排层。它在球面坐标系中离散采样相机位姿，通过多轮自适应修复机制逼近最优解。支持并行搜索、可取消、早停优化。

### 1.2 为什么需要这个模块

3D 空间中的相机位姿搜索是连续空间优化问题。粗暴的网格搜索计算量不可接受，而梯度下降难以处理多个不可微的评分函数。本模块采用离散采样 + 自适应修复的策略，在性能和质量之间取得平衡。

### 1.3 范围边界

| 包含 | 不包含 |
|------|--------|
| 球面坐标系离散采样 | 连续优化算法（梯度下降） |
| 多轮自适应修复机制 | 贝叶斯优化 |
| 并行水平搜索 | GPU 加速 |
| SearchVariant 生成（3×3 参数组合） | 参数自动调优（AutoML） |
| WeightBuilder 自适应权重 | 用户自定义修复策略 |

## 2. 核心目标

| 编号 | 目标 | 量化指标 |
|------|------|----------|
| G1 | 在合理时间内返回有效结果 | Draft < 1s, Standard < 3s, High < 8s（典型场景） |
| G2 | 自适应修复提升结果质量 | 修复后评分 ≥ 修复前评分 |
| G3 | 达到阈值时早停 | PassesAllThresholds() = true 时立即停止 |
| G4 | 并行搜索不产生竞态 | Parallel.For 中无共享写状态 |

## 3. 关键类

### 3.1 ProfessionalSearchStrategy

```csharp
public class ProfessionalSearchStrategy
{
    // 构造器注入所有依赖（7 参数）
    public ProfessionalSearchStrategy(
        ScenePresetMode presetMode, CompositionStyle compositionStyle,
        SearchQuality searchQuality, MetricThresholds thresholds,
        bool autoRepair, int maxRepairPasses,
        RandomAestheticProfile? randomProfile = null);

    // 主搜索入口，返回全局最优 CameraPose
    public CameraPose Search(SearchContext context, CancellationToken token);
}
```

### 3.2 SearchVariantBuilder

```csharp
public class SearchVariantBuilder
{
    // 构造器注入（4 参数）
    // 生成当前修复轮的搜索变体列表（3×3 fill/padding 组合 = 9 个 variant）
    public List<SearchVariant> Build(CompositionWeights weights, int repairPass);
}
```

### 3.3 WeightBuilder

```csharp
public class WeightBuilder
{
    // 构造器注入（3 参数）
    // 生成基础权重（根据预设或随机美学）
    public CompositionWeights BuildBaseWeights(CompositionStyle style);
    // 生成自适应权重（短板加权）
    public CompositionWeights BuildAdaptiveWeights(
        CompositionWeights baseWeights, CompositionDetail previousBest, int repairPass);
}
```

### 3.4 FocusPointBuilder

```csharp
public class FocusPointBuilder
{
    // 构造器注入（2 参数）
    // 根据场景模式、方向、锚点、偏移计算注视点
    public Vector3 Build(SearchContext context, Vector3 direction,
                         Vector2 anchor, float offsetScale);
}
```

### 3.5 FocusAnchorBuilder (static)

```csharp
public static class FocusAnchorBuilder
{
    // 根据修复轮和场景模式返回焦点锚点数组
    // 首轮 5~7 个锚点，修复轮 7~9 个锚点
    public static Vector2[] Build(int repairPass, ScenePresetMode presetMode);
}
```

## 4. 详细行为

### 4.1 搜索算法伪代码

```
function Search(context, token):
    globalBest.acceptanceRank = -∞
    baseWeights = WeightBuilder.BuildBaseWeights(style)
    baseWeights.Normalize()

    repairPassCount = autoRepair ? (maxRepairPasses + 1) : 1

    for repairPass in 0..repairPassCount-1:
        token.ThrowIfCancellationRequested()
        adaptiveWeights = WeightBuilder.BuildAdaptiveWeights(baseWeights, previousBest, repairPass)
        variants = SearchVariantBuilder.Build(adaptiveWeights, repairPass)  // 9 个 variant

        passBest.acceptanceRank = -∞
        for each variant in variants:
            variantBest = SearchVariantCandidates(context, variant, token)
            if variantBest.acceptanceRank > passBest.acceptanceRank:
                passBest = variantBest

        if passBest.acceptanceRank > globalBest.acceptanceRank:
            globalBest = passBest

        previousBest = globalBest.details

        if AcceptanceEvaluator.PassesAllThresholds(globalBest.score, globalBest.details):
            break  // 早停

    return globalBest
```

### 4.2 单 variant 搜索（SearchVariantCandidates）

```
function SearchVariantCandidates(context, variant, token):
    anchors = FocusAnchorBuilder.Build(variant.repairPass, presetMode)
    orbitCenter = boundsProvider.GetCenter(context)
    framingBounds = boundsProvider.GetBounds(context)
    orbitRadius = boundsProvider.GetRadius(context)

    perHorizontalBest[] = new CameraPose[variant.horizontalSamples]

    Parallel.For(0, variant.horizontalSamples, h ->
        theta = (h / horizontalSamples) * 2π

        for v in 0..variant.verticalRings-1:
            phi = nonlinear_interp(v, verticalRings, elevationCurveExponent)
            direction = spherical_to_cartesian(theta, phi)
            distance = EstimateCameraDistance(orbitRadius, padding, minFov, maxFov)
            cameraPosition = orbitCenter + direction * distance

            for each anchor in anchors:
                focusPoint = FocusPointBuilder.Build(context, direction, anchor, focusOffsetScale)
                fov = CalculateOptimalFOV(framingBounds, cameraPosition, focusPoint, fillTarget)
                fov = Clamp(fov, minFov, maxFov)
                score, details = CompositionEvaluator.Evaluate(...)
                acceptanceRank = AcceptanceEvaluator.CalculateAcceptanceRank(score, details)

                if acceptanceRank > localBest.acceptanceRank:
                    localBest = (cameraPosition, focusPoint, fov, score, details, ...)

        perHorizontalBest[h] = localBest
    )

    return max(perHorizontalBest)  // 取 acceptanceRank 最大的
```

### 4.3 自适应修复权重公式

```
BuildAdaptiveWeights(baseWeights, previousBest, repairPass):
    if repairPass == 0:
        return baseWeights  // 首轮不改权重

    boost = 0.45 + repairPass * 0.15
    baseWeights.ruleOfThirds += max(0, threshold_ruleOfThirds - previousBest.ruleOfThirds) * boost
    baseWeights.fillRatio    += max(0, threshold_fillRatio    - previousBest.fillRatio) * boost
    baseWeights.balance      += max(0, threshold_balance      - previousBest.balance) * boost
    baseWeights.depthLayers  += max(0, threshold_depthLayers  - previousBest.depthLayers) * boost
    baseWeights.Normalize()
    return baseWeights
```

### 4.4 搜索空间扩展（每轮修复）

```
修复轮 0:
  horizontalSamples = baseQuality (36/72/120)
  verticalRings     = baseQuality (3/4/6)
  elevationRange    = [minElevation + bias, maxElevation + bias]
  fovRange          = [minFov, maxFov]
  fillCandidates    = [baseFill - shift, baseFill, baseFill + shift]
  paddingCandidates = [basePadding - shift, basePadding, basePadding + shift]

修复轮 N (N > 0):
  horizontalSamples += N * 24
  verticalRings     += N
  elevationRange    扩展 ± N*8°
  fovRange          扩展 ± N*6°
  fillShift         += N * 0.03
  paddingShift      += N * 0.05
  focusShift        += N * 0.04
  preferredElevation += N * 4°
```

## 5. 技术约束

| 约束 | 说明 |
|------|------|
| CancellationToken 每层循环检查 | 3 层循环开头都有 token.ThrowIfCancellationRequested() |
| Parallel.For 只修改线程本地变量 | perHorizontalBest[h] 各线程独立写入 |
| 聚焦点构建依赖 ScenePresetMode | Floor/InteriorZone 使用降低的注视点 |
| 展示边界选择依赖 ScenePresetMode | BuildingBlock/Floor/InteriorZone 用 focusBounds |
| 锚点策略依赖 ScenePresetMode | BuildingBlock 有 5 个方向锚点，Floor 有 5 个水平锚点 |

## 6. 验收标准

### AC-SEA-001: 首轮搜索返回有效结果

```
GIVEN 有效的 SearchContext 和默认配置
WHEN  调用 Search() 且 autoRepair = false
THEN  返回的 CameraPose.score > 0
AND   CameraPose.position 不是 Vector3.zero
AND   CameraPose.repairPass = 0
```

### AC-SEA-002: 自适应修复不降低质量

```
GIVEN autoRepair = true, maxRepairPasses = 3
WHEN  调用 Search()
THEN  最终结果 score ≥ 首轮结果 score
AND   resultRepairPass ≥ 0
AND   resultRepairPass ≤ maxRepairPasses
```

### AC-SEA-003: 早停条件生效

```
GIVEN 场景简单, 首轮即可达到阈值
AND   autoRepair = true
WHEN  调用 Search()
THEN  resultRepairPass = 0
AND   没有执行第二轮搜索的开销（可通过 variant 数量验证）
```

### AC-SEA-004: 搜索可取消

```
GIVEN 正在执行 Search()
WHEN  CancellationToken 被取消
THEN  抛出 OperationCanceledException
AND   全局状态没有被破坏
```

### AC-SEA-005: 并行搜索结果确定性

```
GIVEN 固定的 SearchContext 和配置
AND   相同的 random seed
WHEN  调用 Search() 两次
THEN  两次返回的 CameraPose 基本一致（浮点精度误差内）
```

### AC-SEA-006: SearchVariant 参数合法性

```
GIVEN SearchVariantBuilder.Build()
WHEN  生成 variant 列表
THEN  每个 variant 的 fillTarget ∈ [0.48, 0.88]
AND   padding ∈ [0.95, 1.90]
AND   minElevation < maxElevation
AND   minFov < maxFov
AND   列表长度 = 9（3 fill × 3 padding）
```

## 7. 上下文与引用

### 7.1 相关 Spec
- 父 Spec: [L1-SYS-001](../L1-system/auto-camera-composer.md) — 数据流见 4.1~4.5
- 关联模块: [L2-MOD-001](scene-presets.md) — 预设参数驱动 SearchVariantBuilder
- 关联模块: [L2-MOD-002](composition-scoring.md) — CompositionEvaluator
- 关联模块: [L2-MOD-005](acceptance-evaluation.md) — AcceptanceEvaluator 提供排名和早停判定

### 7.2 源代码文件
- `Assets/Scripts/AutoCamera/Search/ProfessionalSearchStrategy.cs`
- `Assets/Scripts/AutoCamera/Search/SearchVariantBuilder.cs`
- `Assets/Scripts/AutoCamera/Search/WeightBuilder.cs`
- `Assets/Scripts/AutoCamera/Search/FocusPointBuilder.cs`
- `Assets/Scripts/AutoCamera/Search/FocusAnchorBuilder.cs`
- `Assets/Scripts/AutoCamera/Search/RandomAestheticProfileBuilder.cs`
