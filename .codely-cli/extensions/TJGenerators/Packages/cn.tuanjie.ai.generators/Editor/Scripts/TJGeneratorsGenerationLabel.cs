using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TJGenerators
{
    /// <summary>
    /// TJGenerators 生成标签管理
    /// </summary>
    public static class TJGeneratorsGenerationLabel
    {
        public const string Label = "TuanjieAI";
        public const string FrontierLabel = "TuanjieAI_Frontier";
        public const string SessionLabelPrefix = "Session_";

        /// <summary>
        /// 为资产添加生成标签
        /// </summary>
        public static void EnableLabel(Object asset)
        {
            if (asset == null)
                return;

            var labels = new List<string>(AssetDatabase.GetLabels(asset));
            if (labels.Contains(Label))
                return;

            labels.Add(Label);
            AssetDatabase.SetLabels(asset, labels.ToArray());
        }

        /// <summary>
        /// 为资产引用添加生成标签
        /// </summary>
        public static void EnableLabel(TJGeneratorsAssetReference assetRef)
        {
            if (assetRef == null || !assetRef.IsValid())
                return;

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetRef.GetPath());
            EnableLabel(asset);
        }

        /// <summary>
        /// 为资产追加 session 标签（sessionId 为空时直接返回）
        /// </summary>
        public static void EnableSessionLabel(Object asset, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || asset == null)
                return;

            string sessionLabel = SessionLabelPrefix + sessionId;
            var labels = new List<string>(AssetDatabase.GetLabels(asset));
            if (labels.Contains(sessionLabel))
                return;

            labels.Add(sessionLabel);
            AssetDatabase.SetLabels(asset, labels.ToArray());
        }

        /// <summary>
        /// 为资产引用追加 session 标签（sessionId 为空时直接返回）
        /// </summary>
        public static void EnableSessionLabel(TJGeneratorsAssetReference assetRef, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || assetRef == null || !assetRef.IsValid())
                return;

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetRef.GetPath());
            EnableSessionLabel(asset, sessionId);
        }

        /// <summary>
        /// 检查资产是否有生成标签
        /// </summary>
        public static bool HasLabel(Object asset)
        {
            if (asset == null)
                return false;

            var labels = AssetDatabase.GetLabels(asset);
            foreach (var label in labels)
            {
                if (label == Label)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 为资产添加 Frontier 序列帧标签
        /// </summary>
        public static void EnableFrontierLabel(Object asset)
        {
            if (asset == null)
                return;

            var labels = new List<string>(AssetDatabase.GetLabels(asset));
            if (!labels.Contains(FrontierLabel))
                labels.Add(FrontierLabel);
            if (!labels.Contains(Label))
                labels.Add(Label);
            AssetDatabase.SetLabels(asset, labels.ToArray());
        }

        /// <summary>
        /// 检查资产是否有 Frontier 序列帧标签
        /// </summary>
        public static bool HasFrontierLabel(Object asset)
        {
            if (asset == null)
                return false;

            var labels = AssetDatabase.GetLabels(asset);
            foreach (var label in labels)
            {
                if (label == FrontierLabel)
                    return true;
            }
            return false;
        }
    }
}
