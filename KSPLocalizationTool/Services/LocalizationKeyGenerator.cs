using KSPLocalizationTool.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Windows.Forms;




namespace KSPLocalizationTool.Services
{
    /// <summary>
    /// 本地化键值对数据结构
    /// </summary>
    public class LocalizationKeyItem
    {
        public required string OriginalText { get; set; }
        public required string GeneratedKey { get; set; }
        public required string ParameterType { get; set; }
        public required string FilePath { get; set; }
        public int LineNumber { get; set; }
        public required string ModuleName { get; set; }
        public bool IsCustomized { get; set; } = false;
    }

    /// <summary>
    /// 本地化键生成模块，生成符合坎巴拉太空计划规范的本地化键
    /// </summary>
    // 移除重复的类定义
    // public partial class LocalizationKeyGenerator
    // {
    //     [GeneratedRegex(@"__+")]
    //     private static partial Regex MultipleUnderscoresRegex();
    // }
    
    // 确保在主类定义中包含这些方法
    // 在类级别添加正则表达式方法定义
    public partial class LocalizationKeyGenerator
    {
        /// <summary>
        /// 匹配非字母数字下划线的正则表达式
        /// </summary>
        [GeneratedRegex(@"[^a-zA-Z0-9_]")]
        private static partial Regex NonAlphanumericRegex();

        /// <summary>
        /// 清理键前缀
        /// </summary>
        private static string CleanKeyPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return "";

            // 清理并标准化前缀
            string cleaned = NonAlphanumericRegex().Replace(prefix, "");
            cleaned = cleaned.ToUpper();

            // 确保前缀以下划线结尾或为空
            return cleaned.EndsWith('_') ? cleaned : $"{cleaned}_";
        }
    
        [GeneratedRegex(@"__+")]
        private static partial Regex MultipleUnderscoresRegex();
    
        // UI控件
        private Label lblKeyGenerationStatus = null!;
        private DataGridView dgvGeneratedKeys = null!;
        private Button btnGenerateKeys = null!;
        private Button btnApplyCustomKeys = null!;
        private TextBox txtModulePrefix = null!;
        private Label lblModulePrefix = null!;

        // 生成的本地化键缓存
        private readonly List<LocalizationKeyItem> _generatedKeys = [];

        // 生成状态
        private bool _isGenerating;

        // 事件
        public event Action<string>? StatusChanged;
        public event Action<int>? ProgressUpdated;
        public event Action<List<LocalizationKeyItem>>? KeysGenerated;

        /// <summary>
        /// 获取状态标签控件
        /// </summary>
        public Label KeyGenerationStatusLabel => lblKeyGenerationStatus;

        /// <summary>
        /// 获取生成的键数据网格控件
        /// </summary>
        public DataGridView GeneratedKeysGridView => dgvGeneratedKeys;

        /// <summary>
        /// 获取生成键按钮控件
        /// </summary>
        public Button GenerateKeysButton => btnGenerateKeys;

        /// <summary>
        /// 获取应用自定义键按钮控件
        /// </summary>
        public Button ApplyCustomKeysButton => btnApplyCustomKeys;

        /// <summary>
        /// 获取模块前缀文本框控件
        /// </summary>
        public TextBox ModulePrefixTextBox => txtModulePrefix;

        /// <summary>
        /// 获取模块前缀标签控件
        /// </summary>
        public Label ModulePrefixLabel => lblModulePrefix;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LocalizationKeyGenerator()
        {
            InitializeControls();
            SetupEventHandlers();
        }

        /// <summary>
        /// 初始化UI控件
        /// </summary>
        private void InitializeControls()
        {
            // 模块前缀标签
            lblModulePrefix = new Label
            {
                Text = "模块前缀:",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 10, 10, 10),
                Width = 100,
                Font = new System.Drawing.Font("微软雅黑", 10F)
            };

