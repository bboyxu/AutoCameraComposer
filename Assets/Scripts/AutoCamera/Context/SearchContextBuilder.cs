using System.Collections.Generic;
using UnityEngine;

namespace AutoCamera
{
    public static class SearchContextBuilder
    {
        public static bool TryBuild(List<GameObject> targetGroups, Camera camera, out SearchContext context, out string error)
        {
            context = default;
            error = string.Empty;

            if (targetGroups == null || targetGroups.Count == 0)
            {
                error = "目标列表为空，请至少指定一个目标对象。";
                return false;
            }

            List<Renderer> renderers = GatherRenderers(targetGroups);
            if (renderers.Count == 0)
            {
                error = "所有目标对象中都没有找到 Renderer。";
                return false;
            }

            float aspect = camera != null ? camera.aspect : 16f / 9f;
            List<SearchContext.RenderSample> renderSamples = BuildRenderSamples(renderers);

            Bounds bounds = CalculateBounds(renderers);
            Bounds subjectBounds = CalculateSubjectBounds(renderers, bounds);
            GetFocusSubjectData(targetGroups, bounds, out Bounds focusBounds, out Vector3 focusForward, out Vector3 focusRight, out bool hasFocusOrientation);
            Vector3 weightedVisualCenter = CalculateWeightedVisualCenter(renderSamples);

            context = new SearchContext
            {
                bounds = bounds,
                subjectBounds = subjectBounds,
                focusBounds = focusBounds,
                center = bounds.center,
                subjectCenter = subjectBounds.center,
                focusCenter = focusBounds.center,
                radius = bounds.extents.magnitude,
                sceneHeight = Mathf.Max(bounds.size.y, 0.01f),
                aspect = aspect,
                renderers = renderSamples,
                separationBounds = BuildSeparationBounds(renderers, bounds),
                priorityBounds = BuildPrioritySubjectBounds(targetGroups, bounds),
                weightedVisualCenter = weightedVisualCenter,
                hasFocusOrientation = hasFocusOrientation,
                focusForward = focusForward,
                focusRight = focusRight
            };

            return true;
        }

        private static List<Renderer> GatherRenderers(List<GameObject> groups)
        {
            HashSet<Renderer> uniqueRenderers = new HashSet<Renderer>();
            List<Renderer> renderers = new List<Renderer>();

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] == null)
                {
                    continue;
                }

