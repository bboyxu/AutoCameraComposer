using UnityEngine;

namespace AutoCamera
{
    [System.Serializable]
    public struct CompositionWeights
    {
        public float ruleOfThirds;
        public float fillRatio;
        public float balance;
        public float depthLayers;

        public void Normalize()
        {
            float sum = ruleOfThirds + fillRatio + balance + depthLayers;
            if (sum <= Mathf.Epsilon)
            {
                ruleOfThirds = 0.25f;
                fillRatio = 0.25f;
                balance = 0.25f;
                depthLayers = 0.25f;
                return;
            }

            ruleOfThirds /= sum;
            fillRatio /= sum;
            balance /= sum;
            depthLayers /= sum;
        }
    }
}
