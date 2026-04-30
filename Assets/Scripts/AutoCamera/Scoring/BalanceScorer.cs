using UnityEngine;

namespace AutoCamera
{
    public class BalanceScorer : ICompositionScorer
    {
        public float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant)
        {
            Vector3 forward = (focusPoint - cameraPosition).normalized;
            CameraMath.BuildCameraBasis(forward, out Vector3 right, out Vector3 up);

            float leftWeight = 0f;
            float rightWeight = 0f;
            float topWeight = 0f;
            float bottomWeight = 0f;

            for (int i = 0; i < context.renderers.Count; i++)
            {
                SearchContext.RenderSample renderer = context.renderers[i];
                float weight = renderer.weight;
                Vector3 offset = renderer.bounds.center - focusPoint;
                float x = Vector3.Dot(offset, right);
                float y = Vector3.Dot(offset, up);

                if (x < 0f)
                {
                    leftWeight += weight;
                }
                else
                {
                    rightWeight += weight;
                }

                if (y < 0f)
                {
                    bottomWeight += weight;
                }
                else
                {
                    topWeight += weight;
                }
            }

            float totalWeight = leftWeight + rightWeight + topWeight + bottomWeight;
            if (totalWeight <= Mathf.Epsilon)
            {
                return 0f;
            }

            float horizontalBalance = 1f - Mathf.Abs(leftWeight - rightWeight) / totalWeight;
            float verticalBalance = 1f - Mathf.Abs(topWeight - bottomWeight) / totalWeight;
            return (horizontalBalance + verticalBalance) * 0.5f;
        }
    }
}