                Renderer[] groupRenderers = groups[i].GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < groupRenderers.Length; r++)
                {
                    if (groupRenderers[r] == null || !uniqueRenderers.Add(groupRenderers[r]))
                    {
                        continue;
                    }

                    renderers.Add(groupRenderers[r]);
                }
            }

            return renderers;
        }

        private static Bounds CalculateBounds(List<Renderer> renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Bounds CalculateSubjectBounds(List<Renderer> renderers, Bounds fullBounds)
        {
            List<Bounds> subjectCandidates = new List<Bounds>();
            float fullHeight = Mathf.Max(fullBounds.size.y, 0.01f);
            float fullFootprint = Mathf.Max(fullBounds.size.x * fullBounds.size.z, 0.01f);
            float baseY = fullBounds.min.y;

            for (int i = 0; i < renderers.Count; i++)
            {
                Bounds item = renderers[i].bounds;
                float heightRatio = item.size.y / fullHeight;
                float footprintRatio = (item.size.x * item.size.z) / fullFootprint;
                bool isNearGround = Mathf.Abs(item.min.y - baseY) < fullHeight * 0.05f;
                bool looksLikeGroundPlane = heightRatio < 0.12f && footprintRatio > 0.35f && isNearGround;

                if (!looksLikeGroundPlane)
                {
                    subjectCandidates.Add(item);
                }
            }

            if (subjectCandidates.Count == 0)
            {
                return fullBounds;
            }

            Bounds subjectBounds = subjectCandidates[0];
            for (int i = 1; i < subjectCandidates.Count; i++)
            {
                subjectBounds.Encapsulate(subjectCandidates[i]);
            }

            return subjectBounds;
        }

        private static List<Bounds> BuildSeparationBounds(List<Renderer> renderers, Bounds fullBounds)
        {
            List<Bounds> candidates = new List<Bounds>();
            float fullHeight = Mathf.Max(fullBounds.size.y, 0.01f);
            float fullFootprint = Mathf.Max(fullBounds.size.x * fullBounds.size.z, 0.01f);
            float baseY = fullBounds.min.y;

            for (int i = 0; i < renderers.Count; i++)
            {
                Bounds item = renderers[i].bounds;
                float heightRatio = item.size.y / fullHeight;
                float footprintRatio = (item.size.x * item.size.z) / fullFootprint;
                bool isNearGround = Mathf.Abs(item.min.y - baseY) < fullHeight * 0.05f;
                bool looksLikeGroundPlane = heightRatio < 0.12f && footprintRatio > 0.35f && isNearGround;

                if (!looksLikeGroundPlane)
                {
                    candidates.Add(item);
                }
            }

            if (candidates.Count == 0)
            {
                for (int i = 0; i < renderers.Count; i++)
                {
                    candidates.Add(renderers[i].bounds);
                }
            }

            return candidates;
        }

        private static void GetFocusSubjectData(
            List<GameObject> groups,
            Bounds fallbackBounds,
            out Bounds focusBounds,
            out Vector3 focusForward,
            out Vector3 focusRight,
            out bool hasFocusOrientation)
        {
            focusBounds = fallbackBounds;
            focusForward = Vector3.forward;
            focusRight = Vector3.right;
            hasFocusOrientation = false;

            GameObject focusGroup = null;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] != null)
                {
                    focusGroup = groups[i];
                    break;
                }
            }

            if (focusGroup == null)
            {
                return;
            }

            Renderer[] focusRenderers = focusGroup.GetComponentsInChildren<Renderer>(true);
            if (focusRenderers.Length > 0)
            {
                List<Renderer> focusList = new List<Renderer>(focusRenderers);
                Bounds fullGroupBounds = CalculateBounds(focusList);
                focusBounds = CalculateSubjectBounds(focusList, fullGroupBounds);
            }

            focusForward = focusGroup.transform.forward;
            focusRight = focusGroup.transform.right;
            hasFocusOrientation = true;
        }

        private static List<Bounds> BuildPrioritySubjectBounds(List<GameObject> groups, Bounds fallbackBounds)
        {
            List<Bounds> priorityBounds = new List<Bounds>();

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] == null)
                {
                    continue;
                }

                Renderer[] renderers = groups[i].GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    continue;
                }

                List<Renderer> rendererList = new List<Renderer>(renderers);
                Bounds groupBounds = CalculateBounds(rendererList);
                priorityBounds.Add(CalculateSubjectBounds(rendererList, groupBounds));
            }

            if (priorityBounds.Count == 0)
            {
                priorityBounds.Add(fallbackBounds);
            }

            return priorityBounds;
        }

        private static Vector3 CalculateWeightedVisualCenter(List<SearchContext.RenderSample> renderSamples)
        {
            Vector3 center = Vector3.zero;
            float totalWeight = 0f;

            for (int i = 0; i < renderSamples.Count; i++)
            {
                float weight = renderSamples[i].weight;
                center += renderSamples[i].bounds.center * weight;
                totalWeight += weight;
            }

            return totalWeight > Mathf.Epsilon ? center / totalWeight : renderSamples[0].bounds.center;
        }

        private static List<SearchContext.RenderSample> BuildRenderSamples(List<Renderer> renderers)
        {
            List<SearchContext.RenderSample> samples = new List<SearchContext.RenderSample>(renderers.Count);
            for (int i = 0; i < renderers.Count; i++)
            {
                Bounds bounds = renderers[i].bounds;
                samples.Add(new SearchContext.RenderSample
                {
                    bounds = bounds,
                    weight = bounds.size.magnitude
                });
            }

            return samples;
        }
    }
}
