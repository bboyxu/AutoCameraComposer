```yaml
spec_id: "L2-MOD-004"
title: "搜索上下文构建（Search Context）"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "Context", "Scene Analysis", "Bounding Box"]
parent_spec: "L1-SYS-001"
sub_specs: []
```

# L2 模块级 Spec：搜索上下文构建

## 1. 概述

### 1.1 模块定位

SearchContextBuilder 负责从 Unity 场景的 GameObject 层级中提取结构化的搜索上下文数据。它将分散的 Renderer 信息聚合为一个 SearchContext struct，作为后续所有评分器和搜索策略的统一数据输入。

### 1.2 为什么需要这个模块

搜索算法和评分器需要的是结构化的场景数据（包围盒、中心点、可见性候选集），而不是原始的 GameObject 层级。SearchContext 将场景分析集中在一处，避免每个评分器重复进行渲染体收集和包围盒计算。

### 1.3 范围边界

| 包含 | 不包含 |
|------|--------|
| Renderer 收集与去重 | LOD 级别选择 |
| 包围盒计算（整体/主体/焦点） | 遮挡剔除预处理 |
| 地面平面检测与过滤 | 材质/纹理分析 |
| 焦点方向提取 | 动画骨骼绑定分析 |
| 优先级边界列表构建 | 动态加载/卸载场景 |

## 2. 核心目标

| 编号 | 目标 | 量化指标 |
|------|------|----------|
| G1 | 从 GameObject 列表构建完整 SearchContext | TryBuild() 返回 true 时 context 的 17 个字段均已填充 |
| G2 | 过滤地面平面 | looksLikeGroundPlane 条件正确排除低矮大面积的渲染体 |
| G3 | 正确识别焦点主体 | 焦点包围盒从第一个 targetGroup 提取 |
| G4 | 失败时提供可读错误信息 | error 字符串不为空且有具体原因 |

## 3. 接口契约

### 3.1 SearchContextBuilder

```csharp
public static class SearchContextBuilder
{
    public static bool TryBuild(
        List<GameObject> targetGroups,
        Camera camera,
        out SearchContext context,
        out string error);
}
```

### 3.2 SearchContext 数据结构

```csharp
public struct SearchContext
{
    public struct RenderSample
    {
        public Bounds bounds;   // 渲染体包围盒
        public float weight;    // 视觉权重（= bounds.size.magnitude）
    }

    public Bounds bounds;                   // 全场景包围盒
    public Bounds subjectBounds;            // 主体包围盒（排除地面）
    public Bounds focusBounds;              // 焦点包围盒（第一个 targetGroup）
    public Vector3 center;                  // bounds.center
    public Vector3 subjectCenter;           // subjectBounds.center
    public Vector3 focusCenter;             // focusBounds.center
    public float radius;                    // bounds.extents.magnitude
    public float sceneHeight;               // max(bounds.size.y, 0.01f)
    public float aspect;                    // 相机宽高比
    public List<RenderSample> renderers;     // 渲染体列表（含权重）
    public List<Bounds> separationBounds;   // 分离度候选（排除地面）
    public List<Bounds> priorityBounds;     // 按 targetGroup 分组的优先级边界
    public Vector3 weightedVisualCenter;    // 加权视觉中心
    public bool hasFocusOrientation;        // 是否有焦点方向
    public Vector3 focusForward;            // 焦点 forward 方向
    public Vector3 focusRight;              // 焦点 right 方向
}
```

## 4. 详细行为

### 4.1 Renderer 收集策略

```
GatherRenderers(groups):
    uniqueRenderers = HashSet<Renderer>()
    renderers = []
    for each group in groups:
        if group == null: continue
        groupRenderers = group.GetComponentsInChildren<Renderer>(true)
        for each renderer in groupRenderers:
            if renderer == null: continue
            if uniqueRenderers.Add(renderer):  // 去重
                renderers.Add(renderer)
    return renderers
```

### 4.2 地面平面检测

