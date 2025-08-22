using KSPLocalizationTool.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace KSPLocalizationTool.Services
{
    /// <summary>
    /// 本地化键值对数据结构
    /// </summary>
    public class LocalizationKeyItem
    {
        public string OriginalText { get; set; }
        public string GeneratedKey { get; set; }
        public string ParameterType { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string ModuleName { get; set; }
        public bool IsCustomized { get; set; } = false;
    }

    /// <summary>
    /// 本地化键生成模块，生成符合坎巴拉太空计划规范的本地化键
    /// </summary>
    public class LocalizationKeyGenerator
    {
        // UI控件
        private Label lblKeyGenerationStatus;
        private DataGridView dgvGeneratedKeys;
        private Button btnGenerateKeys;
        private Button btnApplyCustomKeys;
        private TextBox txtModulePrefix;
        private Label lblModulePrefix;

        // 生成的本地化键缓存
        private List<LocalizationKeyItem> _generatedKeys = new List<LocalizationKeyItem>();

        // 生成状态
        private bool _isGenerating;

        // 事件
        public event Action<string> StatusChanged;
        public event Action<int> ProgressUpdated;
        public event Action<List<LocalizationKeyItem>> KeysGenerated;

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
                    _generatedKeys[e.RowIndex].GeneratedKey = dgvGeneratedKeys.Rows[e.RowIndex].Cells[2].Value?.ToString();
                    _generatedKeys[e.RowIndex].IsCustomized = true;
                }
            };
        }

        /// <summary>
        /// 从查找结果生成本地化键
        /// </summary>
        public void GenerateKeys(object sender, EventArgs e)
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

            worker.DoWork += (s, args) => GenerateKeysInBackground(worker);
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
        /// 在后台生成本地化键
        /// </summary>
        private void GenerateKeysInBackground(BackgroundWorker worker)
        {
            try
            {
                _generatedKeys.Clear();
                var keyTracker = new HashSet<string>();
                string modulePrefix = CleanKeyPrefix(txtModulePrefix.Text);
                int totalItems = SearchResults.Count;

                for (int i = 0; i < totalItems; i++)
                {
                    var result = SearchResults[i];
                    string generatedKey = GenerateKspCompliantKey(result, modulePrefix, keyTracker);

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

                    // 更新进度
                    worker.ReportProgress((int)((double)(i + 1) / totalItems * 100));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 生成符合KSP规范的本地化键
        /// </summary>
        private string GenerateKspCompliantKey(SearchResultItem result, string modulePrefix, HashSet<string> keyTracker)
        {
            // KSP本地化键规则：
            // 1. 仅包含大写字母、数字和下划线
            // 2. 不包含空格、特殊字符和标点
            // 3. 通常使用模块前缀+描述性名称的格式
            // 4. 键应该唯一且具有描述性

            // 1. 处理原始文本生成基础键
            string baseKey = CleanTextForKey(result.OriginalText);

            // 2. 添加参数类型前缀（根据SearchResultItem的ParameterType）
            string typePrefix = result.ParameterType == "CFG参数" ? "CFG_" : "CODE_";

            // 3. 组合完整键（模块前缀+类型前缀+基础键）
            string fullKey = $"{modulePrefix}{typePrefix}{baseKey}";

            // 4. 确保键的唯一性
            string uniqueKey = fullKey;
            int duplicateCounter = 1;

            while (keyTracker.Contains(uniqueKey))
            {
                uniqueKey = $"{fullKey}_{duplicateCounter++}";
            }

            keyTracker.Add(uniqueKey);
            return uniqueKey;
        }

        /// <summary>
        /// 清理文本以生成符合规范的键
        /// </summary>
        private string CleanTextForKey(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "EMPTY_TEXT";

            // 截取前50个字符防止键过长
            string cleaned = text.Length > 50 ? text.Substring(0, 50) : text;

            // 替换空格为下划线
            cleaned = cleaned.Replace(' ', '_');

            // 移除所有非字母数字和下划线的字符
            cleaned = Regex.Replace(cleaned, @"[^a-zA-Z0-9_]", "");

            // 转换为大写
            cleaned = cleaned.ToUpper();

            // 移除连续的下划线
            cleaned = Regex.Replace(cleaned, @"__+", "_");

            // 移除首尾下划线
            cleaned = cleaned.Trim('_');

            // 确保键不为空
            return string.IsNullOrEmpty(cleaned) ? "UNNAMED_TEXT" : cleaned;
        }

        /// <summary>
        /// 清理键前缀
        /// </summary>
        private string CleanKeyPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return "";

            // 清理并标准化前缀
            string cleaned = Regex.Replace(prefix, @"[^a-zA-Z0-9_]", "");
            cleaned = cleaned.ToUpper();

            // 确保前缀以下划线结尾或为空
            return string.IsNullOrEmpty(cleaned) ? "" : $"{cleaned}_";
        }

        /// <summary>
        /// 从文件路径提取模块名称
        /// </summary>
        private string ExtractModuleName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "UNKNOWN";

            // 从文件路径提取可能的模块名称（通常是文件名或父目录名）
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string parentDir = Path.GetFileName(Path.GetDirectoryName(filePath));

            return !string.IsNullOrEmpty(parentDir) ? parentDir.ToUpper() : fileName.ToUpper();
        }

        /// <summary>
        /// 更新生成的键数据网格
        /// </summary>
        private void UpdateGeneratedKeysGrid()
        {
            if (dgvGeneratedKeys.InvokeRequired)
            {
                dgvGeneratedKeys.Invoke(new Action(UpdateGeneratedKeysGrid));
                return;
            }

            dgvGeneratedKeys.Rows.Clear();

            foreach (var item in _generatedKeys)
            {
                dgvGeneratedKeys.Rows.Add(
                    item.ParameterType,
                    TruncateText(item.OriginalText, 50), // 截断长文本以便显示
                    item.GeneratedKey,
                    TruncateText(Path.GetFileName(item.FilePath), 20),
                    item.LineNumber
                );
            }
        }

        /// <summary>
        /// 截断文本用于显示
        /// </summary>
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return $"{text.Substring(0, maxLength)}...";
        }

        /// <summary>
        /// 应用自定义键（保存用户编辑的键）
        /// </summary>
        private void ApplyCustomKeys(object sender, EventArgs e)
        {
            if (_generatedKeys.Count == 0 || dgvGeneratedKeys.Rows.Count == 0)
            {
                StatusChanged?.Invoke("没有可应用的本地化键");
                return;
            }

            // 保存用户编辑的键
            for (int i = 0; i < _generatedKeys.Count && i < dgvGeneratedKeys.Rows.Count; i++)
            {
                string customKey = dgvGeneratedKeys.Rows[i].Cells[2].Value?.ToString();
                if (!string.IsNullOrEmpty(customKey) && customKey != _generatedKeys[i].GeneratedKey)
                {
                    _generatedKeys[i].GeneratedKey = customKey;
                    _generatedKeys[i].IsCustomized = true;
                }
            }

            StatusChanged?.Invoke("已应用自定义本地化键");
            KeysGenerated?.Invoke(new List<LocalizationKeyItem>(_generatedKeys));
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
        public List<SearchResultItem> SearchResults { get; set; } = new List<SearchResultItem>();
    }
}
