```yaml
spec_id: "L1-SYS-001"
title: "AutoCameraComposer — 3D 相机自动构图系统"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "3D Camera", "Auto Composition", "Search Optimization"]
parent_spec: ~
sub_specs:
  - "L2-MOD-001"
  - "L2-MOD-002"
  - "L2-MOD-003"
  - "L2-MOD-004"
  - "L2-MOD-005"
  - "L2-MOD-006"
```

# L1 系统级 Spec：AutoCameraComposer

## 1. 概述（Overview）

### 1.1 一句话定位

给定一组 3D 模型节点（GameObject），自动搜索并应用**最优相机位姿**（位置 + 注视点 + 视场角），使画面在 5 项专业构图指标上达到预设阈值。

### 1.2 为什么需要这个系统

建筑可视化、工业数字孪生等场景中，人工为每种场景模式（外景、楼栋、楼层、室内区域、设备组）手动调整相机是低效且不稳定的。本系统将摄影构图原则量化为可计算的评分函数，通过并行搜索 + 自适应修复机制，在秒级时间内产出专业级构图结果。

### 1.3 范围边界

| 包含（In Scope） | 不包含（Out of Scope） |
|---|---|
| 静态场景的相机位姿自动求解 | 实时动画路径规划 |
| 6 种预设模式 + 4 种构图风格 | 用户自定义评分公式的 GUI 编辑器 |
| 多轮自适应修复（短板加权 + 搜索空间扩展） | 视频序列自动剪辑 |
| 编辑器实时预览 + Play 模式平滑过渡 | 多相机协同构图 |
| 随机美学多样化探索 | 基于机器学习的偏好学习 |
| 5 项核心指标 + 9 项辅助指标评分 | VR/AR 双目相机适配 |

## 2. 核心目标（Core Goals）

| 编号 | 目标 | 量化指标 | 验证方式 |
|------|------|----------|----------|
| G1 | 自动找到满足专业阈值的相机位姿 | 5 项核心指标全部达标 | AcceptanceEvaluator.PassesAllThresholds() |
| G2 | 未达标时自动修复 | 最多 3~4 轮修复，每轮权重与搜索空间自适应 | resultRepairPass 字段 |
| G3 | 支持 6 种建筑可视化场景一键切换 | 每种场景有独立预设配置 | PresetRegistry 覆盖全部 6 个 ScenePresetMode |
| G4 | 编辑器实时预览 | 参数变更后自动重算 | livePreview 机制 |
| G5 | Play 模式平滑相机过渡 | 位置/旋转/FOV 插值动画 | SmoothStep 插值 |
| G6 | 搜索过程可取消 | 任意时刻取消不崩溃、不残留 | CancellationToken 全链路穿透 |
| G7 | 搜索过程可并行 | 水平方向采样在多线程并行执行 | Parallel.For |

## 3. 系统架构（Architecture）

### 3.1 模块分区

```
AutoCamera/
├── Context/        # L2-MOD-004: SearchContextBuilder（静态，无状态）
├── Enums/          # 枚举定义（CompositionStyle, SearchQuality, ScenePresetMode）
├── Evaluation/     # L2-MOD-005: AcceptanceEvaluator + FeedbackBuilder
├── Presets/        # L2-MOD-001: IScenePreset 接口 + 6 实现 + PresetRegistry
├── Scoring/        # L2-MOD-002: ICompositionScorer 接口 + 4 核心 + 9 辅助
├── Search/         # L2-MOD-003: ProfessionalSearchStrategy + Variant/Weight/Focus Builder
├── Structs/        # 纯数据结构（12 个 struct，零行为）
└── Utility/        # L2-MOD-006: CameraMath（静态）+ PresentationBoundsProvider
```

### 3.2 顶层依赖图

```
AutoCameraComposer (MonoBehaviour)          ← 入口层
  └─► ProfessionalSearchStrategy            ← 编排层
        ├─► WeightBuilder                   ← 权重适配
        ├─► SearchVariantBuilder            ← 变体生成
        ├─► CompositionEvaluator            ← 评分聚合
        │     ├─► ICompositionScorer × 13
        │     └─► CompositionDetail
        ├─► AcceptanceEvaluator             ← 验收判定
        ├─► FocusPointBuilder               ← 焦点计算
        ├─► FocusAnchorBuilder (static)     ← 锚点策略
        ├─► PresentationBoundsProvider      ← 边界选择
        └─► CameraMath (static)             ← 数学工具
```

