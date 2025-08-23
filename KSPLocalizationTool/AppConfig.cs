using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace KSPLocalizationTool
{

    /// <summary>
    /// 应用程序配置类，保存程序的各种设置
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// MOD目录
        /// </summary>
        public string ModDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 备份目录
        /// </summary>
        public string BackupDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 本地化文件目录
        /// </summary>
        public string LocalizationDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 日志目录
        /// </summary>
        public string LogDirectory { get; set; } = Path.Combine(Application.StartupPath, "Logs");

        public static class ConfigConstants
        {
            public static string ConfigFilePath => Path.Combine(Application.StartupPath, "app_config.xml");
        }

        // 在AppConfig类中添加
        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void Save()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppConfig));
                // 第48行修复
                using var writer = new StreamWriter(ConfigConstants.ConfigFilePath);
                serializer.Serialize(writer, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigConstants.ConfigFilePath))
                {
                    var serializer = new XmlSerializer(typeof(AppConfig));
                    using var reader = new StreamReader(ConfigConstants.ConfigFilePath);
                    var result = serializer.Deserialize(reader) as AppConfig;
                    return result ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return new AppConfig();
        }
        /// <summary>
        /// CFG文件筛选参数（简化集合初始化）
        /// </summary>
        public string[] CfgFilters = [
            "startEventGUIName:", "endEventGUIName:", "actionGUIName:", "toggleGUIName:",
            "deployActionName:", "retractActionName:", "experimentActionName:",
            "reviewActionName:", "storeActionName:", "collectActionName:",
            "resetActionName:", "experimentTitle:", "dataDisplayName:",
            "StartActionName:", "StopActionName:", "ToggleActionName:",
            "ConverterName:", "harvesterName:", "partName:", "description:",
            "manufacturer:", "category:", "subcategory:", "statusText:",
            "warningText:", "infoText:", "menuName:", "title:", "Display Name:",
            "Abbreviation:", "RESULTS:", "desc:", "effectDescription:", "headName:"
        ];

        /// <summary>
        /// CS文件筛选参数（简化集合初始化）
        /// </summary>
        public string[] CsFilters = [
            "button", "message", "tooltip", "label", "agreement",
            "menu", "format", "guiname"
        ];
    }
}