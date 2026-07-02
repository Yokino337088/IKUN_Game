#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace TJGenerators.Utils
{
    /// <summary>
    /// 图片压缩工具类
    /// 支持 PNG、JPG、JPEG 格式的压缩
    /// 
    /// 压缩策略：
    /// - PNG：无损格式，不做压缩（保留原始数据，保留透明通道）
    /// - JPG/JPEG：重新编码降低质量
    /// </summary>
    public static class ImageCompressor
    {
        /// <summary>
        /// 压缩图片
        /// </summary>
        /// <param name="imageData">原始图片字节数据</param>
        /// <param name="quality">压缩质量 (1-100)，数值越低压缩率越高</param>
        /// <param name="originalExtension">原始文件扩展名 (.png, .jpg, .jpeg)</param>
        /// <returns>压缩结果：包含压缩后的字节数据和新的扩展名</returns>
        public static CompressionResult Compress(byte[] imageData, int quality, string originalExtension)
        {
            if (imageData == null || imageData.Length == 0)
            {
                TJLog.LogWarning("[ImageCompressor] 输入图片数据为空");
                return new CompressionResult
                {
                    data = imageData,
                    extension = originalExtension,
                    originalSize = 0,
                    compressedSize = 0,
                    success = false
                };
            }

            int originalSize = imageData.Length;
            string ext = originalExtension?.ToLower() ?? "";

            // 验证质量参数
            quality = Mathf.Clamp(quality, 1, 100);

            // PNG 是无损格式，不做压缩（保留透明通道）
            if (ext == ".png")
            {
                TJLog.Log("[ImageCompressor] PNG 是无损格式，保留原始数据（保留透明通道）");
                return new CompressionResult
                {
                    data = imageData,
                    extension = ".png",
                    originalSize = originalSize,
                    compressedSize = originalSize,
                    success = true
                };
            }

            // JPG/JPEG 格式：重新编码以降低质量
            if (ext == ".jpg" || ext == ".jpeg")
            {
                // 创建 Texture2D 并解码图片
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                bool loadSuccess = false;

                try
                {
                    loadSuccess = texture.LoadImage(imageData);
                }
                catch (Exception e)
                {
                    TJLog.LogError($"[ImageCompressor] 解码图片失败: {e.Message}");
                    UnityEngine.Object.DestroyImmediate(texture);
                    return new CompressionResult
                    {
                        data = imageData,
                        extension = originalExtension,
                        originalSize = originalSize,
                        compressedSize = originalSize,
                        success = false
                    };
                }

                if (!loadSuccess)
                {
                    TJLog.LogWarning("[ImageCompressor] 无法解码图片数据");
                    UnityEngine.Object.DestroyImmediate(texture);
                    return new CompressionResult
                    {
                        data = imageData,
                        extension = originalExtension,
                        originalSize = originalSize,
                        compressedSize = originalSize,
                        success = false
                    };
                }

                byte[] compressedData = null;
                try
                {
                    compressedData = texture.EncodeToJPG(quality);
                    TJLog.Log($"[ImageCompressor] JPG 重新编码，质量: {quality}%");
                }
                catch (Exception e)
                {
                    TJLog.LogError($"[ImageCompressor] 编码图片失败: {e.Message}");
                    UnityEngine.Object.DestroyImmediate(texture);
                    return new CompressionResult
                    {
                        data = imageData,
                        extension = originalExtension,
                        originalSize = originalSize,
                        compressedSize = originalSize,
                        success = false
                    };
                }

                UnityEngine.Object.DestroyImmediate(texture);

                if (compressedData == null || compressedData.Length == 0)
                {
                    TJLog.LogWarning("[ImageCompressor] 压缩后数据为空，返回原始数据");
                    return new CompressionResult
                    {
                        data = imageData,
                        extension = originalExtension,
                        originalSize = originalSize,
                        compressedSize = originalSize,
                        success = false
                    };
                }

                int compressedSize = compressedData.Length;
                float compressionRatio = (1f - (float)compressedSize / originalSize) * 100f;

                TJLog.Log($"[ImageCompressor] 压缩完成: {originalSize} -> {compressedSize} bytes, 压缩率: {compressionRatio:F1}%");

                return new CompressionResult
                {
                    data = compressedData,
                    extension = ".jpg",
                    originalSize = originalSize,
                    compressedSize = compressedSize,
                    success = true
                };
            }

            // 不支持的格式，返回原始数据
            TJLog.LogWarning($"[ImageCompressor] 不支持的格式 '{ext}'，跳过压缩");
            return new CompressionResult
            {
                data = imageData,
                extension = originalExtension,
                originalSize = originalSize,
                compressedSize = originalSize,
                success = false
            };
        }

        /// <summary>
        /// 检查文件扩展名是否支持压缩
        /// </summary>
        /// <param name="extension">文件扩展名</param>
        /// <returns>是否支持压缩</returns>
        public static bool IsSupportedFormat(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            string ext = extension.ToLower();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
        }

        /// <summary>
        /// 获取压缩后的保存路径（如果扩展名改变则更新）
        /// </summary>
        /// <param name="originalPath">原始保存路径</param>
        /// <param name="newExtension">新的扩展名</param>
        /// <returns>更新后的保存路径</returns>
        public static string GetCompressedSavePath(string originalPath, string newExtension)
        {
            if (string.IsNullOrEmpty(originalPath) || string.IsNullOrEmpty(newExtension))
                return originalPath;

            string currentExt = Path.GetExtension(originalPath).ToLower();
            if (currentExt == newExtension.ToLower())
                return originalPath;

            return Path.ChangeExtension(originalPath, newExtension);
        }
    }

    /// <summary>
    /// 图片压缩结果
    /// </summary>
    public struct CompressionResult
    {
        /// <summary>压缩后的图片字节数据</summary>
        public byte[] data;

        /// <summary>新的文件扩展名（可能因格式转换而改变）</summary>
        public string extension;

        /// <summary>原始文件大小（字节）</summary>
        public int originalSize;

        /// <summary>压缩后文件大小（字节）</summary>
        public int compressedSize;

        /// <summary>压缩是否成功</summary>
        public bool success;

        /// <summary>压缩率（百分比）</summary>
        public float compressionRatio
        {
            get
            {
                if (originalSize == 0) return 0f;
                return (1f - (float)compressedSize / originalSize) * 100f;
            }
        }
    }
}
#endif
