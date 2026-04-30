using System.Collections.Generic;
using UnityEngine;

namespace AutoCamera
{
    public struct SearchContext
    {
        public struct RenderSample
        {
            public Bounds bounds;
            public float weight;
        }

        public Bounds bounds;
        public Bounds subjectBounds;
        public Bounds focusBounds;
        public Vector3 center;
        public Vector3 subjectCenter;
        public Vector3 focusCenter;
        public float radius;
        public float sceneHeight;
        public float aspect;
        public List<RenderSample> renderers;
        public List<Bounds> separationBounds;
        public List<Bounds> priorityBounds;
        public Vector3 weightedVisualCenter;
        public bool hasFocusOrientation;
        public Vector3 focusForward;
        public Vector3 focusRight;
    }
}
