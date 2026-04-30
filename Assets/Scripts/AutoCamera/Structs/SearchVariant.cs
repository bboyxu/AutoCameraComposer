namespace AutoCamera
{
    public struct SearchVariant
    {
        public float minElevation;
        public float maxElevation;
        public float minFov;
        public float maxFov;
        public float fillTarget;
        public float padding;
        public float focusOffsetScale;
        public CompositionWeights weights;
        public int horizontalSamples;
        public int verticalRings;
        public int repairPass;
        public float preferredElevation;
        public float elevationInfluence;
        public float separationInfluence;
        public float focusVisibilityInfluence;
        public float priorityVisibilityInfluence;
        public float facadeInfluence;
        public float layoutInfluence;
        public float planarDirectionInfluence;
        public float distanceInfluence;
        public float preferredDistanceScale;
        public float enforcedMinElevation;
        public float enforcedMaxElevation;
        public float topDownPitchInfluence;
        public float preferredTopDownPitch;
        public float elevationCurveExponent;
    }
}
