using UnityEngine;

namespace AutoCamera
{
    public struct CameraPose
    {
        public Vector3 position;
        public Vector3 lookAt;
        public float fov;
        public float score;
        public float acceptanceRank;
        public CompositionDetail details;
        public CompositionWeights weights;
        public float fillTarget;
        public float padding;
        public int repairPass;
    }
}
