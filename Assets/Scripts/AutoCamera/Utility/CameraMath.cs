using UnityEngine;

namespace AutoCamera
{
    public static class CameraMath
    {
        public static void BuildCameraBasis(Vector3 forward, out Vector3 right, out Vector3 up)
        {
            Vector3 safeUp = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            right = Vector3.Cross(forward, safeUp).normalized;
            up = Vector3.Cross(right, forward).normalized;
        }

        public static float EstimateCameraDistance(float radius, float padding, float minFov, float maxFov)
        {
            float referenceFov = Mathf.Lerp(minFov, maxFov, 0.5f);
            float halfAngle = Mathf.Max(18f, referenceFov * 0.45f);
            return radius * padding / Mathf.Sin(halfAngle * Mathf.Deg2Rad);
        }

        public static float CalculateOptimalFOV(SearchContext context, Bounds bounds, Vector3 cameraPosition, Vector3 focusPoint, float fillTarget)
        {
            Vector3[] corners = GetBoundsCorners(bounds);

            Vector3 forward = (focusPoint - cameraPosition).normalized;
            BuildCameraBasis(forward, out Vector3 right, out Vector3 up);

            float maxHalfAngleH = 0f;
            float maxHalfAngleV = 0f;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 toCorner = (corners[i] - cameraPosition).normalized;
                float angleH = Mathf.Abs(Mathf.Asin(Mathf.Clamp(Vector3.Dot(toCorner, right), -1f, 1f))) * Mathf.Rad2Deg;
                float angleV = Mathf.Abs(Mathf.Asin(Mathf.Clamp(Vector3.Dot(toCorner, up), -1f, 1f))) * Mathf.Rad2Deg;
                maxHalfAngleH = Mathf.Max(maxHalfAngleH, angleH);
                maxHalfAngleV = Mathf.Max(maxHalfAngleV, angleV);
            }

            float safeFillTarget = Mathf.Clamp(fillTarget, 0.4f, 0.95f);
            float fovV = (maxHalfAngleV / safeFillTarget) * 2f;
            float aspect = context.aspect;
            float fovH = (maxHalfAngleH / safeFillTarget) * 2f;
            float fovFromWidth = Mathf.Atan(Mathf.Tan(fovH * 0.5f * Mathf.Deg2Rad) / aspect) * 2f * Mathf.Rad2Deg;
            return Mathf.Max(fovV, fovFromWidth);
        }

        public static bool TryProjectBoundsToViewport(SearchContext context, Bounds bounds, Vector3 cameraPosition, Vector3 focusPoint, float fov, out Rect rect)
        {
            rect = default;

            Vector3 forward = (focusPoint - cameraPosition).normalized;
            BuildCameraBasis(forward, out Vector3 right, out Vector3 up);
            float aspect = context.aspect;
            float tanFov = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

            Vector3[] corners = GetBoundsCorners(bounds);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            int visibleCount = 0;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 toPoint = corners[i] - cameraPosition;
                float depth = Vector3.Dot(toPoint, forward);
                if (depth <= 0.001f)
                {
                    continue;
                }

                float localX = Vector3.Dot(toPoint, right);
                float localY = Vector3.Dot(toPoint, up);
                float viewportX = 0.5f + localX / (depth * tanFov * aspect * 2f);
                float viewportY = 0.5f + localY / (depth * tanFov * 2f);

                minX = Mathf.Min(minX, viewportX);
                maxX = Mathf.Max(maxX, viewportX);
                minY = Mathf.Min(minY, viewportY);
                maxY = Mathf.Max(maxY, viewportY);
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                return false;
            }

            minX = Mathf.Clamp01(minX);
            maxX = Mathf.Clamp01(maxX);
            minY = Mathf.Clamp01(minY);
            maxY = Mathf.Clamp01(maxY);

            if (maxX - minX <= 0.0001f || maxY - minY <= 0.0001f)
            {
                return false;
            }

            rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        public static bool TryGetViewportProjectionMetrics(SearchContext context, Bounds bounds, Vector3 cameraPosition, Vector3 focusPoint, float fov, out ViewportProjectionMetrics metrics)
        {
            metrics = default;

            Vector3 forward = (focusPoint - cameraPosition).normalized;
            BuildCameraBasis(forward, out Vector3 right, out Vector3 up);
            float aspect = context.aspect;
            float tanFov = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

            Vector3[] corners = GetBoundsCorners(bounds);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            int visibleCount = 0;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 toPoint = corners[i] - cameraPosition;
                float depth = Vector3.Dot(toPoint, forward);
                if (depth <= 0.001f)
                {
                    continue;
                }

                float localX = Vector3.Dot(toPoint, right);
                float localY = Vector3.Dot(toPoint, up);
                float viewportX = 0.5f + localX / (depth * tanFov * aspect * 2f);
                float viewportY = 0.5f + localY / (depth * tanFov * 2f);

                minX = Mathf.Min(minX, viewportX);
                maxX = Mathf.Max(maxX, viewportX);
                minY = Mathf.Min(minY, viewportY);
                maxY = Mathf.Max(maxY, viewportY);
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                return false;
            }

            metrics = new ViewportProjectionMetrics
            {
                minX = minX,
                maxX = maxX,
                minY = minY,
                maxY = maxY
            };
            return true;
        }

        public static float RectOverlapArea(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float yMax = Mathf.Min(a.yMax, b.yMax);

            if (xMax <= xMin || yMax <= yMin)
            {
                return 0f;
            }

            return (xMax - xMin) * (yMax - yMin);
        }

        public static bool IsLikelyPartOfFocusSubject(Bounds candidate, Bounds focusBounds)
        {
            Vector3 delta = candidate.center - focusBounds.center;
            Vector3 half = focusBounds.extents;

            bool centerInside =
                Mathf.Abs(delta.x) <= half.x * 1.05f &&
                Mathf.Abs(delta.y) <= half.y * 1.05f &&
                Mathf.Abs(delta.z) <= half.z * 1.05f;

            bool notLargerThanFocus =
                candidate.size.x <= focusBounds.size.x * 1.02f &&
                candidate.size.y <= focusBounds.size.y * 1.02f &&
                candidate.size.z <= focusBounds.size.z * 1.02f;

            return centerInside && notLargerThanFocus;
        }

        public static bool ApproximatelySameBounds(Bounds a, Bounds b)
        {
            return Vector3.SqrMagnitude(a.center - b.center) < 0.0001f &&
                   Vector3.SqrMagnitude(a.size - b.size) < 0.0001f;
        }

        public static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        public static Vector3[] GetBoundsCorners(Bounds bounds)
        {
            return new Vector3[8]
            {
                bounds.min,
                bounds.max,
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)
            };
        }
    }
}
