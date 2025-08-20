using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using KSPLocalizationTool.Models;
using KSPLocalizationTool.Services;

namespace KSPLocalizationTool
{
    public partial class MainForm : Form
    {
        private readonly List<LocalizationItem> _foundItems = new List<LocalizationItem>();
        private string _targetLanguageCode = "zh-cn";

        private SearchService? _searchService;
        private LocalizationFileHandler? _localizationHandler;
        private ReplacementService? _replacementService;
        private BackupManager? _backupManager;

        private IContainer? components;
        private readonly TabControl tabControl1 = new TabControl();
        private readonly TabPage tabPageSettings = new TabPage();
        private readonly TabPage tabPageResults = new TabPage();
        private readonly Button btnSelectModDir = new Button();
        private readonly TextBox txtModDir = new TextBox();
        private readonly Label label1 = new Label();
        private readonly Button btnSelectLocalizationDir = new Button();
        private readonly TextBox txtLocalizationDir = new TextBox();
        private readonly Label label3 = new Label();
        private readonly Button btnSelectBackupDir = new Button();
        private readonly TextBox txtBackupDir = new TextBox();
        private readonly Label label2 = new Label();
        private readonly ComboBox cboTargetLanguage = new ComboBox();
        private readonly Label label4 = new Label();
        private readonly Button btnStartSearch = new Button();
        private readonly DataGridView dgvResults = new DataGridView();
        private readonly Button btnReplaceAll = new Button();
        private readonly ProgressBar progressBar = new ProgressBar();
        private readonly TabPage tabPageLogs = new TabPage();
        private readonly TextBox txtLog = new TextBox();
        private readonly StatusStrip statusStrip1 = new StatusStrip();
        private readonly ToolStripStatusLabel toolStripStatusLabel = new ToolStripStatusLabel();
        // 新增的字段
        private HardcodedSearchService? _hardcodedSearchService;
        private LocalizationGeneratorService? _localizationGenerator;
        private readonly List<LocalizationItem> _hardcodedItems = new List<LocalizationItem>();
        private bool _searchingParameters = false;
        private bool _searchingHardcoded = false;

        public MainForm()
        {
            InitializeComponent();
            SetupDataGridView();
            InitializeServices();
        }

        // 初始化数据网格视图
        private void SetupDataGridView()
        {
            dgvResults.AutoGenerateColumns = false;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.Dock = DockStyle.Top;
            dgvResults.ReadOnly = false;
            dgvResults.RowHeadersVisible = false;
            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResults.CellEndEdit += DgvResults_CellEndEdit;

            // 设置列
            DataGridViewTextBoxColumn keyColumn = new DataGridViewTextBoxColumn
            {
                Name = "Key",
                HeaderText = "本地化键",
                DataPropertyName = "Key",
                ReadOnly = true,
                FillWeight = 25
            };

            DataGridViewTextBoxColumn originalColumn = new DataGridViewTextBoxColumn
            {
                Name = "OriginalText",
                HeaderText = "原始文本",
                DataPropertyName = "OriginalText",
                ReadOnly = true,
                FillWeight = 30
            };

            DataGridViewTextBoxColumn localizedColumn = new DataGridViewTextBoxColumn
            {
                Name = "LocalizedText",
                HeaderText = "本地化文本",
                DataPropertyName = "LocalizedText",
                FillWeight = 30
            };

            DataGridViewTextBoxColumn fileColumn = new DataGridViewTextBoxColumn
            {
                Name = "FilePath",
                HeaderText = "文件路径",
                DataPropertyName = "FilePath",
                ReadOnly = true,
                FillWeight = 15
            };

            dgvResults.Columns.AddRange(keyColumn, originalColumn, localizedColumn, fileColumn);

            // 添加上下文菜单，区分参数和硬编码结果
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem parameterSearchItem = new ToolStripMenuItem("搜索参数文本");
            parameterSearchItem.Click += (_, _) => StartParameterSearch();
            ToolStripMenuItem hardcodedSearchItem = new ToolStripMenuItem("搜索硬编码文本");
            hardcodedSearchItem.Click += (_, _) => StartHardcodedSearch();
            contextMenu.Items.AddRange(new ToolStripItem[] { parameterSearchItem, hardcodedSearchItem });
            dgvResults.ContextMenuStrip = contextMenu;
        }