### 3.3 架构原则

| 原则 | 规约 | 量化证据 |
|------|------|----------|
| **Spec First** | 系统设计先于编码 | 本文档 = 系统级 Spec |
| **接口隔离** | 每个接口 ≤ 1 方法, ≤ 10 属性 | `ICompositionScorer`: 1 方法; `IScenePreset`: 10 属性 |
| **构造器注入** | 业务类通过构造函数接收依赖，不 new 内部依赖 | 100% 业务类遵循 |
| **数据行为分离** | `Structs/` 目录全是纯数据 struct，无业务逻辑 | 12 个 struct，仅 `CompositionWeights.Normalize()` 有方法 |
| **MonoBehaviour 薄化** | MonoBehaviour 只做参数暴露 + 编排调用 + 动画/显示 | ≤ 350 行 |
| **可取消性** | 全搜索链路支持 CancellationToken | 3 层嵌套循环均含 token 检查 |

## 4. 详细行为（Detailed Behavior）

### 4.1 主流程：从输入到相机就位

```
[阶段 1] SearchContextBuilder.TryBuild(targetGroups, camera)
           → 收集 Renderer → 计算包围盒 → 过滤地面 → 构建 SearchContext
           → 失败时返回 error 字符串，不抛异常

[阶段 2] ProfessionalSearchStrategy.Search(context, token)
           ┌─ for repairPass = 0..maxRepairPasses:
           │   [2a] WeightBuilder.BuildAdaptiveWeights() → 短板加权
           │   [2b] SearchVariantBuilder.Build() → 3×3 fill/padding 组合 + 扩展
           │   ┌─ for each variant:
           │   │   [2c] Parallel.For 水平采样:
           │   │         for 垂直环 × 焦点锚点:
           │   │           FocusPointBuilder.Build()
           │   │           CameraMath.CalculateOptimalFOV()
           │   │           CompositionEvaluator.Evaluate() → score+details
           │   │           AcceptanceEvaluator.CalculateAcceptanceRank()
           │   │   取 variant 内最优
           │   [2h] AcceptanceEvaluator.PassesAllThresholds() → 早停
           └─ 返回全局最优 CameraPose

[阶段 3] ApplyResult()
           → AcceptanceEvaluator.BuildEvaluationReport()
           → FeedbackBuilder.Build()
           → ApplyPoseAnimated() | ApplyPoseImmediate()
```

### 4.2 场景模式行为矩阵

| 场景模式 | 构图风格 | 搜索质量 | 修复轮数 | 过渡时间 | 核心阈值 (O/T/F/B/D) | 核心权重 (T/F/B/D) |
|----------|---------|---------|---------|---------|----------------------|-------------------|
| **Exterior** | WideScene | High | 4 | 0.9s | .80/.72/.74/.80/.66 | .20/.32/.18/.30 |
| **BuildingBlock** | Balanced | High | 4 | 0.7s | .80/.74/.78/.80/.60 | .25/.25/.20/.30 |
| **Floor** | TopDown | High | 3 | 0.6s | .81/.68/.84/.84/.40 | .20/.24/.24/.32 |
| **InteriorZone** | TopDown | High | 4 | 0.6s | .80/.72/.82/.78/.46 | .20/.24/.24/.32 |
| **EquipmentGroup** | HeroProduct | High | 4 | 0.5s | .84/.76/.88/.76/.52 | .30/.26/.16/.28 |
| **RandomAesthetic** | Balanced | High | 4 | 0.55s | .82/.74/.80/.76/.52 | .25/.25/.20/.30 |

> 缩写: O=Overall, T=RuleOfThirds, F=FillRatio, B=Balance, D=DepthLayers

### 4.3 搜索空间参数

| 质量等级 | 水平采样 | 垂直环 | 修复增量（每轮） |
|----------|----------|--------|-----------------|
| Draft | 36 | 3 | +24 / +1 |
| Standard | 72 | 4 | +24 / +1 |
| High | 120 | 6 | +24 / +1 |

### 4.4 评分公式