```
地面平面判定条件（三条同时满足）:
  1. heightRatio < 0.12     — 高度占比极小
  2. footprintRatio > 0.35  — 占地面积大
  3. isNearGround           — 底部接近场景底部（< 5% 全高）

满足条件的渲染体从 subjectBounds 和 separationBounds 中排除。
```

### 4.3 焦点主体提取

```
GetFocusSubjectData(groups):
    focusGroup = 第一个非 null 的 group
    从 focusGroup 收集 Renderer → 计算包围盒 → CalculateSubjectBounds（过滤地面）
    提取 focusGroup.transform.forward 和 right
    hasFocusOrientation = true
```

### 4.4 优先级边界

```
BuildPrioritySubjectBounds(groups):
    为每个 targetGroup:
        收集 Renderer → 计算包围盒 → CalculateSubjectBounds
        添加到 priorityBounds 列表
    如果列表为空，回退为全场景包围盒
```

### 4.5 加权视觉中心

```
CalculateWeightedVisualCenter(renderSamples):
    weightedSum = Σ (renderSample.bounds.center × renderSample.weight)
    totalWeight = Σ renderSample.weight
    return weightedSum / totalWeight
    （提供比几何中心更准确的视觉重心）
```

## 5. 技术约束

| 约束 | 说明 |
|------|------|
| 静态类，无状态 | 所有方法 static，不保存实例数据 |
| Try 模式处理失败 | 返回 bool + out error，不抛异常 |
| 渲染体去重 | 使用 HashSet<Renderer> 防止同一 Renderer 被重复计入 |
| 除零保护 | 所有除法操作前检查分母 > epsilon |
| 包容性包围盒 | 即使场景数据异常，也返回合理的 fallback 值 |

## 6. 验收标准

### AC-CTX-001: 空列表处理

```
GIVEN targetGroups 为 null 或 Count = 0
WHEN  调用 TryBuild()
THEN  返回 false
AND   error 包含 "目标列表为空"
```

### AC-CTX-002: 无 Renderer 处理

```
GIVEN targetGroups 包含一个空 GameObject（无 Renderer 子节点）
WHEN  调用 TryBuild()
THEN  返回 false
AND   error 包含 "Renderer"
```

### AC-CTX-003: 正常场景构建

```
GIVEN targetGroups 包含一个带 MeshRenderer 的 Cube
WHEN  调用 TryBuild()
THEN  返回 true
AND   context.renderers.Count ≥ 1
AND   context.bounds 包围 Cube
AND   context.aspect ≈ 16/9（无 camera 时）或 camera.aspect
AND   error 为空字符串
```

### AC-CTX-004: 地面过滤

```
GIVEN 场景包含一个大型平面（地面）+ 一个塔形建筑
WHEN  调用 TryBuild()
THEN  地面的 Renderer 被包含在 context.bounds 中
AND   地面的 Renderer 从 context.subjectBounds 中排除
AND   塔形建筑的 Renderer 在 context.subjectBounds 中
```

### AC-CTX-005: 多 targetGroup 优先级列表

```
GIVEN targetGroups 包含 3 个 GameObject，每个都有 Renderer
WHEN  调用 TryBuild()
THEN  context.priorityBounds.Count = 3
AND   每个 priorityBound 对应一个 targetGroup
```

### AC-CTX-006: 焦点方向提取

```
GIVEN 第一个 targetGroup 有非默认的 transform.forward
WHEN  调用 TryBuild()
THEN  context.hasFocusOrientation = true
AND   context.focusForward ≈ targetGroup.transform.forward
AND   context.focusRight ≈ targetGroup.transform.right
```

## 7. 上下文与引用

### 7.1 相关 Spec
- 父 Spec: [L1-SYS-001](../L1-system/auto-camera-composer.md) — 数据流见阶段 1
- 关联模块: [L2-MOD-002](composition-scoring.md) — 所有 Scorer 依赖 SearchContext
- 关联模块: [L2-MOD-003](search-strategy.md) — Search() 入口依赖 SearchContext

### 7.2 源代码文件
- `Assets/Scripts/AutoCamera/Context/SearchContextBuilder.cs`
- `Assets/Scripts/AutoCamera/Structs/SearchContext.cs`
