using System;
using System.IO;
using System.Text.Json;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public static class ConfigManager
    {
        // 缓存JsonSerializerOptions实例，避免重复创建
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appData, "KSPLocalizationTool");
            Directory.CreateDirectory(configDir);
            return Path.Combine(configDir, "settings.json");
        }

        public static AppSettings? LoadSettings()
        {
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"加载配置失败: {ex.Message}");
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                if (settings == null) return;

                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(GetConfigPath(), json);
            }
            catch (Exception ex)
            {
                LogManager.Log($"保存配置失败: {ex.Message}");
            }
        }
    }
}
