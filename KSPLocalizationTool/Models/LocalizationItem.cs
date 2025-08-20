using System;

namespace KSPLocalizationTool.Models
{
    /// <summary>
    /// 本地化项模型
    /// </summary>
    public class LocalizationItem
    {
        /// <summary>
        /// 本地化键
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// 原始文本
        /// </summary>
        public string OriginalText { get; set; }
        
        /// <summary>
        /// 本地化文本
        /// </summary>
        public string LocalizedText { get; set; }
        
        /// <summary>
        /// 所在文件路径
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// 所在行号
        /// </summary>
        public int LineNumber { get; set; }
        
        /// <summary>
        /// 参数类型
        /// </summary>
        public string ParameterType { get; set; }
        
        /// <summary>
        /// 是否已经本地化
        /// </summary>
        public bool IsLocalized { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
