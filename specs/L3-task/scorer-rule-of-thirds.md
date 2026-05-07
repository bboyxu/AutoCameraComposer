```yaml
spec_id: "L3-TASK-002"
title: "RuleOfThirds 评分器实现规范"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "Scorer", "Rule of Thirds", "Task"]
parent_spec: "L2-MOD-002"
```

# L3 任务级 Spec：RuleOfThirds 评分器

## 1. 任务描述

实现 `RuleOfThirdsScorer`，评估画面视觉重心是否接近三分法（Rule of Thirds）的 4 个交点。三分法将画面水平和垂直各三等分，形成 4 个美学交点：(1/3, 1/3), (2/3, 1/3), (1/3, 2/3), (2/3, 2/3)。

## 2. 详细行为

### 2.1 算法步骤

```
Score(context, cameraPosition, focusPoint, fov, variant):
    [1] 构建相机坐标系 (forward, right, up)
    [2] 计算 weightedVisualCenter 相对于 focusPoint 的偏移
    [3] 将偏移投影到屏幕空间 (screenX, screenY)
    [4] 计算 (screenX, screenY) 到最近三分交点的归一化距离
    [5] 返回 1 - 距离/√2
```

### 2.2 屏幕投影公式

```
screenX = 0.5 + dot(offset, right) / (distance × tan(fov/2) × aspect)
screenY = 0.5 + dot(offset, up)    / (distance × tan(fov/2))
screenX = clamp(screenX, 0, 1)
screenY = clamp(screenY, 0, 1)
```

### 2.3 三分交点

```
idealX = [1/3, 2/3]
idealY = [1/3, 2/3]

minDistance = min( sqrt((screenX-ix)² + (screenY-iy)²) ) for ix in idealX, iy in idealY

score = 1 - minDistance / sqrt(2)   // sqrt(2) 是对角线最大距离
```

## 3. 接口签名

```csharp
public class RuleOfThirdsScorer : ICompositionScorer
{
    // 零依赖构造器
    public float Score(SearchContext context, Vector3 cameraPosition,
                       Vector3 focusPoint, float fov, SearchVariant variant);
}
```

## 4. 验收标准

### AC-RT-001: 完美三分

```
GIVEN weightedVisualCenter 投影到恰好 (1/3, 1/3)
WHEN  调用 Score()
THEN  返回值 ≈ 1.0（容差 0.001）
```

### AC-RT-002: 最差偏离

```
GIVEN weightedVisualCenter 投影到 (0, 0)（最远角）
WHEN  调用 Score()
THEN  返回值 ≈ 1 - 1/3×√2/√2 = 0.667（容差 0.01）
```

### AC-RT-003: 分数范围

```
GIVEN 任意合法输入
THEN  0 ≤ Score() ≤ 1
```

## 5. 上下文与引用

- 父 Spec: [L2-MOD-002](../L2-module/composition-scoring.md) — 评分系统总览
- 源代码: `Assets/Scripts/AutoCamera/Scoring/RuleOfThirdsScorer.cs`
