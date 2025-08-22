using KspModLocalizer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using KSPLocalizationTool.Models; // 添加缺少的命名空间引用

namespace KSPLocalizationTool.Services
{
    /// <summary>
    /// 查找结果数据结构
    /// </summary>
    public class SearchResult
    {
        public string FilePath { get; set; }
        public string ParameterType { get; set; } // "CFG参数" 或 "硬编码参数"
        public string ParameterKey { get; set; }
        public string OriginalText { get; set; }
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// MOD参数查找模块
    /// </summary>
    public class SearchModule
    {
        // 缓存查找结果
        private List<SearchResult> _searchResults = new List<SearchResult>();

        // 排除的目录名称（本地化文件夹）
        private readonly List<string> _excludedDirectories = new List<string> { "Localization", "localization" };

        // 查找中标志
        private bool _isSearching;

        // 取消查找标志
        private bool _cancelSearch;

        // 查找进度事件
        public event Action<int> ProgressUpdated;

        // 查找状态变更事件
        public event Action<string> StatusChanged;

        // 查找完成事件
        public event Action<List<SearchResult>> SearchCompleted;

        private AppConfig _config;
        private List<LocalizationItem> _results = new List<LocalizationItem>();

        // 添加BackgroundWorker的定义
        private BackgroundWorker _searchWorker = new BackgroundWorker
        {
            WorkerSupportsCancellation = true
        };

        /// <summary>
        /// 获取缓存的查找结果
        /// </summary>
        public List<SearchResult> GetCachedResults()
        {
            return new List<SearchResult>(_searchResults);
        }

        /// <summary>
        /// 开始查找操作
        /// </summary>
        /// <param name="modDirectory">MOD目录路径</param>
        /// <param name="cfgParameters">CFG参数列表</param>
        /// <param name="hardcodeParameters">硬编码参数列表</param>
        /// <param name="worker">后台工作器</param>
        public void StartSearch(string modDirectory, List<string> cfgParameters,
                               List<string> hardcodeParameters, BackgroundWorker worker)
        {
            if (_isSearching) return;

            _isSearching = true;
            _cancelSearch = false;
            _searchResults.Clear();

            // 在后台线程执行查找
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    OnStatusChanged("开始扫描目录...");

                    // 验证目录
                    if (!Directory.Exists(modDirectory))
                    {
                        OnStatusChanged("无效的MOD目录");
                        OnSearchCompleted();
                        return;
                    }

                    // 获取所有要查找的文件
                    var files = GetFilesToSearch(modDirectory);
                    int totalFiles = files.Count;
                    int processedFiles = 0;

                    OnStatusChanged($"找到 {totalFiles} 个文件待处理");

                    // 逐个文件查找
                    foreach (var file in files)
                    {
                        if (_cancelSearch || (worker != null && worker.CancellationPending))
                        {
                            OnStatusChanged("查找已取消");
                            OnSearchCompleted();
                            return;
                        }

                        // 处理CFG文件
                        if (file.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
                        {
                            ProcessCfgFile(file, cfgParameters);
                        }
                        // 处理CS文件（硬编码）
                        else if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        {
                            ProcessCsFile(file, hardcodeParameters);
                        }

                        // 更新进度
                        processedFiles++;
                        int progress = (int)((double)processedFiles / totalFiles * 100);
                        OnProgressUpdated(progress);
                    }

                    OnStatusChanged($"查找完成，找到 {_searchResults.Count} 个匹配项");
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"查找出错: {ex.Message}");
                }
                finally
                {
                    OnSearchCompleted();
                }
            });
        }

        /// <summary>
        /// 停止查找操作
        /// </summary>
        public void StopSearch()
        {
            _cancelSearch = true;
        }

        /// <summary>
        /// 获取所有需要查找的文件（包含子目录，排除本地化目录）
        /// </summary>
        private List<string> GetFilesToSearch(string rootDirectory)
        {
            var files = new List<string>();

            try
            {
                // 添加当前目录的文件
                files.AddRange(Directory.GetFiles(rootDirectory, "*.cfg", SearchOption.TopDirectoryOnly));
                files.AddRange(Directory.GetFiles(rootDirectory, "*.cs", SearchOption.TopDirectoryOnly));

                // 递归处理子目录（排除本地化目录）
                foreach (var dir in Directory.GetDirectories(rootDirectory))
                {
                    string dirName = Path.GetFileName(dir);
                    if (!_excludedDirectories.Contains(dirName))
                    {
                        files.AddRange(GetFilesToSearch(dir));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 忽略无权限访问的目录
                OnStatusChanged($"无权限访问目录: {rootDirectory}");
            }
            catch (PathTooLongException)
            {
                // 忽略路径过长的目录
                OnStatusChanged($"路径过长: {rootDirectory}");
            }

            return files;
        }

        /// <summary>
        /// 处理CFG文件，查找指定参数
        /// </summary>
        private void ProcessCfgFile(string filePath, List<string> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;

            try
            {
                var lines = File.ReadAllLines(filePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("#"))
                        continue;

                    // 查找参数匹配
                    foreach (var param in parameters)
                    {
                        if (string.IsNullOrWhiteSpace(param)) continue;

                        // 简单匹配: 参数名=值 格式
                        string pattern = $@"^{Regex.Escape(param)}\s*=.*";
                        if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                        {
                            // 提取参数值
                            string value = line.Substring(line.IndexOf('=') + 1).Trim();

                            _searchResults.Add(new SearchResult
                            {
                                FilePath = filePath,
                                ParameterType = "CFG参数",
                                ParameterKey = param,
                                OriginalText = value,
                                LineNumber = i + 1
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"处理文件出错 {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理CS文件，查找硬编码参数
        /// </summary>
        private void ProcessCsFile(string filePath, List<string> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;

            try
            {
                var lines = File.ReadAllLines(filePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                        continue;

                    // 查找硬编码参数匹配
                    foreach (var param in parameters)
                    {
                        if (string.IsNullOrWhiteSpace(param)) continue;

                        // 查找包含该参数的字符串
                        string pattern = $@""".*?{Regex.Escape(param)}.*?""";
                        var matches = Regex.Matches(line, pattern);

                        foreach (Match match in matches)
                        {
                            _searchResults.Add(new SearchResult
                            {
                                FilePath = filePath,
                                ParameterType = "硬编码参数",
                                ParameterKey = param,
                                OriginalText = match.Value.Trim('"'),
                                LineNumber = i + 1
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"处理文件出错 {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 触发进度更新事件
        /// </summary>
        private void OnProgressUpdated(int progress)
        {
            ProgressUpdated?.Invoke(progress);
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(status);
        }

        /// <summary>
        /// 触发查找完成事件
        /// </summary>
        private void OnSearchCompleted()
        {
            _isSearching = false;
            SearchCompleted?.Invoke(new List<SearchResult>(_searchResults));
        }
    }
}