```
totalScore = Σ(4 核心指标 × 归一化权重) + Σ(9 辅助指标 × 影响力系数)

核心（权重归一化到 1.0）:
  ruleOfThirds × w[0] + fillRatio × w[1] + balance × w[2] + depthLayers × w[3]

辅助（影响力独立可调，范围 [0, 1]）:
  + screenSeparation × variant.separationInfluence
  + elevationPreference × variant.elevationInfluence
  + topDownPitch × variant.topDownPitchInfluence
  + distanceCompactness × variant.distanceInfluence
  + planarDirectionMatch × variant.planarDirectionInfluence
  + focusVisibility × variant.focusVisibilityInfluence
  + priorityVisibility × variant.priorityVisibilityInfluence
  + facadeProminence × variant.facadeInfluence
  + viewportLayout × variant.layoutInfluence
```

### 4.5 自适应修复机制

```
规则: 每轮修复执行三步操作:
  1. 权重自适应 — 短板指标权重递增 boost = 0.45 + repairPass × 0.15
  2. 搜索空间扩展 — 仰角 ±8°/轮, FOV ±6°/轮, 采样 +24/+1 每轮
  3. 参数探索扩展 — fill 和 padding 做 ±shift 的 3×3 组合 = 9 个 variant

早停条件: AcceptanceEvaluator.PassesAllThresholds() = true
```

### 4.6 验收排名公式

```
acceptanceRank = passCount × 100 + positiveMargin × 20 - deficit × 30
               + minMetric × 10 + overallScore

其中:
  - passCount: 5 项核心指标中达标数（0~5）
  - positiveMargin: 各项超出阈值的超额之和
  - deficit: 各项低于阈值的差额之和
  - minMetric: CompositionDetail.minimumMetric（最短板）
```

## 5. 技术约束（Technical Constraints）

| 类别 | 约束 | 理由 |
|------|------|------|
| **语言/平台** | C# / Unity 2021.3+ | 目标运行环境 |
| **异步库** | UniTask (Cysharp.Threading.Tasks) | 支持 CancellationToken 的 async/await |
| **并行** | System.Threading.Tasks.Parallel.For | 水平方向采样可安全并行 |
| **文件行数上限** | 单文件 ≤ 350 行 | 单一职责，防止 God Class |
| **命名空间** | 统一 `namespace AutoCamera` | 避免跨目录类型污染 |
| **数据容器** | 纯数据用 struct，有行为用 class | 值语义 vs 引用语义正确选择 |
| **依赖方向** | Structs ← Utility ← Scoring/Search/Evaluation ← MonoBehaviour | 单向无循环 |
| **Tooltip** | 所有 public Inspector 字段必须标注 | AI 和人类理解参数含义 |

## 6. 验收标准（Acceptance Criteria）

### AC-SYS-001: 基础构图正确性

```
GIVEN 场景中包含至少一个带 Renderer 的 GameObject
WHEN  调用 ComposeCamera()
THEN  系统计算出的 resultPosition 不是 Vector3.zero
AND   resultScore > 0
AND   resultFOV 在 [10, 90] 范围内
AND   resultLookAt 指向场景中心区域
```

### AC-SYS-002: 空目标处理

```
GIVEN targetGroups 列表为空或所有元素为 null
WHEN  调用 ComposeCamera()
THEN  不抛出异常
AND   resultFeedback 包含明确的错误信息
AND   Debug.LogWarning 被调用
```

### AC-SYS-003: 搜索可取消

```
GIVEN 一个正在进行的 ComposeCamera() 调用
WHEN  在搜索过程中触发 CancellationToken.Cancel()
THEN  搜索在 100ms 内终止
AND   不残留后台线程
AND   OnDisable 时 CancellationTokenSource 被正确释放
```

### AC-SYS-004: 预设一致性

```
GIVEN PresetRegistry 中注册了 6 个 IScenePreset
WHEN  遍历每个预设
THEN  每个预设的 5 个阈值均在 [0.2, 0.95] 范围内
AND   每个预设的 4 个权重之和约等于 1.0（容差 0.001）
AND   每个预设的 minElevation < maxElevation
AND   每个预设的 minFov < maxFov
```

### AC-SYS-005: 自适应修复达到阈值

```
GIVEN autoRepair = true, maxRepairPasses ≥ 3
AND   目标场景来自包含多个 Renderer 的标准测试场景
WHEN  调用 ComposeCamera()
THEN  如果首轮未达标，后续轮次的搜索空间大于首轮
AND   resultRepairPass 记录最终使用的修复轮次
AND   修复轮次不会超过 maxRepairPasses
```

