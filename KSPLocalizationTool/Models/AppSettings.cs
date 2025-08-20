namespace KSPLocalizationTool.Models
{
    public class AppSettings
    {
        public string ModDirectory { get; set; } = string.Empty;
        public string LocalizationDirectory { get; set; } = string.Empty;
        public string BackupDirectory { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = "zh-cn";
    }
}
    