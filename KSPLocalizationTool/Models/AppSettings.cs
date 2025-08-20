namespace KSPLocalizationTool.Models
{
    /// <summary>
    /// 应用程序设置模型
    /// </summary>
    public class AppSettings
    {
        public string ModDirectory { get; set; } = string.Empty;
        public string LocalizationDirectory { get; set; } = string.Empty;
        public string BackupDirectory { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = "zh-cn";
    }
}
