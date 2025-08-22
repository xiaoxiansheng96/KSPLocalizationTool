using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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
        private CancellationTokenSource? _searchCancellationTokenSource;
        private readonly Button btnCancelSearch = new Button();

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
        private HardcodedSearchService? _hardcodedSearchService;
        private LocalizationGeneratorService? _localizationGenerator;
        private readonly List<LocalizationItem> _hardcodedItems = new List<LocalizationItem>();

        // 备份管理相关控件
        private readonly Label label5 = new Label();
        private readonly ComboBox cboBackups = new ComboBox();
        private readonly Button btnRefreshBackups = new Button();
        private readonly Button btnRestoreBackup = new Button();
        private readonly Label backupStatusLabel = new Label();

        // 日志页面新增清空按钮
        private readonly Button btnClearLog = new Button();

        // 搜索选项
        private readonly CheckBox chkSearchParameters = new CheckBox();
        private readonly CheckBox chkSearchHardcoded = new CheckBox();

        public MainForm()
        {
            InitializeComponent();
            SetupDataGridView();
            InitializeServices();
            LoadSettings();
            UpdateLogDisplay();

            // 设置最小窗口尺寸，确保所有控件能正常显示
            MinimumSize = new Size(1024, 768);
            // 监听窗口大小变化，动态调整控件
            Resize += MainForm_Resize;

            // 订阅日志更新事件
            LogManager.Logged += OnLogUpdated;

            // 初始化取消按钮
            SetupCancelButton();
        }

        private void SetupCancelButton()
        {
            btnCancelSearch.Text = "取消搜索";
            btnCancelSearch.Enabled = false;
            btnCancelSearch.Size = new Size(100, 35);
            btnCancelSearch.Font = new Font(btnCancelSearch.Font, FontStyle.Bold);
            btnCancelSearch.Click += BtnCancelSearch_Click;
        }

        private void BtnCancelSearch_Click(object? sender, EventArgs e)
        {
            if (_searchCancellationTokenSource != null && !_searchCancellationTokenSource.IsCancellationRequested)
            {
                _searchCancellationTokenSource.Cancel();
                toolStripStatusLabel.Text = "正在取消搜索...";
                btnCancelSearch.Enabled = false;
            }
        }

        // 日志更新事件处理
        private void OnLogUpdated(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(OnLogUpdated), message);
            }
            else
            {
                UpdateLogDisplay();
            }
        }

        // 窗口大小变化时调整控件布局
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            AdjustResultPageLayout();
            AdjustSettingsPageLayout();
        }

        // 调整结果页面布局
        private void AdjustResultPageLayout()
        {
            if (tabPageResults.ClientSize.Height <= 0) return;

            // 底部控件区域高度（按钮+进度条+间距）
            const int bottomAreaHeight = 70;
            var availableHeight = tabPageResults.ClientSize.Height - bottomAreaHeight;

            // 调整数据网格高度
            dgvResults.Height = Math.Max(availableHeight, 200); // 确保最小高度
        }

        // 调整设置页面布局
        private void AdjustSettingsPageLayout()
        {
            if (tabPageSettings.Controls[0] is TableLayoutPanel mainTable)
            {
                mainTable.Width = tabPageSettings.ClientSize.Width - 20; // 减去边距
            }
        }

        private void InitializeComponent()
        {
            // 基础窗体设置
            Text = "KSP本地化工具";
            Size = new Size(1280, 800);
            StartPosition = FormStartPosition.CenterScreen;

            // TabControl设置
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.TabPages.Add(tabPageSettings);
            tabControl1.TabPages.Add(tabPageResults);
            tabControl1.TabPages.Add(tabPageLogs);
            tabPageSettings.Text = "设置";
            tabPageResults.Text = "结果";
            tabPageLogs.Text = "日志";

            // 日志页面设置
            SetupLogPage();

            // 设置页面控件布局 - 使用TableLayoutPanel彻底解决干涉问题
            SetupSettingsPageLayout();

            // 结果页面控件
            SetupResultsPage();

            // 状态栏
            toolStripStatusLabel.Text = "就绪";
            statusStrip1.Items.Add(toolStripStatusLabel);

            // 添加主控件
            Controls.Add(tabControl1);
            Controls.Add(statusStrip1);
        }

        private void SetupLogPage()
        {
            // 日志页面使用面板布局
            var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };

            // 清空日志按钮
            btnClearLog.Text = "清空日志";
            btnClearLog.Dock = DockStyle.Top;
            btnClearLog.Height = 30;
            btnClearLog.Margin = new Padding(0, 0, 0, 5);
            btnClearLog.Click += (s, e) => txtLog.Clear();

            // 日志文本框
            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Font = new Font("Consolas", 9f);
            txtLog.Margin = new Padding(0, 5, 0, 0);

            logPanel.Controls.Add(txtLog);
            logPanel.Controls.Add(btnClearLog);
            tabPageLogs.Controls.Add(logPanel);
        }

        private void SetupResultsPage()
        {
            tabPageResults.Padding = new Padding(10);

            // 底部控制面板使用TableLayoutPanel避免重叠
            var bottomTable = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                RowCount = 2,
                Height = 80,
                Padding = new Padding(5)
            };
            bottomTable.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
            bottomTable.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));

            // 替换按钮和取消按钮的容器
            var buttonPanel = new Panel { Dock = DockStyle.Fill };
            
            btnReplaceAll.Text = "替换所有";
            btnReplaceAll.Dock = DockStyle.Left;
            btnReplaceAll.Width = 150;
            btnReplaceAll.Margin = new Padding(0, 0, 5, 5);
            btnReplaceAll.Click += BtnReplaceAll_Click;
            btnReplaceAll.Enabled = false;

            btnCancelSearch.Dock = DockStyle.Left;
            btnCancelSearch.Margin = new Padding(5, 0, 0, 5);
            
            buttonPanel.Controls.Add(btnCancelSearch);
            buttonPanel.Controls.Add(btnReplaceAll);

            progressBar.Dock = DockStyle.Fill;
            progressBar.Margin = new Padding(0, 5, 0, 0);
            progressBar.Value = 0;

            bottomTable.Controls.Add(buttonPanel, 0, 0);
            bottomTable.Controls.Add(progressBar, 0, 1);

            // 数据网格视图
            dgvResults.Dock = DockStyle.Fill;
            dgvResults.Margin = new Padding(0, 0, 0, 10);
            dgvResults.BorderStyle = BorderStyle.Fixed3D;

            tabPageResults.Controls.Add(dgvResults);
            tabPageResults.Controls.Add(bottomTable);
        }

        private void SetupSettingsPageLayout()
        {
            // 使用TableLayoutPanel实现精确布局，彻底解决控件干涉
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20),
                RowCount = 8, // 增加两行用于搜索选项
                ColumnCount = 1
            };

            // 设置行样式 - 自动调整高度
            for (int i = 0; i < mainTable.RowCount; i++)
            {
                mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
            }
            // 最后一行用于搜索按钮，设置更大高度
            mainTable.RowStyles[7] = new RowStyle(SizeType.Absolute, 50f);

            // 1. MOD目录行
            var modDirTable = CreateDirectoryRow("MOD目录:", txtModDir, btnSelectModDir);
            btnSelectModDir.Text = "浏览...";
            btnSelectModDir.Click += BtnSelectModDir_Click;
            mainTable.Controls.Add(modDirTable, 0, 0);

            // 2. 本地化目录行
            var locDirTable = CreateDirectoryRow("本地化目录:", txtLocalizationDir, btnSelectLocalizationDir);
            btnSelectLocalizationDir.Text = "浏览...";
            btnSelectLocalizationDir.Click += BtnSelectLocalizationDir_Click;
            mainTable.Controls.Add(locDirTable, 0, 1);

            // 3. 备份目录行
            var backupDirTable = CreateDirectoryRow("备份目录:", txtBackupDir, btnSelectBackupDir);
            btnSelectBackupDir.Text = "浏览...";
            btnSelectBackupDir.Click += BtnSelectBackupDir_Click;
            mainTable.Controls.Add(backupDirTable, 0, 2);

            // 4. 目标语言行
            var langTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            langTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f)); // 标签宽度固定
            langTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // 控件占剩余宽度

            label4.Text = "目标语言:";
            label4.Dock = DockStyle.Left;
            label4.TextAlign = ContentAlignment.MiddleLeft;
            label4.Margin = new Padding(0, 5, 0, 0);

            cboTargetLanguage.Items.AddRange(new[] { "zh-cn", "en-us", "ja-jp", "ko-kr" });
            cboTargetLanguage.SelectedItem = _targetLanguageCode;
            cboTargetLanguage.SelectedIndexChanged += CboTargetLanguage_SelectedIndexChanged;
            cboTargetLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTargetLanguage.Dock = DockStyle.Fill;
            cboTargetLanguage.Margin = new Padding(5);
            cboTargetLanguage.Height = 25;

            langTable.Controls.Add(label4, 0, 0);
            langTable.Controls.Add(cboTargetLanguage, 1, 0);
            mainTable.Controls.Add(langTable, 0, 3);

            // 5. 搜索选项行
            var searchOptionsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            searchOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            searchOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            chkSearchParameters.Text = "搜索参数文本";
            chkSearchParameters.Checked = true;
            chkSearchParameters.Dock = DockStyle.Left;
            chkSearchParameters.Margin = new Padding(5);

            chkSearchHardcoded.Text = "搜索硬编码文本";
            chkSearchHardcoded.Checked = true;
            chkSearchHardcoded.Dock = DockStyle.Left;
            chkSearchHardcoded.Margin = new Padding(5);

            searchOptionsTable.Controls.Add(chkSearchParameters, 0, 0);
            searchOptionsTable.Controls.Add(chkSearchHardcoded, 1, 0);
            mainTable.Controls.Add(searchOptionsTable, 0, 4);

            // 6. 空白行（分隔用）
            var spacerPanel = new Panel { Dock = DockStyle.Fill };
            mainTable.Controls.Add(spacerPanel, 0, 5);

            // 7. 备份管理行 - 使用4列布局避免干涉
            var backupTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            backupTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f)); // 标签
            backupTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));   // 备份列表
            backupTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));  // 刷新按钮
            backupTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));  // 恢复按钮

            label5.Text = "备份管理:";
            label5.Dock = DockStyle.Left;
            label5.TextAlign = ContentAlignment.MiddleLeft;
            label5.Margin = new Padding(0, 5, 0, 0);

            cboBackups.Dock = DockStyle.Fill;
            cboBackups.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBackups.Margin = new Padding(5);

            btnRefreshBackups.Text = "刷新";
            btnRefreshBackups.Dock = DockStyle.Fill;
            btnRefreshBackups.Margin = new Padding(5);

            btnRestoreBackup.Text = "恢复";
            btnRestoreBackup.Dock = DockStyle.Fill;
            btnRestoreBackup.Margin = new Padding(5);
            btnRestoreBackup.Enabled = false;

            backupTable.Controls.Add(label5, 0, 0);
            backupTable.Controls.Add(cboBackups, 1, 0);
            backupTable.Controls.Add(btnRefreshBackups, 2, 0);
            backupTable.Controls.Add(btnRestoreBackup, 3, 0);
            mainTable.Controls.Add(backupTable, 0, 6);

            // 8. 搜索按钮行
            var buttonPanel = new Panel { Dock = DockStyle.Fill };
            btnStartSearch.Text = "开始搜索";
            btnStartSearch.Size = new Size(150, 35);
            btnStartSearch.Font = new Font(btnStartSearch.Font, FontStyle.Bold);
            btnStartSearch.Click += BtnStartSearch_Click;

            // 按钮居中显示
            buttonPanel.Controls.Add(btnStartSearch);
            btnStartSearch.Location = new Point(
                (buttonPanel.ClientSize.Width - btnStartSearch.Width) / 2,
                (buttonPanel.ClientSize.Height - btnStartSearch.Height) / 2
            );
            buttonPanel.Resize += (s, e) => {
                btnStartSearch.Location = new Point(
                    (buttonPanel.ClientSize.Width - btnStartSearch.Width) / 2,
                    (buttonPanel.ClientSize.Height - btnStartSearch.Height) / 2
                );
            };
            mainTable.Controls.Add(buttonPanel, 0, 7);

            tabPageSettings.Controls.Add(mainTable);
        }

        // 创建目录选择行的辅助方法
        private TableLayoutPanel CreateDirectoryRow(string labelText, TextBox textBox, Button browseButton)
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f)); // 标签
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // 文本框
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));  // 按钮

            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, 0, 0)
            };

            textBox.Dock = DockStyle.Fill;
            textBox.Margin = new Padding(5);
            textBox.Height = 25;

            browseButton.Dock = DockStyle.Fill;
            browseButton.Margin = new Padding(5);

            table.Controls.Add(label, 0, 0);
            table.Controls.Add(textBox, 1, 0);
            table.Controls.Add(browseButton, 2, 0);

            return table;
        }

        private void SetupDataGridView()
        {
            dgvResults.AutoGenerateColumns = false;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.ReadOnly = false;
            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.MultiSelect = false;
            dgvResults.CellEndEdit += DgvResults_CellEndEdit;

            // 设置列
            var keyColumn = new DataGridViewTextBoxColumn
            {
                Name = "Key",
                HeaderText = "键",
                DataPropertyName = "Key",
                ReadOnly = true,
                Width = 200
            };

            var originalTextColumn = new DataGridViewTextBoxColumn
            {
                Name = "OriginalText",
                HeaderText = "原始文本",
                DataPropertyName = "OriginalText",
                ReadOnly = true,
                Width = 300
            };

            var localizedTextColumn = new DataGridViewTextBoxColumn
            {
                Name = "LocalizedText",
                HeaderText = "本地化文本",
                DataPropertyName = "LocalizedText",
                Width = 300
            };

            var filePathColumn = new DataGridViewTextBoxColumn
            {
                Name = "FilePath",
                HeaderText = "文件路径",
                DataPropertyName = "FilePath",
                ReadOnly = true,
                Width = 300
            };

            var lineNumberColumn = new DataGridViewTextBoxColumn
            {
                Name = "LineNumber",
                HeaderText = "行号",
                DataPropertyName = "LineNumber",
                ReadOnly = true,
                Width = 60
            };

            dgvResults.Columns.AddRange(keyColumn, originalTextColumn, localizedTextColumn, filePathColumn, lineNumberColumn);

            // 添加右键菜单
            var contextMenu = new ContextMenuStrip();
            var parameterSearchItem = new ToolStripMenuItem("搜索参数文本");
            parameterSearchItem.Click += (_, _) => StartParameterSearch();
            var hardcodedSearchItem = new ToolStripMenuItem("搜索硬编码文本");
            hardcodedSearchItem.Click += (_, _) => StartHardcodedSearch();
            contextMenu.Items.AddRange(new ToolStripItem[] { parameterSearchItem, hardcodedSearchItem });
            dgvResults.ContextMenuStrip = contextMenu;
        }

        private void InitializeServices()
        {
            try
            {
                string modDir = txtModDir.Text;
                string locDir = txtLocalizationDir.Text;
                string backupDir = txtBackupDir.Text;

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
                if (!string.IsNullOrEmpty(modDir))
                {
                    if (_searchService != null)
                    {
                        _searchService.UpdateSearchDirectory(modDir);
                    }
                    else
                    {
                        _searchService = new SearchService(modDir);
                    }

                    if (_hardcodedSearchService != null)
                    {
                        _hardcodedSearchService.UpdateSearchDirectory(modDir);
                    }
                    else
                    {
                        _hardcodedSearchService = new HardcodedSearchService(modDir);
                    }
                }

                // 更新本地化处理器
                if (!string.IsNullOrEmpty(locDir))
                {
                    if (_localizationHandler != null)
                    {
                        _localizationHandler.UpdateDirectory(locDir);
                        _localizationHandler.SetTargetLanguage(_targetLanguageCode);
                    }
                    else
                    {
                        _localizationHandler = new LocalizationFileHandler(_targetLanguageCode, locDir);
                    }

                    if (_localizationGenerator != null)
                    {
                        _localizationGenerator.UpdateSettings(locDir, _targetLanguageCode);
                    }
                    else
                    {
                        _localizationGenerator = new LocalizationGeneratorService(locDir, _targetLanguageCode);
                    }
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
                    else
                    {
                        // _replacementService.UpdateDependencies(_localizationHandler, _backupManager);
                        // 注释或删除上面的行，因为ReplacementService通过构造函数已接收依赖项
                    }
                }

                LogManager.Log("服务已更新");
            }
            catch (Exception ex)
            {
                LogManager.Log($"服务更新失败: {ex.Message}");
            }
        }

        private void CboTargetLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboTargetLanguage.SelectedItem != null)
            {
                _targetLanguageCode = cboTargetLanguage.SelectedItem.ToString() ?? "zh-cn";
                if (_localizationHandler != null)
                {
                    _localizationHandler.SetTargetLanguage(_targetLanguageCode);
                }
                if (_localizationGenerator != null)
                {
                    _localizationGenerator.UpdateSettings(txtLocalizationDir.Text, _targetLanguageCode);
                }
            }
        }

        private void BtnSelectModDir_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtModDir.Text = folderDialog.SelectedPath;
                if (string.IsNullOrEmpty(txtBackupDir.Text))
                {
                    txtBackupDir.Text = Path.Combine(folderDialog.SelectedPath, "Backup");
                }
                UpdateServices();
                SaveSettings();
            }
        }

        private void BtnSelectLocalizationDir_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtLocalizationDir.Text = folderDialog.SelectedPath;
                UpdateServices();
                SaveSettings();
            }
        }

        private void BtnSelectBackupDir_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtBackupDir.Text = folderDialog.SelectedPath;
                UpdateServices();
                SaveSettings();
            }
        }

        private void LoadSettings()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configDir = Path.Combine(appData, "KSPLocalizationTool");
                string configPath = Path.Combine(configDir, "config.txt");

                if (File.Exists(configPath))
                {
                    foreach (string line in File.ReadAllLines(configPath))
                    {
                        if (line.StartsWith("ModDir="))
                        {
                            txtModDir.Text = line.Substring(7);
                        }
                        else if (line.StartsWith("LocalizationDir="))
                        {
                            txtLocalizationDir.Text = line.Substring(16);
                        }
                        else if (line.StartsWith("BackupDir="))
                        {
                            txtBackupDir.Text = line.Substring(10);
                        }
                        else if (line.StartsWith("TargetLanguage="))
                        {
                            _targetLanguageCode = line.Substring(15);
                        }
                    }
                }

                // 刷新备份列表
                RefreshBackupList();
            }
            catch (Exception ex)
            {
                LogManager.Log($"加载设置失败: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configDir = Path.Combine(appData, "KSPLocalizationTool");
                Directory.CreateDirectory(configDir);
                string configPath = Path.Combine(configDir, "config.txt");

                var lines = new List<string>
                {
                    $"ModDir={txtModDir.Text}",
                    $"LocalizationDir={txtLocalizationDir.Text}",
                    $"BackupDir={txtBackupDir.Text}",
                    $"TargetLanguage={_targetLanguageCode}"
                };

                File.WriteAllLines(configPath, lines);
                LogManager.Log("设置已保存");
            }
            catch (Exception ex)
            {
                LogManager.Log($"保存设置失败: {ex.Message}");
            }
        }

        private async void BtnStartSearch_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtModDir.Text) || !Directory.Exists(txtModDir.Text))
            {
                MessageBox.Show("请选择有效的MOD目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(txtLocalizationDir.Text) || !Directory.Exists(txtLocalizationDir.Text))
            {
                MessageBox.Show("请选择有效的本地化目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 禁用搜索按钮防止重复点击，启用取消按钮
            btnStartSearch.Enabled = false;
            btnCancelSearch.Enabled = true;
            toolStripStatusLabel.Text = "正在搜索...";
            _foundItems.Clear();
            progressBar.Value = 0;

            try
            {
                // 更新服务确保使用最新的目录设置
                UpdateServices();

                // 创建取消令牌
                _searchCancellationTokenSource?.Dispose();
                _searchCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _searchCancellationTokenSource.Token;

                // 并行执行搜索操作
                var searchTasks = new List<Task>();

                // 根据选择的选项执行搜索
                if (chkSearchParameters.Checked && _searchService != null)
                {
                    searchTasks.Add(Task.Run(() =>
                    {
                        // 定期检查取消请求
                        cancellationToken.ThrowIfCancellationRequested();
                        var parameterItems = _searchService.SearchLocalizationItems();
                        
                        cancellationToken.ThrowIfCancellationRequested();
                        lock (_foundItems)
                        {
                            _foundItems.AddRange(parameterItems);
                        }
                    }, cancellationToken));
                }

                if (chkSearchHardcoded.Checked && _hardcodedSearchService != null)
                {
                    searchTasks.Add(Task.Run(() =>
                    {
                        // 定期检查取消请求
                        cancellationToken.ThrowIfCancellationRequested();
                        var hardcodedItems = _hardcodedSearchService.SearchHardcodedItems(txtLocalizationDir.Text);
                        
                        cancellationToken.ThrowIfCancellationRequested();
                        lock (_foundItems)
                        {
                            _hardcodedItems.Clear();
                            _hardcodedItems.AddRange(hardcodedItems);
                            _foundItems.AddRange(hardcodedItems);
                        }
                    }, cancellationToken));
                }

                // 等待所有搜索任务完成或取消
                if (searchTasks.Count > 0)
                {
                    try
                    {
                        // 显示进度
                        var progressTask = Task.Run(async () =>
                        {
                            while (!cancellationToken.IsCancellationRequested && 
                                   !Task.WhenAll(searchTasks).IsCompleted)
                            {
                                await Task.Delay(500, cancellationToken);
                                Invoke(new Action(() => 
                                {
                                    if (progressBar.Value < 90)
                                        progressBar.Value += 5;
                                }));
                            }
                        }, cancellationToken);

                        await Task.WhenAll(searchTasks);
                        progressBar.Value = 95;
                    }
                    catch (OperationCanceledException)
                    {
                        Invoke(new Action(() => 
                        {
                            toolStripStatusLabel.Text = "搜索已取消";
                            _foundItems.Clear();
                            UpdateResultsGrid();
                        }));
                        return;
                    }
                }

                // 生成或更新本地化文件
                if (_localizationGenerator != null && _foundItems.Count > 0 && 
                    !cancellationToken.IsCancellationRequested)
                {
                    await Task.Run(() => 
                        _localizationGenerator.GenerateOrUpdateLocalizationFiles(_foundItems),
                        cancellationToken);
                }

                // 显示结果
                if (!cancellationToken.IsCancellationRequested)
                {
                    Invoke(new Action(() =>
                    {
                        UpdateResultsGrid();
                        toolStripStatusLabel.Text = $"搜索完成，找到 {_foundItems.Count} 项";
                        btnReplaceAll.Enabled = _foundItems.Count > 0;
                        progressBar.Value = 100;
                    }));
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"搜索过程出错: {ex.Message}");
                Invoke(new Action(() =>
                {
                    toolStripStatusLabel.Text = "搜索出错";
                    MessageBox.Show($"搜索出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
            finally
            {
                // 无论成功失败都重新启用搜索按钮，禁用取消按钮
                Invoke(new Action(() =>
                {
                    btnStartSearch.Enabled = true;
                    btnCancelSearch.Enabled = false;
                    tabControl1.SelectedTab = tabPageResults;
                }));
                
                // 释放取消令牌
                _searchCancellationTokenSource?.Dispose();
                _searchCancellationTokenSource = null;
            }
        }

        private void StartParameterSearch()
        {
            if (_searchService == null)
            {
                MessageBox.Show("请先设置MOD目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            toolStripStatusLabel.Text = "正在搜索参数文本...";
            btnStartSearch.Enabled = false;
            btnCancelSearch.Enabled = true;
            progressBar.Value = 0;

            // 创建取消令牌
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _searchCancellationTokenSource.Token;

            Task.Run(() =>
            {
                try
                {
                    var items = _searchService.SearchLocalizationItems();
                    cancellationToken.ThrowIfCancellationRequested();

                    Invoke(new Action(() =>
                    {
                        _foundItems.Clear();
                        _foundItems.AddRange(items);
                        UpdateResultsGrid();
                        toolStripStatusLabel.Text = $"参数文本搜索完成，找到 {items.Count} 项";
                        btnStartSearch.Enabled = true;
                        btnCancelSearch.Enabled = false;
                        btnReplaceAll.Enabled = _foundItems.Count > 0;
                        tabControl1.SelectedTab = tabPageResults;
                        progressBar.Value = 100;
                    }));
                }
                catch (OperationCanceledException)
                {
                    Invoke(new Action(() =>
                    {
                        toolStripStatusLabel.Text = "搜索已取消";
                        btnStartSearch.Enabled = true;
                        btnCancelSearch.Enabled = false;
                    }));
                }
                catch (Exception ex)
                {
                    LogManager.Log($"参数搜索出错: {ex.Message}");
                    Invoke(new Action(() =>
                    {
                        toolStripStatusLabel.Text = "参数搜索出错";
                        btnStartSearch.Enabled = true;
                        btnCancelSearch.Enabled = false;
                        MessageBox.Show($"参数搜索出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                finally
                {
                    _searchCancellationTokenSource?.Dispose();
                    _searchCancellationTokenSource = null;
                }
            }, cancellationToken);
        }

        private void StartHardcodedSearch()
        {
            if (_hardcodedSearchService == null)
            {
                MessageBox.Show("请先设置MOD目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            toolStripStatusLabel.Text = "正在搜索硬编码文本...";
            btnStartSearch.Enabled = false;
            btnCancelSearch.Enabled = true;
            progressBar.Value = 0;

            // 创建取消令牌
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _searchCancellationTokenSource.Token;

            Task.Run(() =>
            {
                try
                {
                    var items = _hardcodedSearchService.SearchHardcodedItems(txtLocalizationDir.Text);
                    cancellationToken.ThrowIfCancellationRequested();

                    Invoke(new Action(() =>
                    {
                        _hardcodedItems.Clear();
                        _hardcodedItems.AddRange(items);
                        _foundItems.Clear();
                        _foundItems.AddRange(items);

                        if (_localizationGenerator != null && items.Count > 0)
                        {
                            _localizationGenerator.GenerateOrUpdateLocalizationFiles(items);
                        }

                        UpdateResultsGrid();
                        toolStripStatusLabel.Text = $"硬编码文本搜索完成，找到 {items.Count} 项";
                        btnStartSearch.Enabled = true;
                        btnCancelSearch.Enabled = false;
                        btnReplaceAll.Enabled = _foundItems.Count > 0;
                        tabControl1.SelectedTab = tabPageResults;
                        progressBar.Value = 100;
                    }));
                }
                catch (OperationCanceledException)
                {
                    Invoke(new Action(() =>
                    {
                        toolStripStatusLabel.Text = "搜索已取消";
                        btnStartSearch.Enabled = true;
                        btnCancelSearch.Enabled = false;
                    }));
                }
                catch (Exception ex)
                {
                    LogManager.Log($"硬编码搜索出错: {ex.Message}");
                    Invoke(new Action(() =>
                    {
                        toolStripStatusLabel.Text = "硬编码搜索出错";
                        btnStartSearch.Enabled = true;
                        btnCancelSearch.Enabled = false;
                        MessageBox.Show($"硬编码搜索出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                finally
                {
                    _searchCancellationTokenSource?.Dispose();
                    _searchCancellationTokenSource = null;
                }
            }, cancellationToken);
        }

        private void UpdateResultsGrid()
        {
            if (dgvResults.InvokeRequired)
            {
                // 使用BeginInvoke异步更新，避免阻塞调用线程
                dgvResults.BeginInvoke(new Action(UpdateResultsGrid));
                return;
            }

            // 临时禁用刷新以提高性能
            dgvResults.SuspendLayout();
            dgvResults.DataSource = null;
            dgvResults.DataSource = _foundItems.ToList(); // 避免直接绑定原列表
            dgvResults.ResumeLayout(false);
            dgvResults.Refresh();
        }

        // 更新日志显示
        private void UpdateLogDisplay()
        {
            if (txtLog.InvokeRequired)
            {
                // 使用BeginInvoke异步更新UI，避免阻塞
                txtLog.BeginInvoke(new Action(UpdateLogDisplay));
                return;
            }

            // 获取最新日志内容
            string logContent = LogManager.GetLogContent();
            
            // 仅当内容发生变化时才更新，减少不必要的UI操作
            if (txtLog.Text != logContent)
            {
                txtLog.Text = logContent;
                // 滚动到最后一行
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
        }

        private void DgvResults_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _foundItems.Count)
            {
                var item = _foundItems[e.RowIndex];
                if (e.ColumnIndex == dgvResults.Columns["LocalizedText"].Index)
                {
                    item.LocalizedText = dgvResults.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
                }
            }
        }

        private void BtnReplaceAll_Click(object? sender, EventArgs e)
        {
            if (_replacementService == null)
            {
                MessageBox.Show("替换服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_foundItems.Count == 0)
            {
                MessageBox.Show("没有可替换的项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要替换所有 {_foundItems.Count} 项吗？\n此操作将修改源文件，建议先备份。",
                "确认替换",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                btnReplaceAll.Enabled = false;
                btnStartSearch.Enabled = false;
                btnCancelSearch.Enabled = false;
                toolStripStatusLabel.Text = "正在替换...";
                progressBar.Value = 0;

                // 在后台执行替换操作
                Task.Run(() => _replacementService.ReplaceAll(_foundItems));
            }
        }

        private void ProgressUpdated(int progress)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action<int>(ProgressUpdated), progress);
            }
            else
            {
                progressBar.Value = progress;
                toolStripStatusLabel.Text = $"正在替换... {progress}%";
            }
        }

        private void ReplacementCompleted(bool success, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool, string>(ReplacementCompleted), success, message);
            }
            else
            {
                progressBar.Value = success ? 100 : 0;
                toolStripStatusLabel.Text = success ? "替换完成" : "替换失败";
                btnReplaceAll.Enabled = true;
                btnStartSearch.Enabled = true;
                btnCancelSearch.Enabled = false;

                MessageBox.Show(message, success ? "成功" : "错误", MessageBoxButtons.OK,
                    success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
        }

        private void BtnRefreshBackups_Click(object? sender, EventArgs e)
        {
            RefreshBackupList();
        }

        private void RefreshBackupList()
        {
            try
            {
                if (_backupManager == null) return;

                cboBackups.Items.Clear();
                var backups = _backupManager.GetBackupDirectories();

                if (backups.Count > 0)
                {
                    cboBackups.Items.AddRange(backups.ToArray());
                    cboBackups.SelectedIndex = 0;
                    btnRestoreBackup.Enabled = true;
                }
                else
                {
                    cboBackups.Items.Add("无备份");
                    cboBackups.SelectedIndex = 0;
                    btnRestoreBackup.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"刷新备份列表失败: {ex.Message}");
            }
        }

        private void BtnRestoreBackup_Click(object? sender, EventArgs e)
        {
            if (_backupManager == null || cboBackups.SelectedItem == null ||
                cboBackups.SelectedItem.ToString() == "无备份")
            {
                return;
            }

            string backupDir = cboBackups.SelectedItem.ToString() ?? "";
            if (string.IsNullOrEmpty(backupDir) || !Directory.Exists(backupDir))
            {
                MessageBox.Show("所选备份不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                $"确定要恢复备份 {Path.GetFileName(backupDir)} 吗？\n这将覆盖当前的MOD文件。",
                "确认恢复",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    toolStripStatusLabel.Text = "正在恢复备份...";
                    btnRestoreBackup.Enabled = false;
                    btnStartSearch.Enabled = false;

                    // 调用不返回值的RestoreBackup方法
                    _backupManager.RestoreBackup(backupDir);
                    bool success = true; // 可以根据需要设置为true，或者添加其他方式来判断备份是否成功

                    if (success)
                    {
                        toolStripStatusLabel.Text = "备份恢复成功";
                        MessageBox.Show("备份恢复成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        toolStripStatusLabel.Text = "备份恢复失败";
                        MessageBox.Show("备份恢复失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log($"恢复备份失败: {ex.Message}");
                    toolStripStatusLabel.Text = "恢复备份失败";
                    MessageBox.Show($"恢复备份失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnRestoreBackup.Enabled = true;
                    btnStartSearch.Enabled = true;
                }
            }
        }
    }
}