        // 初始化所有服务
        private void InitializeServices()
        {
            try
            {
                string modDir = txtModDir.Text;
                string locDir = txtLocalizationDir.Text;
                string backupDir = txtBackupDir.Text;

                // 初始化备份目录（默认为MOD目录下的Backup文件夹）
                if (string.IsNullOrEmpty(backupDir) && !string.IsNullOrEmpty(modDir))
                {
                    backupDir = Path.Combine(modDir, "Backup");
                    txtBackupDir.Text = backupDir;
                }

                _backupManager = new BackupManager(backupDir);
                _searchService = !string.IsNullOrEmpty(modDir) ? new SearchService(modDir) : null;
                _hardcodedSearchService = !string.IsNullOrEmpty(modDir) ? new HardcodedSearchService(modDir) : null;
                _localizationHandler = !string.IsNullOrEmpty(locDir) ?
                    new LocalizationFileHandler(_targetLanguageCode, locDir) : null;
                _localizationGenerator = !string.IsNullOrEmpty(locDir) ?
                    new LocalizationGeneratorService(locDir, _targetLanguageCode) : null;

                if (_localizationHandler != null && _backupManager != null)
                {
                    _replacementService = new ReplacementService(_localizationHandler, _backupManager);
                    _replacementService.ProgressUpdated += ProgressUpdated;
                    _replacementService.ReplacementCompleted += ReplacementCompleted;
                }

                LogManager.Log("服务初始化完成");
            }
            catch (Exception ex)
            {
                LogManager.Log($"服务初始化失败: {ex.Message}");
            }
        }

        // 更新所有服务
        private void UpdateServices()
        {
            try
            {
                string modDir = txtModDir.Text;
                string locDir = txtLocalizationDir.Text;
                string backupDir = txtBackupDir.Text;

                // 更新备份目录
                if (_backupManager != null)
                {
                    _backupManager.SetBackupDirectory(backupDir);
                }
                else if (!string.IsNullOrEmpty(backupDir))
                {
                    _backupManager = new BackupManager(backupDir);
                }

                // 更新搜索服务
                if (_searchService != null)
                {
                    _searchService.UpdateSearchDirectory(modDir);
                }
                else if (!string.IsNullOrEmpty(modDir))
                {
                    _searchService = new SearchService(modDir);
                }

                // 更新硬编码搜索服务
                if (_hardcodedSearchService != null)
                {
                    _hardcodedSearchService.UpdateSearchDirectory(modDir);
                }
                else if (!string.IsNullOrEmpty(modDir))
                {
                    _hardcodedSearchService = new HardcodedSearchService(modDir);
                }

                // 更新本地化处理器
                if (_localizationHandler != null)
                {
                    _localizationHandler.UpdateDirectory(locDir);
                    _localizationHandler.SetTargetLanguage(_targetLanguageCode);
                }
                else if (!string.IsNullOrEmpty(locDir))
                {
                    _localizationHandler = new LocalizationFileHandler(_targetLanguageCode, locDir);
                }

                // 更新本地化生成器
                if (_localizationGenerator != null)
                {
                    _localizationGenerator.UpdateSettings(locDir, _targetLanguageCode);
                }
                else if (!string.IsNullOrEmpty(locDir))
                {
                    _localizationGenerator = new LocalizationGeneratorService(locDir, _targetLanguageCode);
                }

                // 更新替换服务
                if (_localizationHandler != null && _backupManager != null)
                {
                    if (_replacementService == null)
                    {
                        _replacementService = new ReplacementService(_localizationHandler, _backupManager);
                        _replacementService.ProgressUpdated += ProgressUpdated;
                        _replacementService.ReplacementCompleted += ReplacementCompleted;
                    }
                }
                else
                {
                    _replacementService = null;
                }

                LogManager.Log("服务已更新");
            }
            catch (Exception ex)
            {
                LogManager.Log($"更新服务失败: {ex.Message}");
            }
        }

