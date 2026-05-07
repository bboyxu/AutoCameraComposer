```yaml
spec_id: "L2-MOD-005"
title: "验收评估引擎（Acceptance Evaluation）"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "Evaluation", "Acceptance", "Threshold", "Feedback"]
parent_spec: "L1-SYS-001"
sub_specs: []
```

# L2 模块级 Spec：验收评估引擎

## 1. 概述

### 1.1 模块定位

验收评估引擎负责将 CompositionEvaluator 产出的评分与预设阈值比较，判定相机位姿是否达标。同时提供排名函数使搜索策略能在多个候选位姿中选出最优，并将评估结果转化为人类可读的诊断信息。

### 1.2 范围边界

| 包含 | 不包含 |
|------|--------|
| 5 项核心指标阈值比较 | 用户自定义阈值 |
| 6 种场景特定验收规则 | 自动调整阈值 |
| acceptanceRank 排名计算 | 多目标帕累托前沿 |
| 人类可读反馈字符串 | 多语言反馈 |

## 2. 核心目标

| 编号 | 目标 |
|------|------|
| G1 | 精确判定 5 项指标达标状态 |
| G2 | 排名函数可区分优劣构图 |
| G3 | 6 种场景各有专属 bonus/penalty 规则 |
| G4 | 反馈信息包含：状态、通过数、最弱项、修复轮次、未达标项、各项评分 |

## 3. 关键类

```csharp
public class AcceptanceEvaluator
{
    public AcceptanceEvaluator(ScenePresetMode presetMode, MetricThresholds thresholds);
    public float CalculateAcceptanceRank(float overallScore, CompositionDetail details);
    public bool PassesAllThresholds(float overallScore, CompositionDetail details);
    public EvaluationReport BuildEvaluationReport(float overallScore, CompositionDetail details);
}

public class FeedbackBuilder
{
    public FeedbackBuilder(MetricThresholds thresholds);
    public string Build(float overallScore, CompositionDetail details,
                        int repairPass, EvaluationReport report);
}
```

## 4. 详细行为

### 4.1 验收排名公式

```
CalculateAcceptanceRank:
    passCount=0; positiveMargin=0; deficit=0
    ScoreThreshold(overallScore,             thresholds.overall, ...)
    ScoreThreshold(details.ruleOfThirds,     thresholds.ruleOfThirds, ...)
    ScoreThreshold(details.fillRatio,        thresholds.fillRatio, ...)
    ScoreThreshold(details.balance,          thresholds.balance, ...)
    ScoreThreshold(details.depthLayers,      thresholds.depthLayers, ...)
    ApplyPresetSpecificRules(details, positiveMargin, deficit)
    return passCount*100 + positiveMargin*20 - deficit*30 + details.minimumMetric*10 + overallScore
```

### 4.2 场景特定规则（汇总）

| 场景 | 规则数量 | 关键检查项 |
|------|---------|-----------|
| Exterior | 1 | priorityVisibility ≥ 0.92（惩罚 × 5.2） |
| BuildingBlock | 6 | priorityVis(0.90), focusVis(0.88), pitch(0.84), facade(0.82), layout(0.82), distance(0.84) |
| Floor | 1 | topDownPitch ≥ 0.92（惩罚 × 4.5） |
| InteriorZone | 1 | topDownPitch ≥ 0.88（惩罚 × 3.8） |
| EquipmentGroup | 2 | priorityVis ≥ 0.94, topDownPitch ≥ 0.70 |
| RandomAesthetic | 0 | 无特殊规则 |

### 4.3 反馈字符串格式

```
{状态} | 通过 {N}/5 项 | 最弱项: {name} | 修复轮次: {pass} | 未达标项: {list}
综合分 {score:F3} / 三分法 {T:F3} / 填充率 {F:F3} / 平衡性 {B:F3} / 层次感 {D:F3}
/ 分离度 {S:F3} / 俯视偏好 {E:F3} / 最终俯视姿态 {P:F3} / 距离紧凑度 {C:F3}
/ 平面方向匹配 {M:F3} / 主体可见性 {V:F3} / 优先级可见性 {PV:F3}
/ 立面突出 {FP:F3}
```

## 5. 验收标准

### AC-EVA-001: 全通过检测

```
GIVEN overallScore 和全部 5 项指标均 ≥ 对应阈值
WHEN  调用 PassesAllThresholds()
THEN  返回 true
```

### AC-EVA-002: 单项失败检测

```
GIVEN depthLayers < threshold.depthLayers，其余 4 项通过
WHEN  调用 PassesAllThresholds()
THEN  返回 false
```

### AC-EVA-003: 排名函数方向正确

```
GIVEN 候选 A 的 5 项指标均高于候选 B
WHEN  计算两者的 acceptanceRank
THEN  rank(A) > rank(B)
```

### AC-EVA-004: 排名函数通过数优先

```
GIVEN 候选 A 5 项全通过但分数较低，候选 B 4 项通过但分数较高
WHEN  计算两者的 acceptanceRank
THEN  rank(A) > rank(B)（passCount 项的权重 100 大于其他项）
```

### AC-EVA-005: 验收报告完整性

```
GIVEN 任意评分
WHEN  调用 BuildEvaluationReport()
THEN  返回的 report 包含 7 个字段的正确值
AND   passedMetricCount = Σ 各指标通过数
AND   allPassed = (passedMetricCount == 5)
```

### AC-EVA-006: BuildingBlock 场景规则触发

```
GIVEN presetMode = BuildingBlock
AND   facadeProminence = 0.81（低于阈值 0.82）
WHEN  计算 acceptanceRank
THEN  deficit 增加 (0.82 - 0.81) × 3.5
```

### AC-EVA-007: 反馈信息含弱项

```
GIVEN 任意的 CompositionDetail
WHEN  FeedbackBuilder.Build()
THEN  返回字符串包含 "最弱项: {正确的弱项名称}"
```

## 6. 上下文与引用

- 父 Spec: [L1-SYS-001](../L1-system/auto-camera-composer.md)
- 源代码: `Assets/Scripts/AutoCamera/Evaluation/AcceptanceEvaluator.cs`
- 源代码: `Assets/Scripts/AutoCamera/Evaluation/FeedbackBuilder.cs`
- 数据结构: `Assets/Scripts/AutoCamera/Structs/EvaluationReport.cs`
- 数据结构: `Assets/Scripts/AutoCamera/Structs/MetricThresholds.cs`
