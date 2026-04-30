using System.Collections.Generic;
using UnityEngine;

namespace AutoCamera
{
    public class ScreenSeparationScorer : ICompositionScorer
    {
        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            if (context.separationBounds == null || context.separationBounds.Count <= 1)
            {
                return 1f;
            }

            List<Rect> projectedRects = new List<Rect>();
            for (int i = 0; i < context.separationBounds.Count; i++)
            {
                if (CameraMath.TryProjectBoundsToViewport(context, context.separationBounds[i], cameraPosition, focusPoint, fov, out Rect rect))
                {
                    projectedRects.Add(rect);
                }
            }

            if (projectedRects.Count <= 1)
            {
                return 1f;
            }

            float overlapArea = 0f;
            float referenceArea = 0f;

            for (int i = 0; i < projectedRects.Count; i++)
            {
                referenceArea += Mathf.Max(0.0001f, projectedRects[i].width * projectedRects[i].height);

                for (int j = i + 1; j < projectedRects.Count; j++)
                {
                    overlapArea += CameraMath.RectOverlapArea(projectedRects[i], projectedRects[j]);
                }
            }

            float overlapRatio = overlapArea / Mathf.Max(referenceArea, 0.0001f);
            return Mathf.Clamp01(1f - overlapRatio * 1.8f);
        }
    }
}