        // 开始参数搜索
        private void StartParameterSearch()
        {
            // 实现参数搜索逻辑
            if (string.IsNullOrEmpty(txtModDir.Text) || !Directory.Exists(txtModDir.Text))
            {
                MessageBox.Show("请选择有效的MOD目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                toolStripStatusLabel.Text = "正在搜索参数文本...";
                btnStartSearch.Enabled = false;
                btnReplaceAll.Enabled = false;
                _searchingParameters = true;
                _searchingHardcoded = false;

                Task.Run(() =>
                {
                    UpdateServices();
                    var results = _searchService?.SearchLocalizationItems() ?? new List<LocalizationItem>();

                    Invoke(new Action(() =>
                    {
                        _foundItems.Clear();
                        _foundItems.AddRange(results);
                        dgvResults.DataSource = _foundItems.ToList();
                        toolStripStatusLabel.Text = $"找到 {results.Count} 个参数项";
                        btnStartSearch.Enabled = true;
                        btnReplaceAll.Enabled = results.Count > 0;
                    }));
                });
            }
            catch (Exception ex)
            {
                LogManager.Log($"参数搜索失败: {ex.Message}");
                toolStripStatusLabel.Text = "搜索失败";
                btnStartSearch.Enabled = true;
            }
        }

        // 开始硬编码搜索
        private void StartHardcodedSearch()
        {
            // 实现硬编码搜索逻辑
            if (string.IsNullOrEmpty(txtModDir.Text) || !Directory.Exists(txtModDir.Text))
            {
                MessageBox.Show("请选择有效的MOD目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                toolStripStatusLabel.Text = "正在搜索硬编码文本...";
                btnStartSearch.Enabled = false;
                btnReplaceAll.Enabled = false;
                _searchingParameters = false;
                _searchingHardcoded = true;

                Task.Run(() =>
                {
                    UpdateServices();
                    var results = _hardcodedSearchService?.SearchHardcodedItems(txtLocalizationDir.Text) ?? new List<LocalizationItem>();

                    // 为硬编码文本生成本地化键
                    foreach (var item in results)
                    {
                        if (string.IsNullOrEmpty(item.Key))
                        {
                            item.Key = LocalizationKeyGenerator.GenerateKey(
                                item.OriginalText,
                                "HARDCODED",
                                item.FilePath);
                        }
                    }

                    Invoke(new Action(() =>
                    {
                        _hardcodedItems.Clear();
                        _hardcodedItems.AddRange(results);
                        _foundItems.Clear();
                        _foundItems.AddRange(results);
                        dgvResults.DataSource = _foundItems.ToList();
                        toolStripStatusLabel.Text = $"找到 {results.Count} 个硬编码项";
                        btnStartSearch.Enabled = true;
                        btnReplaceAll.Enabled = results.Count > 0;
                    }));
                });
            }
            catch (Exception ex)
            {
                LogManager.Log($"硬编码搜索失败: {ex.Message}");
                toolStripStatusLabel.Text = "搜索失败";
                btnStartSearch.Enabled = true;
            }
        }

        // 替换所有项
        private void BtnReplaceAll_Click(object? sender, EventArgs e)
        {
            var itemsToReplace = _foundItems.Where(i => !string.IsNullOrEmpty(i.LocalizedText)).ToList();

            if (itemsToReplace.Count == 0)
            {
                MessageBox.Show("没有可替换的本地化项，请先填写本地化文本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"确定要替换所有 {itemsToReplace.Count} 项本地化内容吗？\n这将修改源文件并创建备份。",
                "确认替换",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmResult != DialogResult.Yes)
                return;

            try
            {
                toolStripStatusLabel.Text = "正在执行替换...";
                progressBar.Value = 0;
                btnReplaceAll.Enabled = false;
                btnStartSearch.Enabled = false;

                // 生成或更新本地化文件
                if (_localizationGenerator != null)
                {
                    _localizationGenerator.GenerateOrUpdateLocalizationFiles(itemsToReplace);
                }

                // 在后台线程执行替换
                Task.Run(() =>
                {
                    if (_searchingParameters)
                    {
                        ReplaceParameterItems(itemsToReplace);
                    }
                    else if (_searchingHardcoded)
                    {
                        ReplaceHardcodedItems(itemsToReplace);
                    }

                    // 替换完成后更新UI
                    Invoke(new Action(() =>
                    {
                        toolStripStatusLabel.Text = "替换完成";
                        progressBar.Value = 100;
                        btnReplaceAll.Enabled = true;
                        btnStartSearch.Enabled = true;

                        MessageBox.Show($"成功替换 {itemsToReplace.Count} 项本地化内容",
                            "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                });
            }
            catch (Exception ex)
            {
                LogManager.Log($"替换失败: {ex.Message}");
                MessageBox.Show($"替换时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabel.Text = "替换失败";
                btnReplaceAll.Enabled = true;
                btnStartSearch.Enabled = true;
            }
        }

        // 替换参数项
        private void ReplaceParameterItems(List<LocalizationItem> items)
        {
            // 备份所有相关文件
            var filesToBackup = items.Select(i => i.FilePath).Distinct().ToList();
            if (_backupManager != null)
            {
                _backupManager.BackupFiles(filesToBackup);
            }

            foreach (var item in items)
            {
                try
                {
                    if (File.Exists(item.FilePath))
                    {
                        string[] lines = File.ReadAllLines(item.FilePath);
                        bool fileModified = false;

                        for (int i = 0; i < lines.Length; i++)
                        {
                            // 查找包含原始文本的行
                            if (lines[i].Contains(item.OriginalText) &&
                                GetParameterList().Any(p => lines[i].Contains(p)))
                            {
                                // 创建原始代码的注释
                                string originalLine = lines[i];
                                // 替换为本地化键
                                string paramName = GetParameterList().First(p => lines[i].Contains(p));
                                lines[i] = $"{paramName} = {item.Key}";
                                // 在下方添加原始代码作为注释
                                lines[i] += $"\n// 原始代码: {originalLine}";

                                fileModified = true;
                                LogManager.Log($"已替换文件 {Path.GetFileName(item.FilePath)} 中的项: {item.Key}");
                            }
                        }

                        if (fileModified)
                        {
                            File.WriteAllLines(item.FilePath, lines);
                        }
                    }

                    // 更新进度
                    int progress = (int)(((double)items.IndexOf(item) / items.Count) * 100);
                    ProgressUpdated(progress);
                }
                catch (Exception ex)
                {
                    LogManager.Log($"替换项 {item.Key} 失败: {ex.Message}");
                }
            }
        }

        // 替换硬编码项
        private void ReplaceHardcodedItems(List<LocalizationItem> items)
        {
            // 备份所有相关文件
            var filesToBackup = items.Select(i => i.FilePath).Distinct().ToList();
            if (_backupManager != null)
            {
                _backupManager.BackupFiles(filesToBackup);
            }

            foreach (var item in items)
            {
                try
                {
                    if (File.Exists(item.FilePath))
                    {
                        string[] lines = File.ReadAllLines(item.FilePath);
                        bool fileModified = false;

                        for (int i = 0; i < lines.Length; i++)
                        {
                            // 查找包含原始文本的行
                            if (lines[i].Contains(item.OriginalText) &&
                                GetHardcodedParameterList().Any(p => lines[i].Contains(p)))
                            {
                                // 创建原始代码的注释
                                string originalLine = lines[i];
                                // 替换为本地化键
                                lines[i] = lines[i].Replace($"\"{item.OriginalText}\"", $"\"{item.Key}\"")
                                                  .Replace($"'{item.OriginalText}'", $"'{item.Key}'");
                                // 在下方添加原始代码作为注释
                                lines[i] += $"\n// 原始代码: {originalLine}";

                                fileModified = true;
                                LogManager.Log($"已替换文件 {Path.GetFileName(item.FilePath)} 中的硬编码项: {item.Key}");
                            }
                        }

                        if (fileModified)
                        {
                            File.WriteAllLines(item.FilePath, lines);
                        }
                    }

                    // 更新进度
                    int progress = (int)(((double)items.IndexOf(item) / items.Count) * 100);
                    ProgressUpdated(progress);
                }
                catch (Exception ex)
                {
                    LogManager.Log($"替换硬编码项 {item.Key} 失败: {ex.Message}");
                }
            }
        }

        #region 初始化方法
        private void InitializeComponent()
        {
            // 初始化组件容器
            components = new Container();

            // 配置主窗体
            SuspendLayout();
            Size = new Size(800, 600);
            Text = "KSP本地化工具";
            Name = "MainForm";

            // 配置tabControl1
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(800, 578);
            tabControl1.TabIndex = 0;
            tabControl1.TabPages.AddRange(new TabPage[] { tabPageSettings, tabPageResults, tabPageLogs });

            // 配置tabPageSettings
            tabPageSettings.Controls.Add(label1);
            tabPageSettings.Controls.Add(txtModDir);
            tabPageSettings.Controls.Add(btnSelectModDir);
            tabPageSettings.Controls.Add(label3);
            tabPageSettings.Controls.Add(txtLocalizationDir);
            tabPageSettings.Controls.Add(btnSelectLocalizationDir);
            tabPageSettings.Controls.Add(label2);
            tabPageSettings.Controls.Add(txtBackupDir);
            tabPageSettings.Controls.Add(btnSelectBackupDir);
            tabPageSettings.Controls.Add(label4);
            tabPageSettings.Controls.Add(cboTargetLanguage);
            tabPageSettings.Controls.Add(btnStartSearch);
            tabPageSettings.Location = new Point(4, 24);
            tabPageSettings.Name = "tabPageSettings";
            tabPageSettings.Padding = new Padding(3);
            tabPageSettings.Size = new Size(792, 550);
            tabPageSettings.TabIndex = 0;
            tabPageSettings.Text = "设置";
            tabPageSettings.UseVisualStyleBackColor = true;

            // 配置label1
            label1.AutoSize = true;
            label1.Location = new Point(15, 20);
            label1.Name = "label1";
            label1.Size = new Size(53, 12);
            label1.TabIndex = 0;
            label1.Text = "MOD目录";

            // 配置txtModDir
            txtModDir.Location = new Point(15, 35);
            txtModDir.Name = "txtModDir";
            txtModDir.Size = new Size(600, 21);
            txtModDir.TabIndex = 1;

            // 配置btnSelectModDir
            btnSelectModDir.Location = new Point(620, 35);
            btnSelectModDir.Name = "btnSelectModDir";
            btnSelectModDir.Size = new Size(75, 23);
            btnSelectModDir.TabIndex = 2;
            btnSelectModDir.Text = "浏览...";
            btnSelectModDir.UseVisualStyleBackColor = true;
            btnSelectModDir.Click += BtnSelectModDir_Click;

            // 配置label3
            label3.AutoSize = true;
            label3.Location = new Point(15, 70);
            label3.Name = "label3";
            label3.Size = new Size(77, 12);
            label3.TabIndex = 3;
            label3.Text = "本地化文件目录";

            // 配置txtLocalizationDir
            txtLocalizationDir.Location = new Point(15, 85);
            txtLocalizationDir.Name = "txtLocalizationDir";
            txtLocalizationDir.Size = new Size(600, 21);
            txtLocalizationDir.TabIndex = 4;

            // 配置btnSelectLocalizationDir
            btnSelectLocalizationDir.Location = new Point(620, 85);
            btnSelectLocalizationDir.Name = "btnSelectLocalizationDir";
            btnSelectLocalizationDir.Size = new Size(75, 23);
            btnSelectLocalizationDir.TabIndex = 5;
            btnSelectLocalizationDir.Text = "浏览...";
            btnSelectLocalizationDir.UseVisualStyleBackColor = true;
            btnSelectLocalizationDir.Click += BtnSelectLocalizationDir_Click;

            // 配置label2
            label2.AutoSize = true;
            label2.Location = new Point(15, 120);
            label2.Name = "label2";
            label2.Size = new Size(53, 12);
            label2.TabIndex = 6;
            label2.Text = "备份目录";

            // 配置txtBackupDir
            txtBackupDir.Location = new Point(15, 135);
            txtBackupDir.Name = "txtBackupDir";
            txtBackupDir.Size = new Size(600, 21);
            txtBackupDir.TabIndex = 7;

            // 配置btnSelectBackupDir
            btnSelectBackupDir.Location = new Point(620, 135);
            btnSelectBackupDir.Name = "btnSelectBackupDir";
            btnSelectBackupDir.Size = new Size(75, 23);
            btnSelectBackupDir.TabIndex = 8;
            btnSelectBackupDir.Text = "浏览...";
            btnSelectBackupDir.UseVisualStyleBackColor = true;
            btnSelectBackupDir.Click += BtnSelectBackupDir_Click;

            // 配置label4
            label4.AutoSize = true;
            label4.Location = new Point(15, 170);
            label4.Name = "label4";
            label4.Size = new Size(53, 12);
            label4.TabIndex = 9;
            label4.Text = "目标语言";

            // 配置cboTargetLanguage
            cboTargetLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTargetLanguage.FormattingEnabled = true;
            cboTargetLanguage.Items.AddRange(new object[] { "zh-cn", "en-us", "ja", "ko", "fr", "de" });
            cboTargetLanguage.Location = new Point(15, 185);
            cboTargetLanguage.Name = "cboTargetLanguage";
            cboTargetLanguage.Size = new Size(121, 20);
            cboTargetLanguage.TabIndex = 10;
            cboTargetLanguage.SelectedIndex = 0;
            cboTargetLanguage.SelectedIndexChanged += CboTargetLanguage_SelectedIndexChanged;

            // 配置btnStartSearch
            btnStartSearch.Location = new Point(15, 220);
            btnStartSearch.Name = "btnStartSearch";
            btnStartSearch.Size = new Size(121, 30);
            btnStartSearch.TabIndex = 11;
            btnStartSearch.Text = "开始搜索";
            btnStartSearch.UseVisualStyleBackColor = true;
            btnStartSearch.Click += BtnStartSearch_Click;

            // 配置tabPageResults
            tabPageResults.Controls.Add(progressBar);
            tabPageResults.Controls.Add(btnReplaceAll);
            tabPageResults.Controls.Add(dgvResults);
            tabPageResults.Location = new Point(4, 24);
            tabPageResults.Name = "tabPageResults";
            tabPageResults.Padding = new Padding(3);
            tabPageResults.Size = new Size(792, 550);
            tabPageResults.TabIndex = 1;
            tabPageResults.Text = "结果";
            tabPageResults.UseVisualStyleBackColor = true;

            // 配置dgvResults
            dgvResults.Location = new Point(3, 3);
            dgvResults.Name = "dgvResults";
            dgvResults.Size = new Size(786, 480);
            dgvResults.TabIndex = 0;

            // 配置btnReplaceAll
            btnReplaceAll.Location = new Point(3, 489);
            btnReplaceAll.Name = "btnReplaceAll";
            btnReplaceAll.Size = new Size(121, 30);
            btnReplaceAll.TabIndex = 1;
            btnReplaceAll.Text = "全部替换";
            btnReplaceAll.UseVisualStyleBackColor = true;
            btnReplaceAll.Click += BtnReplaceAll_Click;
            btnReplaceAll.Enabled = false;

            // 配置progressBar
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Location = new Point(3, 525);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(786, 22);
            progressBar.TabIndex = 2;

            // 配置tabPageLogs
            tabPageLogs.Controls.Add(txtLog);
            tabPageLogs.Location = new Point(4, 24);
            tabPageLogs.Name = "tabPageLogs";
            tabPageLogs.Padding = new Padding(3);
            tabPageLogs.Size = new Size(792, 550);
            tabPageLogs.TabIndex = 2;
            tabPageLogs.Text = "日志";
            tabPageLogs.UseVisualStyleBackColor = true;

            // 配置txtLog
            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(786, 544);
            txtLog.TabIndex = 0;

            // 配置statusStrip1
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel });
            statusStrip1.Location = new Point(0, 578);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(800, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";

            // 配置toolStripStatusLabel
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(31, 17);
            toolStripStatusLabel.Text = "就绪";

            // 配置主窗体控件
            Controls.Add(tabControl1);
            Controls.Add(statusStrip1);

            // 完成初始化
            ResumeLayout(false);
            PerformLayout();

            // 注册日志更新事件
            LogManager.Log("程序初始化完成");
            UpdateLogDisplay();
        }
        #endregion

        #region 事件处理方法
        private void BtnSelectModDir_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择MOD目录";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtModDir.Text = dialog.SelectedPath;
                UpdateServices();
            }
        }

        private void BtnSelectLocalizationDir_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择本地化文件目录";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtLocalizationDir.Text = dialog.SelectedPath;
                UpdateServices();
            }
        }

        private void BtnSelectBackupDir_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择备份目录";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtBackupDir.Text = dialog.SelectedPath;
                UpdateServices();
            }
        }

        private void CboTargetLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboTargetLanguage.SelectedItem != null)
            {
                _targetLanguageCode = cboTargetLanguage.SelectedItem.ToString() ?? "zh-cn";
                UpdateServices();
            }
        }

        private void BtnStartSearch_Click(object? sender, EventArgs e)
        {
            // 显示搜索选项对话框
            var result = MessageBox.Show(
                "请选择搜索类型:\n- 点击确定搜索参数文本\n- 点击取消搜索硬编码文本",
                "选择搜索类型",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.OK)
            {
                StartParameterSearch();
            }
            else
            {
                StartHardcodedSearch();
            }
        }

        private void DgvResults_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            // 当用户编辑本地化文本后更新数据
            if (e.ColumnIndex == dgvResults.Columns["LocalizedText"].Index && e.RowIndex >= 0)
            {
                var item = _foundItems[e.RowIndex];
                item.LocalizedText = dgvResults.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
            }
        }

