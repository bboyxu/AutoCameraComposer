using UnityEngine;

namespace AutoCamera
{
    public class DepthLayersScorer : ICompositionScorer
    {
        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            Vector3 forward = (focusPoint - cameraPosition).normalized;
            float minDepth = float.MaxValue;
            float maxDepth = float.MinValue;

            for (int i = 0; i < context.renderers.Count; i++)
            {
                float depth = Vector3.Dot(context.renderers[i].bounds.center - cameraPosition, forward);
                minDepth = Mathf.Min(minDepth, depth);
                maxDepth = Mathf.Max(maxDepth, depth);
            }

            float distance = Vector3.Distance(cameraPosition, focusPoint);
            if (distance <= Mathf.Epsilon)
            {
                return 0f;
            }

            float normalizedDepth = (maxDepth - minDepth) / distance;
            return Mathf.Clamp01(normalizedDepth * 2f);
        }
    }
}
