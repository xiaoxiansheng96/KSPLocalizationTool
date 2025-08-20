namespace KSPLocalizationTool.Models
{
    public class LocalizationItem
    {
        public string Key { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string LocalizedText { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }
}