        private void ProgressUpdated(int progress)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action<int>(ProgressUpdated), progress);
                return;
            }

            progressBar.Value = progress;
            toolStripStatusLabel.Text = $"正在替换... {progress}%";
        }

        private void ReplacementCompleted(bool success, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool, string>(ReplacementCompleted), success, message);
                return;
            }

            if (success)
            {
                toolStripStatusLabel.Text = "替换完成";
                MessageBox.Show("所有本地化项已成功替换", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                toolStripStatusLabel.Text = "替换失败";
                MessageBox.Show($"替换过程中出现错误: {message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            btnReplaceAll.Enabled = true;
            btnStartSearch.Enabled = true;
        }

        private void UpdateLogDisplay()
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(UpdateLogDisplay));
                return;
            }

            var logEntries = LogManager.GetLogEntries();
            txtLog.Text = string.Join(Environment.NewLine, logEntries);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void SaveCurrentSettings()
        {
            // 实现保存设置的逻辑
            var settings = new Dictionary<string, string>
            {
                { "ModDirectory", txtModDir.Text },
                { "LocalizationDirectory", txtLocalizationDir.Text },
                { "BackupDirectory", txtBackupDir.Text },
                { "TargetLanguage", _targetLanguageCode }
            };

            // 保存到配置文件
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configDir = Path.Combine(appData, "KSPLocalizationTool");
                Directory.CreateDirectory(configDir);
                string configPath = Path.Combine(configDir, "settings.ini");

                using var writer = new StreamWriter(configPath);
                foreach (var pair in settings)
                {
                    writer.WriteLine($"{pair.Key}={pair.Value}");
                }

                LogManager.Log("设置已保存");
            }
            catch (Exception ex)
            {
                LogManager.Log($"保存设置失败: {ex.Message}");
            }
        }
        #endregion

        #region 辅助方法
        private static List<string> GetParameterList()
        {
            // 返回参数列表
            return new List<string> { "name", "title", "description", "tooltip", "message", "text", "label" };
        }

        private static List<string> GetHardcodedParameterList()
        {
            // 返回硬编码参数列表
            return new List<string> { "Debug.Log", "Console.WriteLine", "GUILayout.Label", "GUILayout.Button", "MessageBox.Show" };
        }
        #endregion
    }
}
