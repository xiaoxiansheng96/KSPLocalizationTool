using System;

namespace KSPLocalizationTool.Models
{
    /// <summary>
    /// 本地化项数据模型
    /// </summary>
    public class LocalizationItem
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        public int LineNumber { get; set; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public string ParameterType { get; set; } = string.Empty;
        
        /// <summary>
        /// 本地化键值
        /// </summary>
        public string LocalizationKey { get; set; } = string.Empty;
        
        /// <summary>
        /// 原始文本
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 是否替换失败
        /// </summary>
        public bool IsReplaceFailed { get; set; } = false;
    }

    /// <summary>
    /// 搜索结果项
    /// </summary>
    public class SearchResultItem
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型
        /// </summary>
        public string ParameterType { get; set; } = string.Empty;

        /// <summary>
        /// 参数键
        /// </summary>
        public string ParameterKey { get; set; } = string.Empty; // 添加ParameterKey属性

        /// <summary>
        /// 原始文本
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; } // 新增LineNumber属性
    }
}
    