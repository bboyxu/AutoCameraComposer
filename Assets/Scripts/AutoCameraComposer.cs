using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AutoCamera
{
    public class AutoCameraComposer : MonoBehaviour
    {
        [Header("基础设置")]
        [Tooltip("需要被自动构图的模型组。每个元素建议是一个父节点，脚本会收集其子节点下的全部 Renderer。")]
        public List<GameObject> targetGroups = new List<GameObject>();

        [HideInInspector]
        public ScenePresetMode scenePresetMode = ScenePresetMode.RandomAesthetic;

        [Tooltip("构图风格。Balanced 更通用，HeroProduct 更像产品海报，WideScene 更偏空间展示，TopDown 偏俯视。")]
        public CompositionStyle compositionStyle = CompositionStyle.Balanced;

        [Tooltip("搜索质量。越高代表采样越密、自动修复越积极，但计算更慢。")]
        public SearchQuality searchQuality = SearchQuality.Standard;

        [Tooltip("在 Inspector 调参时是否自动实时重算并直接预览。")]
        public bool livePreview = true;

        [Tooltip("开启后会在首轮结果不达标时继续做多轮自我修复搜索。")]
        public bool autoRepair = true;

        [Range(1, 4)]
        [Tooltip("自我修复最大轮数。轮数越多，系统越会扩大搜索空间去补弱项。")]
        public int maxRepairPasses = 3;

        [Tooltip("专业构图的最低验收门槛。脚本会尽量让 5 个指标都达到这些目标值。")]
        public MetricThresholds targetThresholds = default;

        [Range(0f, 3f)]
        [Tooltip("运行时过渡时间。编辑器预览时会直接应用，Play 模式下才使用平滑动画。")]
        public float transitionDuration = 0.8f;

        [Header("结果")]
        [Tooltip("自动求解后的相机位置。")]
        public Vector3 resultPosition;

        [Tooltip("自动求解后的注视点。")]
        public Vector3 resultLookAt;

        [Tooltip("自动求解后的视场角。")]
        public float resultFOV = 50f;

        [Tooltip("综合评分。")]
        public float resultScore;

        [Tooltip("5 个指标中的最短板，越高越稳定。")]
        public float resultMinimumMetric;

        [Tooltip("各维度明细。")]
        public CompositionDetail resultDetails;

        [Tooltip("是否通过专业阈值验收，以及通过了多少项。")]
        public EvaluationReport resultEvaluation;

        [Tooltip("自动搜索后最终采用的有效权重。")]
        public CompositionWeights resultEffectiveWeights;

        [Tooltip("最终采用的目标填充率。")]
        public float resultFillTarget;

        [Tooltip("最终采用的留白系数。")]
        public float resultPadding;

        [Tooltip("最终用了第几轮修复策略。0 表示首轮就找到结果。")]
        public int resultRepairPass;

        [TextArea(3, 8)]
        [Tooltip("自动评估、校验和修复后的文字反馈。")]
        public string resultFeedback;

        private Camera _camera;
        private bool _isAnimating;
        private Vector3 _animStartPos;
        private Quaternion _animStartRot;
        private float _animStartFOV;
        private float _animTime;
        private RandomAestheticProfileBuilder _randomAestheticBuilder;
        private RandomAestheticProfile? _currentRandomProfile;
        private CancellationTokenSource _composeCts;
        private int _composeVersion;

        private void Reset()
        {
            targetThresholds = MetricThresholds.Default();
        }

        public void ApplyPreset(ScenePresetMode mode)
        {
            IScenePreset preset = PresetRegistry.GetPreset(mode);
            if (preset == null) return;

            scenePresetMode = mode;
            compositionStyle = preset.Style;
            searchQuality = preset.Quality;
            livePreview = preset.LivePreview;
            autoRepair = preset.AutoRepair;
            maxRepairPasses = preset.MaxRepairPasses;
            transitionDuration = preset.TransitionDuration;
            targetThresholds = preset.Thresholds;
        }

        public void ApplyPresetExteriorScene() => ApplyPreset(ScenePresetMode.Exterior);
        public void ApplyPresetBuildingBlock() => ApplyPreset(ScenePresetMode.BuildingBlock);
        public void ApplyPresetBuildingFloor() => ApplyPreset(ScenePresetMode.Floor);
        public void ApplyPresetInteriorZone() => ApplyPreset(ScenePresetMode.InteriorZone);
        public void ApplyPresetEquipmentGroup() => ApplyPreset(ScenePresetMode.EquipmentGroup);

        public void ApplyPresetRandomAesthetic()
        {
            ApplyPreset(ScenePresetMode.RandomAesthetic);
            if (_randomAestheticBuilder == null)
            {
                _randomAestheticBuilder = new RandomAestheticProfileBuilder();
            }
            _randomAestheticBuilder.IncrementSeedVersion();
        }

        private void Awake()
        {
            if (targetThresholds.overall <= 0f)
            {
                targetThresholds = MetricThresholds.Default();
            }

            EnsureCamera();
            _randomAestheticBuilder = new RandomAestheticProfileBuilder();
        }

        private void Update()
        {
            if (!_isAnimating || _camera == null)
            {
                return;
            }

            if (transitionDuration <= Mathf.Epsilon)
            {
                _isAnimating = false;
                return;
            }

            _animTime += Time.deltaTime / transitionDuration;
            if (_animTime >= 1f)
            {
                _animTime = 1f;
                _isAnimating = false;
            }

            float t = CameraMath.SmoothStep(_animTime);
            transform.position = Vector3.Lerp(_animStartPos, resultPosition, t);
            transform.rotation = Quaternion.Slerp(_animStartRot, Quaternion.LookRotation(resultLookAt - resultPosition), t);
            _camera.fieldOfView = Mathf.Lerp(_animStartFOV, resultFOV, t);
        }

        [ContextMenu("Compose Camera")]
        public void ComposeCamera()
        {
            ComposeInternalAsync(false).Forget();
        }

        public void ComposeForEditorPreview()
        {
            ComposeInternalAsync(true).Forget();
        }

        private async UniTaskVoid ComposeInternalAsync(bool immediatePreview)
        {
            _composeCts?.Cancel();
            _composeCts?.Dispose();
            _composeCts = new CancellationTokenSource();
            CancellationToken token = _composeCts.Token;
            int composeVersion = ++_composeVersion;

            if (!SearchContextBuilder.TryBuild(targetGroups, _camera, out SearchContext context, out string error))
            {
                resultFeedback = error;
                Debug.LogWarning("[AutoCameraComposer] " + error);
                return;
            }

            PrepareRandomProfileIfNeeded(context);

            CameraPose bestPose;
            try
            {
                ProfessionalSearchStrategy strategy = CreateSearchStrategy();
                bestPose = await UniTask.RunOnThreadPool(() => strategy.Search(context, token), cancellationToken: token);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested || composeVersion != _composeVersion)
            {
                return;
            }

            ApplyResult(bestPose, immediatePreview);
        }

        private void ComposeInternal(bool immediatePreview)
        {
            if (!SearchContextBuilder.TryBuild(targetGroups, _camera, out SearchContext context, out string error))
            {
                resultFeedback = error;
                Debug.LogWarning("[AutoCameraComposer] " + error);
                return;
            }

            PrepareRandomProfileIfNeeded(context);

            ProfessionalSearchStrategy strategy = CreateSearchStrategy();
            CameraPose bestPose = strategy.Search(context, CancellationToken.None);
            ApplyResult(bestPose, immediatePreview);
        }

        private void PrepareRandomProfileIfNeeded(SearchContext context)
        {
            if (scenePresetMode == ScenePresetMode.RandomAesthetic && _randomAestheticBuilder != null)
            {
                _currentRandomProfile = _randomAestheticBuilder.Build(context);
            }
            else
            {
                _currentRandomProfile = null;
            }
        }

        private ProfessionalSearchStrategy CreateSearchStrategy()
        {
            return new ProfessionalSearchStrategy(
                scenePresetMode,
                compositionStyle,
                searchQuality,
                targetThresholds,
                autoRepair,
                maxRepairPasses,
                _currentRandomProfile);
        }

        private void ApplyResult(CameraPose pose, bool immediatePreview)
        {
            EnsureCamera();

            resultPosition = pose.position;
            resultLookAt = pose.lookAt;
            resultFOV = pose.fov;
            resultScore = pose.score;
            resultDetails = pose.details;
            resultMinimumMetric = pose.details.minimumMetric;
            resultEffectiveWeights = pose.weights;
            resultFillTarget = pose.fillTarget;
            resultPadding = pose.padding;
            resultRepairPass = pose.repairPass;

            AcceptanceEvaluator evaluator = new AcceptanceEvaluator(scenePresetMode, targetThresholds);
            resultEvaluation = evaluator.BuildEvaluationReport(pose.score, pose.details);

            FeedbackBuilder feedbackBuilder = new FeedbackBuilder(targetThresholds);
            resultFeedback = feedbackBuilder.Build(pose.score, pose.details, pose.repairPass, resultEvaluation);

            bool useImmediate = immediatePreview || !Application.isPlaying || transitionDuration <= Mathf.Epsilon;
            if (useImmediate)
            {
                ApplyPoseImmediate();
            }
            else
            {
                ApplyPoseAnimated();
            }

            Debug.Log("[AutoCameraComposer] " + resultFeedback);
        }

        private void EnsureCamera()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (_camera == null)
            {
                _camera = gameObject.AddComponent<Camera>();
            }
        }

        private void ApplyPoseAnimated()
        {
            EnsureCamera();

            _animStartPos = transform.position;
            _animStartRot = transform.rotation;
            _animStartFOV = _camera.fieldOfView;
            _animTime = 0f;
            _isAnimating = true;
        }

        public void ApplyPoseImmediate()
        {
            EnsureCamera();

            _isAnimating = false;
            transform.position = resultPosition;
            transform.LookAt(resultLookAt);
            _camera.fieldOfView = resultFOV;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(transform);
                UnityEditor.EditorUtility.SetDirty(_camera);
            }
#endif
        }

        private void OnDisable()
        {
            _composeCts?.Cancel();
            _composeCts?.Dispose();
            _composeCts = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!SearchContextBuilder.TryBuild(targetGroups, _camera, out SearchContext context, out _))
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(context.bounds.center, context.bounds.size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(resultPosition, resultLookAt);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(resultPosition, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(resultLookAt, 0.2f);
        }
    }
}
