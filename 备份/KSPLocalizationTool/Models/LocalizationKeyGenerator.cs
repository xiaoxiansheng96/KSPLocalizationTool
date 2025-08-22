// KSPLocalizationTool/Models/LocalizationKeyGenerator.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KSPLocalizationTool.Models
{
    public static class LocalizationKeyGenerator
    {
        // 缓存已生成的键，确保唯一性
        private static readonly Dictionary<string, string> _generatedKeys = new Dictionary<string, string>();
        private static readonly object _lockObj = new object();

        // 生成符合KSP规范的本地化键
        public static string GenerateKey(string originalText, string parameterName, string fileName)
        {
            // 1. 严格验证输入
            if (string.IsNullOrEmpty(originalText) && string.IsNullOrEmpty(parameterName))
                throw new ArgumentException("原始文本和参数名不能同时为空");

            // 2. 提取文件名前缀（确保不包含特殊字符）
            string filePrefix = Path.GetFileNameWithoutExtension(fileName)
                .Replace(" ", "_")
                .ToUpper();
            filePrefix = CleanTextForKey(filePrefix); // 复用清理方法
            if (filePrefix.Length > 10)
                filePrefix = filePrefix.Substring(0, 10);

            // 3. 处理参数名（确保不为空）
            string paramPart = CleanTextForKey(parameterName);
            paramPart = paramPart.ToUpper();
            if (string.IsNullOrEmpty(paramPart))
                paramPart = "PARAM"; // 默认为PARAM
            if (paramPart.Length > 15)
                paramPart = paramPart.Substring(0, 15);

            // 4. 处理原始文本（确保不为空）
            string textPart = CleanTextForKey(originalText);
            if (string.IsNullOrEmpty(textPart))
                textPart = "TEXT"; // 默认为TEXT
            if (textPart.Length > 15)
                textPart = textPart.Substring(0, 15);

            // 5. 生成唯一ID（使用8位GUID）
            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);

            // 6. 组合键并验证长度
            string newKey = $"LOC_{filePrefix}_{paramPart}_{textPart}_{uniqueId}";

            // 7. 最终验证：确保符合KSP键规范（仅包含字母、数字和下划线）
            newKey = Regex.Replace(newKey, @"[^A-Z0-9_]", "_");
            if (newKey.Length > 128)
                newKey = newKey.Substring(0, 128);

            // 缓存逻辑保持不变...
            lock (_lockObj)
            {
                string baseKey = $"{fileName}_{parameterName}_{originalText}".ToLower();
                if (!_generatedKeys.ContainsKey(baseKey))
                {
                    _generatedKeys[baseKey] = newKey;
                }
            }

            return newKey;
        }

        // 清理文本，仅保留字母数字字符
        private static string CleanTextForKey(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "EMPTY";

            // 移除特殊字符，保留字母、数字和部分符号
            string cleaned = Regex.Replace(text, @"[^a-zA-Z0-9_]", "_");
            // 替换多个下划线为单个
            cleaned = Regex.Replace(cleaned, @"_+", "_");
            // 去除首尾下划线
            cleaned = cleaned.Trim('_');

            // 如果清理后为空，使用默认值
            if (string.IsNullOrEmpty(cleaned))
                cleaned = "TEXT";

            return cleaned.ToUpper();
        }

        // 清除缓存（主要用于测试和特殊场景）
        public static void ClearCache()
        {
            lock (_lockObj)
            {
                _generatedKeys.Clear();
            }
        }
    }
}