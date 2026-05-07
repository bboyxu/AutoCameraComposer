```yaml
spec_id: "L2-MOD-006"
title: "相机数学工具（Camera Math Utils）"
version: "1.0"
author: "Spec Author (从生产代码逆向提取)"
date: "2026-05-07"
status: "stable"
tags: ["Unity", "Math", "Camera", "Projection", "Utility"]
parent_spec: "L1-SYS-001"
sub_specs: []
```

# L2 模块级 Spec：相机数学工具

## 1. 概述

### 1.1 模块定位

提供相机位姿相关的基础数学运算：坐标系统构建、距离估算、FOV 计算、视口投影、碰撞检测、包围盒操作。所有方法为纯函数，无状态，无副作用。

### 1.2 范围边界

| 包含 | 不包含 |
|------|--------|
| 相机坐标系构建 | 物理相机模拟 |
| FOV 反算 | 镜头畸变 |
| 包围盒视口投影 | 三角形级遮挡检测 |
| 包围盒工具函数 | 视锥体裁剪 |

## 2. 核心目标

| 编号 | 目标 |
|------|------|
| G1 | 所有数学运算正确且无除零 |
| G2 | 万向锁安全向量处理 |
| G3 | 投影函数正确处理相机后方物体 |
| G4 | 包围盒操作使用 8 顶点精确采样 |

## 3. API 清单

### 3.1 坐标系

```csharp
// 根据 forward 向量构建正交基 (right, up)
// 处理万向锁：forward 接近 ±up 时使用 forward 作为 safeUp
public static void BuildCameraBasis(Vector3 forward, out Vector3 right, out Vector3 up);
```

### 3.2 距离/FOV

```csharp
// 估算相机距离：radius × padding / sin(referenceFov × 0.45 / 2)
public static float EstimateCameraDistance(float radius, float padding, float minFov, float maxFov);

// 根据包围盒 8 顶点投影反算最优 FOV
// fillTarget 越低 → FOV 越大；aspect 考虑水平/垂直双方向
public static float CalculateOptimalFOV(SearchContext context, Bounds bounds,
    Vector3 cameraPosition, Vector3 focusPoint, float fillTarget);
```

### 3.3 视口投影

```csharp
// 将包围盒投影到视口坐标 [0,1]²
// 返回 false 表示所有顶点在相机后方或无有效投影
public static bool TryProjectBoundsToViewport(SearchContext context, Bounds bounds,
    Vector3 cameraPosition, Vector3 focusPoint, float fov, out Rect rect);

// 返回无 clamp 的投影边界（用于布局分析）
public static bool TryGetViewportProjectionMetrics(SearchContext context, Bounds bounds,
    Vector3 cameraPosition, Vector3 focusPoint, float fov, out ViewportProjectionMetrics metrics);
```

### 3.4 碰撞/包含

```csharp
// 计算两个 Rect 的重叠面积
public static float RectOverlapArea(Rect a, Rect b);

// 判断候选包围盒是否可能是焦点主体的一部分
// 条件：中心在焦点内（1.05倍容差）且不大于焦点（1.02倍容差）
public static bool IsLikelyPartOfFocusSubject(Bounds candidate, Bounds focusBounds);

// 判断两个包围盒是否近似相同（中心/尺寸平方差 < 0.0001）
public static bool ApproximatelySameBounds(Bounds a, Bounds b);
```

### 3.5 插值与工具

```csharp
// SmoothStep 缓动函数：t²(3 - 2t)
public static float SmoothStep(float t);

// 返回包围盒的 8 个顶点
public static Vector3[] GetBoundsCorners(Bounds bounds);
```

## 4. 关键算法

### 4.1 FOV 反算

```
CalculateOptimalFOV(context, bounds, cameraPosition, focusPoint, fillTarget):
    forward = (focusPoint - cameraPosition).normalized
    BuildCameraBasis(forward, right, up)
    corners = GetBoundsCorners(bounds)

    maxHalfAngleH = maxHalfAngleV = 0
    for each corner in corners:
        toCorner = (corner - cameraPosition).normalized
        angleH = |asin(dot(toCorner, right))| * Rad2Deg
        angleV = |asin(dot(toCorner, up))|    * Rad2Deg
        maxHalfAngleH = max(maxHalfAngleH, angleH)
        maxHalfAngleV = max(maxHalfAngleV, angleV)

    safeFillTarget = clamp(fillTarget, 0.4, 0.95)
    fovV = (maxHalfAngleV / safeFillTarget) * 2
    fovFromWidth = atan(tan(fovH / 2) / aspect) * 2
    return max(fovV, fovFromWidth)
```

## 5. 技术约束

| 约束 | 说明 |
|------|------|
| 全 static 方法 | 无实例状态 |
| 无 Unity 场景依赖 | 不访问 GameObject/Camera.main |
| 除零保护 | 所有分母检查 > epsilon |
| 万向锁安全 | forward 接近 (0,±1,0) 时自动选择 safeUp |

## 6. 验收标准

### AC-MTH-001: BuildCameraBasis 万向锁安全

```
GIVEN forward = (0, 1, 0)
WHEN  调用 BuildCameraBasis()
THEN  right 和 up 均为有效单位向量
AND   不产生 NaN 或异常
```

### AC-MTH-002: EstimateCameraDistance 合理性

```
GIVEN radius = 10, padding = 1.2, fovRange = [30, 60]
WHEN  调用 EstimateCameraDistance()
THEN  返回值 > 10（距离应大于半径）
```

### AC-MTH-003: CalculateOptimalFOV 填充率反向关系

```
GIVEN 相同场景，fillTarget 分别为 0.4 和 0.95
WHEN  调用 CalculateOptimalFOV()
THEN  fillTarget=0.4 的 FOV > fillTarget=0.95 的 FOV
```

### AC-MTH-004: TryProjectBoundsToViewport 后方检测

```
GIVEN 包围盒完全在相机后方
WHEN  调用 TryProjectBoundsToViewport()
THEN  返回 false
```

### AC-MTH-005: TryProjectBoundsToViewport 前方检测

```
GIVEN 包围盒在相机前方且完全在视锥内
WHEN  调用 TryProjectBoundsToViewport()
THEN  返回 true
AND   rect 在 [0, 1]² 范围内
```

### AC-MTH-006: RectOverlapArea 边界情况

```
GIVEN 两个完全不相交的 Rect
THEN  返回 0
GIVEN 两个完全重叠的 Rect
THEN  返回面积
GIVEN 部分重叠的 Rect
THEN  0 < result < area
```

### AC-MTH-007: SmoothStep 边界值

```
GIVEN t = 0 → 返回 0
GIVEN t = 1 → 返回 1
GIVEN t = 0.5 → 返回 0.5
```

### AC-MTH-008: GetBoundsCorners 数量

```
GIVEN 任意 Bounds
THEN  返回长度为 8 的数组
```

## 7. 上下文与引用

- 父 Spec: [L1-SYS-001](../L1-system/auto-camera-composer.md)
- 源代码: `Assets/Scripts/AutoCamera/Utility/CameraMath.cs`
- 源代码: `Assets/Scripts/AutoCamera/Utility/PresentationBoundsProvider.cs`
- 数据结构: `Assets/Scripts/AutoCamera/Structs/ViewportProjectionMetrics.cs`
