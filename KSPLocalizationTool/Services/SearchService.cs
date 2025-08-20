using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    /// <summary>
    /// 文本查找服务
    /// </summary>
    public class SearchService
    {
        private readonly string[] _cfgParameters = new[]
        {
            "startEventGUIName", "endEventGUIName", "actionGUIName", "toggleGUIName",
            "deployActionName", "retractActionName", "experimentActionName", "reviewActionName",
            "storeActionName", "collectActionName", "resetActionName", "experimentTitle",
            "dataDisplayName", "StartActionName", "StopActionName", "ToggleActionName",
            "ConverterName", "harvesterName", "partName", "description", "manufacturer",
            "category", "subcategory", "statusText", "warningText", "infoText", "menuName",
            "title", "Display Name", "Abbreviation", "RESULTS", "desc", "effectDescription",
            "headName"
        };
        
        private readonly string[] _hardcodedPatterns = new[]
        {
            @"TitleBarText\.text\s*=", @"Row1HeaderLabel\.text\s*=", @"Row2HeaderLabel\.text\s*=",
            @"AlertText\.text\s*=", @"_transferTargetsController\.DropdownDefaultText",
            @"target\.Value\.DisplayName", @"Column1HeaderText\.text\s*=", @"Column2HeaderText\.text\s*=",
            @"Column3HeaderText\.text\s*=", @"Column1Instructions\.text\s*=", 
            @"Column2Instructions\.text\s*=", @"Column3Instructions\.text\s*=",
            @"ResourceHeaderText\.text\s*=", @"AvailableAmountHeaderText\.text\s*=",
            @"RequiredAmountHeaderText\.text\s*=", @"SelectedShipHeaderText\.text\s*=",
            @"ShipNameText\.text\s*=", @"ShipMassText\.text\s*=", @"ShipCostText\.text\s*=",
            @"BuildShipButtonText\.text\s*=", @"SelectShipButtonText\.text\s*=",
            @"_konstructor\.InsufficientResourcesErrorText", @"ResourceText\.text\s*=",
            @"RequiredAmountText\.text\s*=", @"AvailableAmountText\.text\s*=",
            @"HeaderLabel\.text\s*=", @"SliderALabel\.text\s*=", @"SliderBLabel\.text\s*=",
            @"TransferAmountInput\.text\s*="
        };
        
        private string _localizationDir;
        
        public SearchService(string localizationDir)
        {
            _localizationDir = localizationDir;
        }
        
        /// <summary>
        /// 搜索指定目录中的所有CFG文件
        /// </summary>
        public List<LocalizationItem> SearchCfgFiles(string directory)
        {
            List<LocalizationItem> results = new List<LocalizationItem>();
            
            if (!Directory.Exists(directory))
                return results;
                
            // 获取所有CFG文件，跳过本地化目录
            var cfgFiles = Directory.EnumerateFiles(directory, "*.cfg", SearchOption.AllDirectories)
                .Where(file => !file.StartsWith(_localizationDir, StringComparison.OrdinalIgnoreCase));
                
            foreach (var file in cfgFiles)
            {
                var items = ParseCfgFile(file);
                results.AddRange(items);
            }
            
            return results;
        }
        
        /// <summary>
        /// 解析CFG文件
        /// </summary>
        private List<LocalizationItem> ParseCfgFile(string filePath)
        {
            List<LocalizationItem> items = new List<LocalizationItem>();
            string[] lines = File.ReadAllLines(filePath);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // 检查是否是我们要找的参数
                foreach (var param in _cfgParameters)
                {
                    // 匹配参数模式，如 "parameter = value" 或 "parameter = "value""
                    string pattern = $@"^{param}\s*=\s*(""(.*?)""|(.*?))(//.*)?$";
                    Match match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                    
                    if (match.Success)
                    {
                        // 提取值（去除引号）
                        string value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        
                        // 检查是否已经是本地化键
                        if (!value.StartsWith("#LOC_"))
                        {
                            items.Add(new LocalizationItem
                            {
                                OriginalText = value,
                                FilePath = filePath,
                                LineNumber = i + 1,
                                ParameterType = param,
                                IsLocalized = false
                            });
                        }
                        break;
                    }
                }
            }
            
            return items;
        }
        
        /// <summary>
        /// 搜索指定目录中的硬编码文本
        /// </summary>
        public List<LocalizationItem> SearchHardcodedText(string directory)
        {
            List<LocalizationItem> results = new List<LocalizationItem>();
            
            if (!Directory.Exists(directory))
                return results;
                
            // 获取所有代码文件，跳过本地化目录
            var codeFiles = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(file => (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || 
                               file.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)) &&
                               !file.StartsWith(_localizationDir, StringComparison.OrdinalIgnoreCase));
                
            foreach (var file in codeFiles)
            {
                var items = ParseCodeFile(file);
                results.AddRange(items);
            }
            
            return results;
        }
        
        /// <summary>
        /// 解析代码文件中的硬编码文本
        /// </summary>
        private List<LocalizationItem> ParseCodeFile(string filePath)
        {
            List<LocalizationItem> items = new List<LocalizationItem>();
            string[] lines = File.ReadAllLines(filePath);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // 检查是否是我们要找的模式
                foreach (var pattern in _hardcodedPatterns)
                {
                    // 匹配赋值模式，提取字符串
                    string regexPattern = $@"{pattern}\s*=.*?""(.*?)""";
                    Match match = Regex.Match(line, regexPattern);
                    
                    if (match.Success)
                    {
                        string value = match.Groups[1].Value;
                        
                        // 检查是否已经是本地化键
                        if (!value.StartsWith("#LOC_"))
                        {
                            items.Add(new LocalizationItem
                            {
                                OriginalText = value,
                                FilePath = filePath,
                                LineNumber = i + 1,
                                ParameterType = pattern,
                                IsLocalized = false
                            });
                        }
                        break;
                    }
                }
            }
            
            return items;
        }
    }
}