### AC-SYS-006: 编辑器实时预览

```
GIVEN livePreview = true
AND   targetGroups 非空
WHEN  在 Inspector 中修改 compositionStyle 或 targetThresholds
THEN  自动触发 ComposeForEditorPreview()
AND   SceneView 自动刷新显示新结果
```

### AC-SYS-007: Play 模式平滑过渡

```
GIVEN Application.isPlaying = true
AND   transitionDuration = 0.8f
WHEN  调用 ComposeCamera()
THEN  相机在 0.8 秒内从当前位置平滑过渡到目标位姿
AND   过渡使用 SmoothStep 缓动曲线
AND   过渡期间 transform.position、rotation、camera.fieldOfView 连续变化
```

### AC-SYS-008: 数据完整性

```
GIVEN ComposeCamera() 完成执行
WHEN  检查 AutoCameraComposer 的结果字段
THEN  resultDetails 的 13 个评分字段均已填充（非默认值）
AND   resultEvaluation 包含正确的通过/未通过判定
AND   resultEffectiveWeights 包含归一化的权重
AND   resultFeedback 包含 human-readable 的诊断信息
```

## 7. 数据模型速览（Data Model）

| Struct | 字段数 | Serializable | 用途 |
|--------|--------|-------------|------|
| `CameraPose` | 8 | ✗ | 搜索最优结果 |
| `CompositionDetail` | 14 | ✓ | 13 项评分 + 最短板 |
| `CompositionWeights` | 4 | ✓ | 核心四项权重 |
| `EvaluationReport` | 7 | ✓ | 逐项验收通过 |
| `MetricThresholds` | 5 | ✓ | 5 项验收阈值 |
| `SearchContext` | 17 | ✗ | 场景上下文（含 RenderSample 嵌套） |
| `SearchVariant` | 23 | ✗ | 搜索变体全参数 |
| `StyleSettings` | 10 | ✗ | 风格基础参数 |
| `PresetRuleSettings` | 14 | ✗ | 预设高级规则 |
| `RandomAestheticProfile` | 22 | ✗ | 随机美学全配置 |
| `QualitySampleSettings` | 2 | ✗ | 采样密度 |
| `ViewportProjectionMetrics` | 4 | ✗ | 视口投影边界 |

## 8. 上下文与引用（Context & References）

### 8.1 代码库映射

| Spec 模块 | 对应源代码目录 |
|-----------|--------------|
| L2-MOD-001 (Presets) | `Assets/Scripts/AutoCamera/Presets/` |
| L2-MOD-002 (Scoring) | `Assets/Scripts/AutoCamera/Scoring/` |
| L2-MOD-003 (Search) | `Assets/Scripts/AutoCamera/Search/` |
| L2-MOD-004 (Context) | `Assets/Scripts/AutoCamera/Context/` |
| L2-MOD-005 (Evaluation) | `Assets/Scripts/AutoCamera/Evaluation/` |
| L2-MOD-006 (Utility) | `Assets/Scripts/AutoCamera/Utility/` |

### 8.2 关键接口

- `ICompositionScorer` — 评分器统一接口（见 L2-MOD-002）
- `IScenePreset` — 场景预设统一接口（见 L2-MOD-001）

### 8.3 外部依赖

- `Cysharp.Threading.Tasks` (UniTask) — 异步操作支持
- `UnityEngine` — Unity 核心运行时
- `UnityEditor` — 编辑器 Inspector 自定义（仅 Editor 脚本）

### 8.4 子 Spec 索引

| Spec ID | 模块名称 | 文件 |
|---------|---------|------|
| L2-MOD-001 | 场景预设系统 | [scene-presets.md](../L2-module/scene-presets.md) |
| L2-MOD-002 | 构图评分系统 | [composition-scoring.md](../L2-module/composition-scoring.md) |
| L2-MOD-003 | 搜索策略引擎 | [search-strategy.md](../L2-module/search-strategy.md) |
| L2-MOD-004 | 搜索上下文构建 | [search-context.md](../L2-module/search-context.md) |
| L2-MOD-005 | 验收评估引擎 | [acceptance-evaluation.md](../L2-module/acceptance-evaluation.md) |
| L2-MOD-006 | 相机数学工具 | [camera-math-utils.md](../L2-module/camera-math-utils.md) |
