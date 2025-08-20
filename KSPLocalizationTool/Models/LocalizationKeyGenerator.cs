using System;
using System.Text.RegularExpressions;

namespace KSPLocalizationTool.Models
{
    public static class LocalizationKeyGenerator
    {
        // 生成符合KSP规范的本地化键
        public static string GenerateKey(string originalText, string parameterName, string fileName)
        {
            // 提取文件名中的前缀
            string filePrefix = Path.GetFileNameWithoutExtension(fileName).ToUpper();
            if (filePrefix.Length > 10)
                filePrefix = filePrefix.Substring(0, 10);

            // 处理原始文本生成键的一部分
            string textPart = CleanTextForKey(originalText);
            if (textPart.Length > 15)
                textPart = textPart.Substring(0, 15);

            // 处理参数名
            string paramPart = parameterName.ToUpper();
            if (paramPart.Length > 15)
                paramPart = paramPart.Substring(0, 15);

            // 组合生成键
            return $"LOC_{filePrefix}_{paramPart}_{textPart}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        }

        // 清理文本，仅保留字母数字字符
        private static string CleanTextForKey(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "EMPTY";

            // 移除特殊字符
            string cleaned = Regex.Replace(text, @"[^a-zA-Z0-9]", "_");
            // 替换多个下划线为单个
            cleaned = Regex.Replace(cleaned, @"_+", "_");
            // 去除首尾下划线
            return cleaned.Trim('_').ToUpper();
        }
    }
}