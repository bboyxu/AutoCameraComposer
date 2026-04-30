using System.Collections.Generic;
using UnityEngine;

namespace AutoCamera
{
    public class FeedbackBuilder
    {
        private readonly MetricThresholds _thresholds;

        public FeedbackBuilder(MetricThresholds thresholds)
        {
            _thresholds = thresholds;
        }

        public string Build(float overallScore, CompositionDetail details, int repairPass, EvaluationReport report)
        {
            List<string> failedMetrics = new List<string>();
            if (overallScore < _thresholds.overall)
            {
                failedMetrics.Add("综合分");
            }
            if (details.ruleOfThirds < _thresholds.ruleOfThirds)
            {
                failedMetrics.Add("三分法");
            }
            if (details.fillRatio < _thresholds.fillRatio)
            {
                failedMetrics.Add("填充率");
            }
            if (details.balance < _thresholds.balance)
            {
                failedMetrics.Add("平衡性");
            }
            if (details.depthLayers < _thresholds.depthLayers)
            {
                failedMetrics.Add("层次感");
            }

            string weakestMetric = GetWeakestMetricName(details);
            string status = failedMetrics.Count == 0 ? "已达到专业阈值" : "仍有指标未完全达标";
            string missing = failedMetrics.Count == 0 ? "无" : string.Join("、", failedMetrics);

            return
                status +
                $" | 通过 {report.passedMetricCount}/5 项" +
                $" | 最弱项: {weakestMetric}" +
                $" | 修复轮次: {repairPass}" +
                $" | 未达标项: {missing}" +
                $"\n综合分 {overallScore:F3} / 三分法 {details.ruleOfThirds:F3} / 填充率 {details.fillRatio:F3} / 平衡性 {details.balance:F3} / 层次感 {details.depthLayers:F3}" +
                $" / 分离度 {details.screenSeparation:F3} / 俯视偏好 {details.elevationPreference:F3} / 最终俯视姿态 {details.topDownPitch:F3} / 距离紧凑度 {details.distanceCompactness:F3} / 平面方向匹配 {details.planarDirectionMatch:F3}" +
                $" / 主体可见性 {details.focusVisibility:F3} / 优先级可见性 {details.priorityVisibility:F3} / 立面突出 {details.facadeProminence:F3}";
        }

        private static string GetWeakestMetricName(CompositionDetail details)
        {
            float minValue = details.ruleOfThirds;
            string name = "三分法";

            if (details.fillRatio < minValue)
            {
                minValue = details.fillRatio;
                name = "填充率";
            }

            if (details.balance < minValue)
            {
                minValue = details.balance;
                name = "平衡性";
            }

            if (details.depthLayers < minValue)
            {
                name = "层次感";
            }

            return name;
        }
    }
}
