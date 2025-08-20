using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public class LocalizationFileHandler
    {
        // 修复IDE0028：简化集合初始化
        private readonly Dictionary<string, string> _localizationData = new();
        private readonly string _languageCode;
        private readonly string _localizationDirectory;

        // 修复IDE0300：简化集合初始化
        private static readonly string[] _supportedExtensions = { ".cfg", ".txt", ".xml" };

        public LocalizationFileHandler(string languageCode, string localizationDirectory)
        {
            _languageCode = languageCode ?? "zh-cn";
            _localizationDirectory = localizationDirectory;

            LoadLocalizationFiles();
        }

        private void LoadLocalizationFiles()
        {
            try
            {
                if (!Directory.Exists(_localizationDirectory))
                {
                    Directory.CreateDirectory(_localizationDirectory);
                    return;
                }

                var files = Directory.EnumerateFiles(_localizationDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => _supportedExtensions.Any(ext =>
                        string.Equals(ext, Path.GetExtension(f), StringComparison.OrdinalIgnoreCase)));

                foreach (var file in files)
                {
                    LoadLocalizationFile(file);
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"加载本地化文件失败: {ex.Message}");
            }
        }

        private static void LoadLocalizationFile(string _)
        {
            // 实现加载逻辑...
        }

        public bool IsKeyExists(string key)
        {
            return !string.IsNullOrEmpty(key) && _localizationData.ContainsKey(key);
        }

        public static void SaveLocalizationItems(List<LocalizationItem> _)
        {
            // 实现保存逻辑...
        }
    }
}
