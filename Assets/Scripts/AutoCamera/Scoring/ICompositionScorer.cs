using UnityEditor.Search;
using UnityEngine;

namespace AutoCamera
{
    public interface ICompositionScorer
    {
        float Score(SearchContext context, Vector3 cameraPosition, Vector3 focusPoint, float fov, SearchVariant variant);
    }
}
