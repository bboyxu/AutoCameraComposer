namespace AutoCamera
{
    [System.Serializable]
    public struct EvaluationReport
    {
        public bool allPassed;
        public int passedMetricCount;
        public bool overallPassed;
        public bool ruleOfThirdsPassed;
        public bool fillRatioPassed;
        public bool balancePassed;
        public bool depthLayersPassed;
    }
}