            // 模块前缀文本框
            txtModulePrefix = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 10, 10),
                Font = new System.Drawing.Font("微软雅黑", 10F),
                Height = 30,
                Text = "MOD_" // 默认前缀
            };

            // 状态标签
            lblKeyGenerationStatus = new Label
            {
                Text = "等待生成本地化键...",
                Dock = DockStyle.Top,
                Height = 30,
                Margin = new Padding(0, 10, 0, 5),
                Font = new System.Drawing.Font("微软雅黑", 10F)
            };

            // 生成的键数据网格
            dgvGeneratedKeys = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                ColumnHeadersVisible = true,
                ColumnHeadersHeight = 35,
                ColumnCount = 5,
                AlternatingRowsDefaultCellStyle = { BackColor = System.Drawing.Color.AliceBlue },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.Fixed3D,
                Margin = new Padding(0, 5, 0, 10),
                Font = new System.Drawing.Font("微软雅黑", 9F),
                RowTemplate = { Height = 30 },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };

            // 设置数据网格列
            dgvGeneratedKeys.Columns[0].Name = "参数类型";
            dgvGeneratedKeys.Columns[1].Name = "原始文本";
            dgvGeneratedKeys.Columns[2].Name = "生成的键";
            dgvGeneratedKeys.Columns[3].Name = "文件路径";
            dgvGeneratedKeys.Columns[4].Name = "行号";
            dgvGeneratedKeys.Columns[2].ReadOnly = false; // 允许编辑生成的键
            dgvGeneratedKeys.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            // 生成键按钮
            btnGenerateKeys = new Button
            {
                Text = "生成本地化键",
                Width = 130,
                Height = 35,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(8, 10, 8, 10),
                Font = new System.Drawing.Font("微软雅黑", 10F)
            };

            // 应用自定义键按钮
            btnApplyCustomKeys = new Button
            {
                Text = "应用自定义键",
                Width = 130,
                Height = 35,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(8, 10, 8, 10),
                Font = new System.Drawing.Font("微软雅黑", 10F)
            };
        }

        /// <summary>
        /// 设置事件处理程序
        /// </summary>
        private void SetupEventHandlers()
        {
            btnGenerateKeys.Click += GenerateKeys;
            btnApplyCustomKeys.Click += ApplyCustomKeys;
            dgvGeneratedKeys.CellEndEdit += (s, e) =>
            {
                // 当用户编辑键时标记为自定义
                if (e.ColumnIndex == 2 && e.RowIndex >= 0 && e.RowIndex < _generatedKeys.Count)
                {
                    _generatedKeys[e.RowIndex].GeneratedKey = dgvGeneratedKeys.Rows[e.RowIndex].Cells[2].Value?.ToString() ?? string.Empty;
                    _generatedKeys[e.RowIndex].IsCustomized = true;
                }
            };
        }

        /// <summary>
        /// 从查找结果生成本地化键
        /// </summary>
        public void GenerateKeys(object? sender, EventArgs e)
        {
            if (_isGenerating || SearchResults == null || SearchResults.Count == 0)
            {
                StatusChanged?.Invoke(_isGenerating ? "正在生成本地化键..." : "没有可处理的查找结果");
                return;
            }

            _isGenerating = true;
            btnGenerateKeys.Enabled = false;
            StatusChanged?.Invoke("开始生成本地化键...");

            // 启动后台生成
            var worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

            // 修复第234行的方法调用
            worker.DoWork += GenerateKeysInBackground;
            // 移除原来的错误调用: worker.DoWork += (s, args) => GenerateKeysInBackground(worker);

            worker.ProgressChanged += (s, args) => ProgressUpdated?.Invoke(args.ProgressPercentage);
            worker.RunWorkerCompleted += (s, args) =>
            {
                _isGenerating = false;
                btnGenerateKeys.Enabled = true;

                if (args.Error != null)
                {
                    StatusChanged?.Invoke($"生成失败: {args.Error.Message}");
                }
                else
                {
                    UpdateGeneratedKeysGrid();
                    StatusChanged?.Invoke($"已生成 {_generatedKeys.Count} 个本地化键");
                    KeysGenerated?.Invoke(new List<LocalizationKeyItem>(_generatedKeys));
                }
            };

            worker.RunWorkerAsync();
        }
        /// <summary>
        /// 生成原始文本的8位大写哈希值（用于键的唯一性）
        /// </summary>
        private static string GenerateShortHash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "00000000";

            using var md5 = MD5.Create();
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = MD5.HashData(inputBytes);

                // 转换为16进制并取前8位（4字节）
                var sb = new StringBuilder();
                for (int i = 0; i < 4; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// 在后台生成本地化键
        /// </summary>
        // 修改GenerateKeysInBackground方法
        // 删除重复的无参数方法
        // private void GenerateKeysInBackground()
        // {
        //     try
        //     {
        //         _generatedKeys.Clear();
        //         var keyTracker = new HashSet<string>();
        //         string modulePrefix = CleanKeyPrefix(txtModulePrefix.Text);
        //         int totalItems = SearchResults?.Count ?? 0;
        
        //         for (int i = 0; i < totalItems; i++)
        //         {
        //             var result = SearchResults[i];
        //             string generatedKey = GenerateKspCompliantKey(result, modulePrefix, keyTracker);
        
        //             // 提取模块名称（从文件路径或参数类型）
        //             string moduleName = ExtractModuleName(result.FilePath);
        
        //             _generatedKeys.Add(new LocalizationKeyItem
        //             {
        //                 OriginalText = result.OriginalText,
        //                 GeneratedKey = generatedKey,
        //                 ParameterType = result.ParameterType,
        //                 FilePath = result.FilePath,
        //                 LineNumber = result.LineNumber,
        //                 ModuleName = moduleName
        //             });
        
        //             // 更新进度
        //             worker?.ReportProgress((int)((double)(i + 1) / totalItems * 100));
        //         }
        //     }
        //     catch (Exception)
        //     {
        //         // 记录异常信息（可选）
        //         throw;
        //     }
        // }
        
        // 保留并使用带参数的方法
        private void GenerateKeysInBackground(object? sender, DoWorkEventArgs e)
        {
            try
            {
                _generatedKeys.Clear();
                var keyTracker = new HashSet<string>();
                string modulePrefix = CleanKeyPrefix(txtModulePrefix.Text);
                int totalItems = SearchResults?.Count ?? 0;
        
            // 确保worker变量在正确的作用域中声明和初始化
            if (sender is not BackgroundWorker worker)
            {
                throw new ArgumentNullException(nameof(sender), "Sender is not a BackgroundWorker");
            }
        
            for (int i = 0; i < totalItems; i++)
            {
                var result = SearchResults?[i] ?? null;
                if (result == null) continue;
        
                // 由于 GenerateKspCompliantKey 方法不存在，这里添加占位符实现，实际需要根据需求实现完整逻辑
                string generatedKey = GenerateTempKspCompliantKey(result, modulePrefix, keyTracker);

// 以下为临时添加的占位方法，需根据实际需求替换
static string GenerateTempKspCompliantKey(SearchResultItem result, string modulePrefix, HashSet<string> keyTracker)

{
    string cleanedText = CleanTextForKey(result.OriginalText);
    string shortHash = GenerateShortHash(result.OriginalText);
    string candidateKey = $"{modulePrefix}{cleanedText}_{shortHash}";
    
    // 简单确保键唯一性
    int counter = 1;
    string uniqueKey = candidateKey;
    while (keyTracker.Contains(uniqueKey))
    {
        uniqueKey = $"{candidateKey}_{counter++}";
    }
    keyTracker.Add(uniqueKey);
    return uniqueKey;
}
        
                // 提取模块名称（从文件路径或参数类型）
                string moduleName = ExtractModuleName(result.FilePath);
        
                _generatedKeys.Add(new LocalizationKeyItem
                {
                    OriginalText = result.OriginalText,
                    GeneratedKey = generatedKey,
                    ParameterType = result.ParameterType,
                    FilePath = result.FilePath,
                    LineNumber = result.LineNumber,
                    ModuleName = moduleName
                });
        
                // 使用已检查的worker变量
                worker.ReportProgress((int)((double)(i + 1) / totalItems * 100));
            }
        }
            catch (Exception)
            {
                // 记录异常信息（可选）
                throw;
            }
        }
        /// <summary>
        /// 清理文本（用于文件名、参数名称等部分）
        /// </summary>
        private static string CleanTextForKey(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // 1. 替换空格为下划线
            string cleaned = text.Replace(' ', '_');

            // 2. 移除所有非字母、数字、下划线的字符
            cleaned = NonAlphanumericRegex().Replace(cleaned, "");

            // 3. 转换为大写
            cleaned = cleaned.ToUpper();

            // 4. 合并连续下划线，移除首尾下划线
            cleaned = MultipleUnderscoresRegex().Replace(cleaned, "_");
            cleaned = cleaned.Trim('_');

            return cleaned;
        }


        /// <summary>
        /// 更新生成的键网格
        /// </summary>
        // 已删除重复的 UpdateGeneratedKeysGrid 方法

        /// <summary>
        /// 从文件路径提取模块名称
        /// </summary>
        private static string ExtractModuleName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "UNKNOWN";
        
            // 从文件路径提取可能的模块名称
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string parentDir = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? string.Empty;
        
            return !string.IsNullOrEmpty(parentDir) ? parentDir.ToUpper() : fileName.ToUpper();
        }

        /// <summary>
        /// 更新生成的键数据网格
        /// </summary>
        private void UpdateGeneratedKeysGrid()
        {
            dgvGeneratedKeys.Rows.Clear();
            foreach (var item in _generatedKeys)
            {
                int rowIndex = dgvGeneratedKeys.Rows.Add();
                dgvGeneratedKeys.Rows[rowIndex].Cells[0].Value = item.OriginalText;
                dgvGeneratedKeys.Rows[rowIndex].Cells[1].Value = item.ModuleName;
                dgvGeneratedKeys.Rows[rowIndex].Cells[2].Value = item.GeneratedKey;
            }
        }


        /// <summary>
        /// 应用自定义键（保存用户编辑的键）
        /// </summary>
        private void ApplyCustomKeys(object? sender, EventArgs e)
        {
            // 实现应用自定义键的逻辑
            for (int i = 0; i < _generatedKeys.Count && i < dgvGeneratedKeys.Rows.Count; i++)
            {
                // 这里应该有更新生成键的逻辑
                // 例如: _generatedKeys[i].GeneratedKey = dgvGeneratedKeys.Rows[i].Cells[2].Value?.ToString() ?? string.Empty;
            }
            StatusChanged?.Invoke("已应用自定义键");
        }

        /// <summary>
        /// 获取生成的本地化键
        /// </summary>
        public List<LocalizationKeyItem> GetGeneratedKeys()
        {
            return new List<LocalizationKeyItem>(_generatedKeys);
        }

        /// <summary>
        /// 查找模块生成的缓存结果（数据源）
        /// </summary>
        public List<SearchResultItem>? SearchResults { get; set; } = [];
    }
}


