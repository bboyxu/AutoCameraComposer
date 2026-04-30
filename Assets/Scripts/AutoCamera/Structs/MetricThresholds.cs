using UnityEngine;

namespace AutoCamera
{
    [System.Serializable]
    public struct MetricThresholds
    {
        [Range(0.5f, 0.95f)]
        public float overall;

        [Range(0.5f, 0.95f)]
        public float ruleOfThirds;

        [Range(0.5f, 0.95f)]
        public float fillRatio;

        [Range(0.5f, 0.95f)]
        public float balance;

        [Range(0.2f, 0.9f)]
        public float depthLayers;

        public static MetricThresholds Default()
        {
            return new MetricThresholds
            {
                overall = 0.78f,
                ruleOfThirds = 0.72f,
                fillRatio = 0.76f,
                balance = 0.72f,
                depthLayers = 0.48f
            };
        }
    }
}
