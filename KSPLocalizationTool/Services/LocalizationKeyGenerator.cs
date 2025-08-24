using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    // 定义LocalizationKeyItem类
    public class LocalizationKeyItem
    {
        public string GeneratedKey { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ParameterType { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }

    // 使用主构造函数，移除重复的传统构造函数
    public partial class LocalizationKeyGenerator
    {
        // 添加KeysGenerated事件
        public event EventHandler<List<LocalizationKeyItem>>? KeysGenerated;

        // 字段声明 - 使用简化的集合初始化
        private readonly List<LocalizationKeyItem> _generatedKeys = [];
        private List<SearchResult> _searchResults = [];
// 根据提示，移除永不会读取值的私有成员 _dgvGeneratedKeys

        // 正则表达式定义
        [GeneratedRegex(@"[^\p{L}\p{N}_]|_+", RegexOptions.Compiled)]
        private static partial Regex NonAlphanumericRegex();

        // 设置搜索结果
        public void SetSearchResults(List<SearchResult> results)
        {
            _searchResults = results;
        }

        // 获取文件类型
        private static string GetFileType(string filePath)
        {
            return Path.GetExtension(filePath).TrimStart('.').ToUpper();
        }

        // 生成键
        private string GenerateKey(SearchResult result)
        {
            string fileType = GetFileType(result.FilePath);

            string fileName = Path.GetFileNameWithoutExtension(result.FilePath);
            // 使用正确的属性名ParameterKey
            string paramName = CleanKeyName(result.ParameterKey);

            // 生成哈希值
            string combined = $"{fileType}_{fileName}_{paramName}_{result.OriginalText}";
            string hash = GetShortHash(combined);

            // 构建键
            string key = $"#LOC_{fileType}_{fileName}_{paramName}_{hash}";

            // 检查重复并处理
            int counter = 1;
            string originalKey = key;
            while (_generatedKeys.Exists(k => k.GeneratedKey == key))
            {
                key = $"{originalKey}_{counter++}";
            }

            return key;
        }

        // 清理键名称
        private static string CleanKeyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "UNKNOWN";

            // 移除非字母数字下划线字符
            string cleanName = NonAlphanumericRegex().Replace(name, "_");
            // 规范化为大写
            return cleanName.ToUpper();
        }

        // 获取短哈希值
        private static string GetShortHash(string input)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
            // 取前4个字节转为十六进制
            return BitConverter.ToString(hashBytes, 0, 4).Replace("-", "");
        }

        // 生成键的主方法
        public void GenerateKeys()
        {
            _generatedKeys.Clear();
            foreach (var result in _searchResults)
            {
                string key = GenerateKey(result);
                _generatedKeys.Add(new LocalizationKeyItem
                {
                    GeneratedKey = key,
                    OriginalText = result.OriginalText,
                    FilePath = result.FilePath,
                    ParameterType = "string",
                    ModuleName = Path.GetFileNameWithoutExtension(result.FilePath),
                    LineNumber = result.LineNumber // 添加LineNumber属性
                });
            }

            // 触发事件
            KeysGenerated?.Invoke(this, _generatedKeys);
        }
    }
}


