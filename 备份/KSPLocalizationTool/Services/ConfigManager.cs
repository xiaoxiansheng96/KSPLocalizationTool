using System;
using System.IO;
using System.Xml.Serialization;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public static class ConfigManager
    {
        private static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appData, "KSPLocalizationTool");
            Directory.CreateDirectory(configDir);
            return Path.Combine(configDir, "settings.xml");
        }
        
        public static AppSettings? LoadSettings()
        {
            try
            {
                string configPath = GetConfigPath();
                if (File.Exists(configPath))
                {
                    using var reader = new StreamReader(configPath);
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    return serializer.Deserialize(reader) as AppSettings;
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
                string configPath = GetConfigPath();
                using var writer = new StreamWriter(configPath);
                var serializer = new XmlSerializer(typeof(AppSettings));
                serializer.Serialize(writer, settings);
            }
            catch (Exception ex)
            {
                LogManager.Log($"保存配置失败: {ex.Message}");
            }
        }
    }
}
    