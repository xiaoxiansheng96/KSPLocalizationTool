using KSPLocalizationTool.Models;
using KSPLocalizationTool.Services;
// 当前报错表明缺少对 Microsoft.Extensions.DependencyInjection 的程序集引用，若确定不需要此引用可移除该行
// 若需要使用此命名空间，需在项目中添加对 Microsoft.Extensions.DependencyInjection 的 NuGet 包引用
// 此处先移除该引用行以消除编译错误
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KSPLocalizationTool
{
    public partial class Form1 : Form
    {
        // 服务依赖
        private readonly FileSearchService _searchService;
// 已根据提示删除未使用的私有成员 _backupService
        private readonly LogService _logService;
        private readonly LocalizationKeyGenerator _keyGenerator;
        private readonly ReplacementService _replacementService;
        private readonly ConfigService _configService;
        private readonly RestoreModule _restoreModule;

        // 应用程序配置
        private readonly AppConfig _appConfig = new();

        // 搜索状态
        private bool _isSearching;
        private CancellationTokenSource? _searchCts;

        // 本地化数据
        private readonly BindingList<LocalizationItem> _localizationItems = new();
        private List<SearchResultItem>? _searchResults;

        // 控件引用
        private TextBox? _modDirTextBox;
        private TextBox? _backupDirTextBox;
        private TextBox? _locDirTextBox;
        private RichTextBox? _logTextBox;
        private ComboBox? _restoreComboBox;
        private DataGridView? _translationGridView;
        private ProgressBar? _progressBar;
        private TextBox? _cfgFilterTextBox;
        private TextBox? _csFilterTextBox;

        // 使用依赖注入的构造函数
        public Form1(
            AppConfig appConfig,
            FileSearchService searchService,
            LogService logService,
            ConfigService configService,
            LocalizationKeyGenerator keyGenerator,
            ReplacementService replacementService,
            RestoreModule restoreModule
        )
        {
            InitializeComponent();
            _appConfig = appConfig;
            _searchService = searchService;
            _logService = logService;
            _configService = configService;
            _keyGenerator = keyGenerator;
            _replacementService = replacementService;
            _restoreModule = restoreModule;

            // 订阅服务事件
            _logService.LogUpdated += LogService_LogUpdated;
            _keyGenerator.KeysGenerated += KeyGenerator_KeysGenerated;
            _searchService.ProgressUpdated += SearchService_ProgressUpdated;

            InitializeUI();
            LoadConfig();
            AddEventHandlers();
        }

        // 初始化事件处理程序
        private void InitializeEventHandlers()
        {
            // 移除重复的日志事件订阅
            // _logService.LogUpdated += LogService_LogUpdated;
            _keyGenerator.KeysGenerated += KeyGenerator_KeysGenerated;
            _searchService.ProgressUpdated += SearchService_ProgressUpdated;

            // 为目录文本框添加文本变更事件
            if (_modDirTextBox != null)
                _modDirTextBox.TextChanged += (s, e) => SaveConfig();
            if (_backupDirTextBox != null)
                _backupDirTextBox.TextChanged += (s, e) => SaveConfig();
            if (_locDirTextBox != null)
                _locDirTextBox.TextChanged += (s, e) => SaveConfig();
        }

        private void SearchService_ProgressUpdated(int progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SearchService_ProgressUpdated), progress);
                return;
            }

            if (_progressBar != null && !_progressBar.IsDisposed)
            {
                // 确保进度值在0-100之间
                int clampedProgress = Math.Min(100, Math.Max(0, progress));
                _progressBar.Value = clampedProgress;
                toolStripStatusLabel1.Text = $"搜索进度: {clampedProgress}%";
            }
        }

        // 在Form1.cs中添加
        private void KeyGenerator_KeysGenerated(List<LocalizationKeyItem> generatedKeys)
        {
            // 确保在UI线程操作（避免跨线程异常）
            if (_translationGridView?.InvokeRequired == true)
            {
                _translationGridView.Invoke(new Action<List<LocalizationKeyItem>>(KeyGenerator_KeysGenerated), generatedKeys);
                return;
            }

            // 清空现有数据（可选，根据需求决定是否保留历史数据）
            _localizationItems.Clear();

            // 转换并添加生成的键值到数据源
            foreach (var keyItem in generatedKeys)
            {
                _localizationItems.Add(new LocalizationItem
                {
                    FilePath = keyItem.FilePath,
                    LineNumber = keyItem.LineNumber,
                    ParameterType = keyItem.ParameterType,
                    OriginalText = keyItem.OriginalText,
                    LocalizationKey = keyItem.GeneratedKey, // 关键：绑定生成的键值
                });
            }

            _logService?.LogMessage($"已加载 {generatedKeys.Count} 个本地化键值到界面");
        }
        private void LogService_LogUpdated(object? sender, LogEventArgs e)
        {
            // 确保在UI线程更新控件（避免跨线程异常）
            if (_logTextBox?.InvokeRequired == true)
            {
                _logTextBox.Invoke(new Action(() => LogService_LogUpdated(sender, e)));
                return;
            }

            // 追加日志到文本框，带时间戳
            _logTextBox?.AppendText($"[{DateTime.Now:HH:mm:ss}] {e.Message}{Environment.NewLine}");
            // 自动滚动到最新日志
            _logTextBox?.ScrollToCaret();
        }
        // 初始化UI设置
        private void InitializeUI()
        {
            // 设置窗体基本属性
            Text = "坎巴拉太空计划MOD本地化工具";
            MinimumSize = new System.Drawing.Size(800, 600); // 与Designer.cs中的ClientSize一致
            WindowState = FormWindowState.Normal; // 正常窗口状态

            // 初始化选项卡控件
            InitializeTabControl();

            // 初始化状态栏
            InitializeStatusStrip();

            // 为控件添加事件处理
            AddEventHandlers();
        }

        // 实现InitializeStatusStrip方法
        private void InitializeStatusStrip()
        {
            statusStrip1.SizingGrip = true;
            toolStripStatusLabel1.Text = "就绪";
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }

        // 初始化选项卡控件
        private void InitializeTabControl()
        {
            // 创建选项卡控件
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 创建三个选项卡页
            var tabSearch = new TabPage("文件搜索");
            var tabTranslation = new TabPage("本地化翻译");
            var tabFilter = new TabPage("参数筛选");

            // 初始化各个选项卡内容
            InitializeSearchTab(tabSearch);
            InitializeTranslationTab(tabTranslation);
            InitializeFilterTab(tabFilter);

            // 将选项卡页添加到选项卡控件
            tabControl.TabPages.Add(tabSearch);
            tabControl.TabPages.Add(tabTranslation);
            tabControl.TabPages.Add(tabFilter);

            // 添加选项卡切换事件
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            // 将选项卡控件添加到窗体
            Controls.Add(tabControl);
        }

        private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is TabControl tabControl && tabControl.SelectedIndex == 2)
            {
                // 当切换到参数筛选选项卡时加载当前筛选参数
                _cfgFilterTextBox!.Text = string.Join(Environment.NewLine, _appConfig.CfgFilters);
                _csFilterTextBox!.Text = string.Join(Environment.NewLine, _appConfig.CsFilters);
            }
        }

        // 初始化文件搜索选项卡
        private void InitializeSearchTab(TabPage tabPage)
        {
            // 创建主容器
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            mainPanel.SizeChanged += MainPanel_SizeChanged;

            // 创建路径输入区域
            var pathPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BorderStyle = BorderStyle.FixedSingle
            };

            // MOD目录输入
            var modDirLabel = new Label
            {
                Text = "MOD目录:",
                Location = new System.Drawing.Point(10, 10),
                Width = 60
            };

            _modDirTextBox = new TextBox
            {
                Name = "txtModDirectory",
                Location = new System.Drawing.Point(80, 10),
                Width = 400
            };

            var modDirBrowseButton = new Button
            {
                Text = "浏览...",
                Name = "btnBrowseMod",
                Location = new System.Drawing.Point(490, 10),
                Width = 75
            };

            // 备份目录输入
            var backupDirLabel = new Label
            {
                Text = "备份目录:",
                Location = new System.Drawing.Point(10, 40),
                Width = 60
            };

            _backupDirTextBox = new TextBox
            {
                Name = "txtBackupDirectory",
                Location = new System.Drawing.Point(80, 40),
                Width = 400
            };

            var backupDirBrowseButton = new Button
            {
                Text = "浏览...",
                Name = "btnBrowseBackup",
                Location = new System.Drawing.Point(490, 40),
                Width = 75
            };

            // 本地化文件目录输入
            var locDirLabel = new Label
            {
                Text = "本地化目录:",
                Location = new System.Drawing.Point(10, 70),
                Width = 60
            };

            _locDirTextBox = new TextBox
            {
                Name = "txtLocalizationDirectory",
                Location = new System.Drawing.Point(80, 70),
                Width = 400
            };

            var locDirBrowseButton = new Button
            {
                Text = "浏览...",
                Name = "btnBrowseLocalization",
                Location = new System.Drawing.Point(490, 70),
                Width = 75
            };

            // 将路径控件添加到路径面板
            pathPanel.Controls.Add(modDirLabel);
            pathPanel.Controls.Add(_modDirTextBox);
            pathPanel.Controls.Add(modDirBrowseButton);
            pathPanel.Controls.Add(backupDirLabel);
            pathPanel.Controls.Add(_backupDirTextBox);
            pathPanel.Controls.Add(backupDirBrowseButton);
            pathPanel.Controls.Add(locDirLabel);
            pathPanel.Controls.Add(_locDirTextBox);
            pathPanel.Controls.Add(locDirBrowseButton);

            // 创建按钮区域
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            var startSearchButton = new Button
            {
                Text = "开始搜索",
                Name = "btnStartSearch",
                Location = new System.Drawing.Point(10, 5),
                Width = 90
            };

            var stopSearchButton = new Button
            {
                Text = "停止搜索",
                Name = "btnStopSearch",
                Location = new System.Drawing.Point(110, 5),
                Width = 90,
                Enabled = false
            };

            var restoreLabel = new Label
            {
                Text = "选择还原节点:",
                Location = new System.Drawing.Point(210, 10),
                Width = 80
            };

            _restoreComboBox = new ComboBox
            {
                Name = "cmbRestorePoints",
                Location = new System.Drawing.Point(290, 7),
                Width = 150
            };

            var restoreButton = new Button
            {
                Text = "恢复备份",
                Name = "btnRestore",
                Location = new System.Drawing.Point(450, 5),
                Width = 90
            };

            var viewLogButton = new Button
            {
                Text = "查看日志",
                Name = "btnViewLog",
                Location = new System.Drawing.Point(550, 5),
                Width = 90
            };

            // 将按钮添加到按钮面板
            buttonPanel.Controls.Add(startSearchButton);
            buttonPanel.Controls.Add(stopSearchButton);
            buttonPanel.Controls.Add(restoreLabel);
            buttonPanel.Controls.Add(_restoreComboBox);
            buttonPanel.Controls.Add(restoreButton);
            buttonPanel.Controls.Add(viewLogButton);

            // 创建日志显示区域
            var logPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };

            _logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            logPanel.Controls.Add(_logTextBox);

            // 添加进度条
            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Visible = false
            };
            logPanel.Controls.Add(_progressBar);

            // 组装主面板
            mainPanel.Controls.Add(logPanel);
            mainPanel.Controls.Add(buttonPanel);
            mainPanel.Controls.Add(pathPanel);

            // 将主面板添加到选项卡
            tabPage.Controls.Add(mainPanel);
        }

        // 初始化翻译选项卡
        private void InitializeTranslationTab(TabPage tabPage)
        {
            // 创建主容器
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // 创建数据网格视图
            _translationGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // 添加列
            _translationGridView.Columns.AddRange(
        new DataGridViewTextBoxColumn { HeaderText = "文件路径", DataPropertyName = "FilePath", Width = 200 },
        new DataGridViewTextBoxColumn { HeaderText = "行号", DataPropertyName = "LineNumber", Width = 60 },
        new DataGridViewTextBoxColumn { HeaderText = "参数类型", DataPropertyName = "ParameterType", Width = 100 },
        new DataGridViewTextBoxColumn { HeaderText = "原始文本", DataPropertyName = "OriginalText", Width = 200 },
        new DataGridViewTextBoxColumn { HeaderText = "本地化键", DataPropertyName = "LocalizationKey", Width = 150 } // 新增此列
         
    );

            // 设置数据源
            _translationGridView.DataSource = _localizationItems;

            // 创建按钮面板
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            var generateKeysButton = new Button
            {
                Text = "生成键",
                Location = new System.Drawing.Point(10, 5),
                Width = 90
            };

           

            var replaceAllButton = new Button
            {
                Text = "全部替换",
                Location = new System.Drawing.Point(210, 5),
                Width = 90
            };

            var replaceSelectedButton = new Button
            {
                Text = "替换选中",
                Location = new System.Drawing.Point(310, 5),
                Width = 90
            };

            var saveLocFilesButton = new Button
            {
                Text = "保存本地化文件",
                Location = new System.Drawing.Point(410, 5),
                Width = 120
            };

            // 添加按钮事件
            generateKeysButton.Click += GenerateKeysButton_Click;
         
            replaceAllButton.Click += ReplaceAllButton_Click;
            replaceSelectedButton.Click += ReplaceSelectedButton_Click;
            saveLocFilesButton.Click += SaveLocFilesButton_Click;

            // 添加按钮到面板
            buttonPanel.Controls.Add(generateKeysButton);
             
            buttonPanel.Controls.Add(replaceAllButton);
            buttonPanel.Controls.Add(replaceSelectedButton);
            buttonPanel.Controls.Add(saveLocFilesButton);

            // 组装翻译选项卡
            mainPanel.Controls.Add(_translationGridView);
            mainPanel.Controls.Add(buttonPanel);

            tabPage.Controls.Add(mainPanel);
        }

        // 初始化筛选选项卡
        private void InitializeFilterTab(TabPage tabPage)
        {
            // 创建主容器
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // 创建分割面板
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };

            // CFG筛选器
            var cfgPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            var cfgLabel = new Label
            {
                Text = "CFG文件筛选参数:",
                Dock = DockStyle.Top,
                Height = 20
            };

            _cfgFilterTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            cfgPanel.Controls.Add(_cfgFilterTextBox);
            cfgPanel.Controls.Add(cfgLabel);

            // CS筛选器
            var csPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            var csLabel = new Label
            {
                Text = "CS文件筛选参数:",
                Dock = DockStyle.Top,
                Height = 20
            };

            _csFilterTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            csPanel.Controls.Add(_csFilterTextBox);
            csPanel.Controls.Add(csLabel);

            // 底部按钮面板
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            var saveFiltersButton = new Button
            {
                Text = "保存筛选参数",
                Location = new System.Drawing.Point(10, 5),
                Width = 120
            };

            saveFiltersButton.Click += SaveFiltersButton_Click;

            buttonPanel.Controls.Add(saveFiltersButton);

            // 组装筛选选项卡
            splitContainer.Panel1.Controls.Add(cfgPanel);
            splitContainer.Panel2.Controls.Add(csPanel);
            mainPanel.Controls.Add(splitContainer);
            mainPanel.Controls.Add(buttonPanel);

            tabPage.Controls.Add(mainPanel);
        }

        // 添加事件处理程序
        private void AddEventHandlers()
        {
            // 浏览按钮事件
            var modDirBrowseButton = Controls.Find("btnBrowseMod", true).FirstOrDefault() is Button button ? button : null;
            var backupDirBrowseButton = Controls.Find("btnBrowseBackup", true).FirstOrDefault() is Button backupDirBtn ? backupDirBtn : null;
            var locDirBrowseButton = Controls.Find("btnBrowseLocalization", true).FirstOrDefault() is Button locDirBtn ? locDirBtn : null;

            // 搜索按钮事件
            var startSearchButton = Controls.Find("btnStartSearch", true).FirstOrDefault() is Button startSearchBtn ? startSearchBtn : null;
            var stopSearchButton = Controls.Find("btnStopSearch", true).FirstOrDefault() is Button stopSearchBtn ? stopSearchBtn : null;
            var restoreButton = Controls.Find("btnRestore", true).FirstOrDefault() is Button restoreBtn ? restoreBtn : null;
            var viewLogButton = Controls.Find("btnViewLog", true).FirstOrDefault() is Button ? Controls.Find("btnViewLog", true).FirstOrDefault() as Button : null;

            if (modDirBrowseButton != null)
                modDirBrowseButton.Click += ModDirBrowseButton_Click;

            if (backupDirBrowseButton != null)
                backupDirBrowseButton.Click += BackupDirBrowseButton_Click;

            if (locDirBrowseButton != null)
                locDirBrowseButton.Click += LocDirBrowseButton_Click;

            if (startSearchButton != null)
                startSearchButton.Click += StartSearchButton_Click;

            if (stopSearchButton != null)
                stopSearchButton.Click += StopSearchButton_Click;

            if (restoreButton != null)
                restoreButton.Click += RestoreButton_Click;

            if (viewLogButton != null)
                viewLogButton.Click += ViewLogButton_Click;
        }

        private void ModDirBrowseButton_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();
            {
                // 如果有上次保存的路径，使用它作为初始路径
                if (_backupDirTextBox != null && string.IsNullOrEmpty(_backupDirTextBox.Text))
{
    string defaultBackupDir = Path.Combine(folderDialog?.SelectedPath ?? string.Empty, "Backup");
    _backupDirTextBox.Text = defaultBackupDir;
    EnsureDirectoryExists(defaultBackupDir);
}

                if (folderDialog != null && folderDialog.ShowDialog() == DialogResult.OK)
                {
if (_modDirTextBox != null)
{
    _modDirTextBox.Text = folderDialog.SelectedPath;
}

                    // 自动设置备份目录为MOD目录下的Backup文件夹
                    if (string.IsNullOrEmpty(_locDirTextBox?.Text))
{
    string defaultLocDir = Path.Combine(folderDialog?.SelectedPath ?? string.Empty, "Localization");
if (_locDirTextBox != null)
{
    _locDirTextBox.Text = defaultLocDir;
}
    EnsureDirectoryExists(defaultLocDir);
}

                    // 自动设置本地化目录为MOD目录下的Localization文件夹
                    if (_locDirTextBox != null && string.IsNullOrEmpty(_locDirTextBox.Text))
                    {
                        string defaultLocDir = !string.IsNullOrEmpty(folderDialog?.SelectedPath) ? Path.Combine(folderDialog.SelectedPath, "Localization") : string.Empty;
                        _locDirTextBox.Text = defaultLocDir;
                        EnsureDirectoryExists(defaultLocDir);
                    }
                }
            }
        }
        private void EnsureDirectoryExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    _logService?.LogMessage($"已创建目录: {path}");
                }
                catch (Exception ex)
                {
                    _logService?.LogMessage($"创建目录失败 {path}: {ex.Message}");
                    MessageBox.Show($"创建目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void BackupDirBrowseButton_Click(object? sender, EventArgs e)
        {
            var folderDialog = new FolderBrowserDialog();
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    if (_backupDirTextBox != null)
                    {
                        _backupDirTextBox.Text = folderDialog.SelectedPath;
                    }
                }
            }
        }

        private void LocDirBrowseButton_Click(object? sender, EventArgs e)
        {
            var folderDialog = new FolderBrowserDialog();
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    if (_locDirTextBox != null)
                    {
                        _locDirTextBox.Text = folderDialog.SelectedPath;
                    }
                }
            }
        }

        private void StartSearchButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_modDirTextBox?.Text) || !Directory.Exists(_modDirTextBox.Text))
            {
                MessageBox.Show("请选择有效的MOD目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 确保备份目录存在
            if (_backupDirTextBox != null && string.IsNullOrEmpty(_backupDirTextBox.Text))
            {
                // 如果未设置备份目录，自动创建
                string defaultBackupDir = Path.Combine(_modDirTextBox.Text, "Backup");
                _backupDirTextBox.Text = defaultBackupDir;
            }
            if (_backupDirTextBox != null)
            {
                EnsureDirectoryExists(_backupDirTextBox.Text);
            }

            // 确保本地化目录存在
            if (_locDirTextBox is null || string.IsNullOrEmpty(_locDirTextBox.Text))
            {
                // 如果未设置本地化目录，自动创建
                string defaultLocDir = Path.Combine(_modDirTextBox.Text, "Localization");
if (_locDirTextBox != null)
{
    _locDirTextBox.Text = defaultLocDir;
}
            }
            if (_locDirTextBox != null)
            {
                EnsureDirectoryExists(_locDirTextBox.Text);
            }

            _isSearching = true;
            _searchCts = new CancellationTokenSource();
            if (_progressBar != null && !_progressBar.IsDisposed)
            {
                _progressBar.Visible = true;
            }

            // 防止重复记录日志，添加检查
            if (!_isSearching)
            {
                _logService?.LogMessage("开始搜索文件...");
            }

            // 启动搜索任务
            Task.Run(() => SearchFilesAsync(), _searchCts.Token);
        }

        private void StopSearchButton_Click(object? sender, EventArgs e)
        {
            if (_isSearching && _searchCts != null)
            {
                _searchCts.Cancel();
                _isSearching = false;
                _logService?.LogMessage("正在停止搜索...");
            }
        }

        private async void RestoreButton_Click(object? sender, EventArgs e)
        {
            if (_restoreComboBox?.SelectedItem == null)
            {
                MessageBox.Show("请选择一个恢复点", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_restoreModule == null)
            {
                _logService?.LogMessage("恢复服务未初始化");
                return;
            }

            // 获取选中的恢复点
            string selectedRestorePoint = _restoreComboBox?.SelectedItem?.ToString() ?? string.Empty;

            // 配置恢复服务参数
            _restoreModule.BackupRootDirectory = _backupDirTextBox?.Text;
            _restoreModule.ModRootDirectory = _modDirTextBox?.Text;

            // 订阅恢复服务的事件（更新日志和进度）
            _restoreModule.StatusChanged += msg => _logService?.LogMessage(msg);
            _restoreModule.ProgressUpdated += progress =>
            {
                if (_progressBar != null && !_progressBar.IsDisposed)
                {
                    _progressBar.Value = progress;
                    toolStripStatusLabel1.Text = $"恢复进度: {progress}%";
                }
            };
            _restoreModule.RestoreCompleted += (success, msg) =>
            {
                _logService?.LogMessage(msg);
                MessageBox.Show(msg, "恢复结果", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            };

            // 调用恢复服务（主程序只调用，不处理具体恢复逻辑）
            await _restoreModule.RestoreAsync(selectedRestorePoint);
        }

        private void ViewLogButton_Click(object? sender, EventArgs e)
        {
            if (_logTextBox != null)
            {
                // 显示日志面板
                _logTextBox.Visible = true;
                _logTextBox.BringToFront();
            }
            // 新增：打开日志保存的目录
            if (_logService != null && Directory.Exists(_logService.LogDirectory))
            {
                try
                {
                    // 启动资源管理器并定位到日志目录
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = _logService.LogDirectory,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception ex)
                {
                    _logService.LogMessage($"打开日志目录失败: {ex.Message}");
                    MessageBox.Show($"无法打开日志目录: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (_logService != null)
            {
                _logService.LogMessage($"日志目录不存在: {_logService.LogDirectory}");
                MessageBox.Show($"日志目录不存在: {_logService.LogDirectory}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        // 加载配置
       // 替换原LoadConfig方法
        private void LoadConfig()
{
    var config = _configService?.LoadConfig() ?? new AppConfig();
if (_modDirTextBox != null)
    _modDirTextBox.Text = config.ModDirectory;
if (_backupDirTextBox != null)
    _backupDirTextBox.Text = config.BackupDirectory;
if (_locDirTextBox != null)
    _locDirTextBox.Text = config.LocalizationDirectory;

    // 加载筛选参数
var (cfgFilters, csFilters) = _configService?.GetFilters() ?? (Array.Empty<string>(), Array.Empty<string>());
    if (_cfgFilterTextBox != null)
        _cfgFilterTextBox.Text = string.Join(Environment.NewLine, cfgFilters);
    if (_csFilterTextBox != null)
        _csFilterTextBox.Text = string.Join(Environment.NewLine, csFilters);
}
        // 添加SaveConfig方法
       // 替换原SaveConfig方法
        private void SaveConfig()
{
    _configService?.SaveConfig(
        _modDirTextBox?.Text ?? string.Empty,
        _backupDirTextBox?.Text ?? string.Empty,
        _locDirTextBox?.Text ?? string.Empty
    );
}

        // 保存筛选参数
        // 替换原SaveFiltersButton_Click方法
        private void SaveFiltersButton_Click(object? sender, EventArgs e)
        {
            var cfgFilters = _cfgFilterTextBox?.Text.Split(
                new[] { Environment.NewLine }, 
                StringSplitOptions.RemoveEmptyEntries
            ) ?? [];

            var csFilters = _csFilterTextBox?.Text.Split(
                new[] { Environment.NewLine }, 
                StringSplitOptions.RemoveEmptyEntries
            ) ?? [];

            _configService?.SaveFilters(cfgFilters, csFilters);
            _logService?.LogMessage("筛选参数已保存");
        }

        // 生成键按钮点击事件
        // 在Form1.cs中完善GenerateKeysButton_Click方法
        private void GenerateKeysButton_Click(object? sender, EventArgs e)
        {
            if (_keyGenerator == null || _searchService == null)
            {
                _logService?.LogMessage("服务未初始化");
                return;
            }

            // 检查是否有搜索结果（键值生成依赖于搜索结果）
// 移除不需要的 results 赋值操作
            if (_searchResults == null || _searchResults.Count == 0)
            {
                _logService?.LogMessage("请先执行搜索，获取待处理的文本");
                return;
            }

            // 将搜索结果传递给键生成器
            _keyGenerator.SearchResults = _searchResults;

            // 触发键值生成（调用生成器的生成方法）
            _keyGenerator.GenerateKeys(sender, e);
        }



        // 保存本地化文件按钮点击事件
        private void SaveLocFilesButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_locDirTextBox?.Text))
            {
                _logService?.LogMessage("请先设置本地化目录");
                return;
            }

            try
            {
                string enUsPath = Path.Combine(_locDirTextBox.Text, "en-us.cfg");
                string zhCnPath = Path.Combine(_locDirTextBox.Text, "zh-cn.cfg");

                // 生成英文本地化文件（原始文本）
                LocalizationService.GenerateLocalizationFile(enUsPath, _localizationItems,
                    item => item.OriginalText);

                
                // 生成中文本地化文件（使用原始文本）
                LocalizationService.GenerateLocalizationFile(zhCnPath, _localizationItems,
                    item => item.OriginalText);

                _logService!.LogMessage($"已生成本地化文件: {enUsPath} 和 {zhCnPath}");
            }
            catch (Exception ex)
            {
                _logService!.LogMessage($"生成本地化文件失败: {ex.Message}");
            }
        }

        private void ReplaceAllButton_Click(object? sender, EventArgs e)
        {
            ReplaceItems([.. _localizationItems]);
        }

        private void ReplaceSelectedButton_Click(object? sender, EventArgs e)
        {
            if (_translationGridView!.SelectedRows.Count == 0)
            {
                _logService!.LogMessage("请先选择要替换的行");
                return;
            }

            var selectedItems = new List<LocalizationItem>();
            foreach (DataGridViewRow row in _translationGridView.SelectedRows)
            {
                if (row.DataBoundItem is LocalizationItem item)
                {
                    selectedItems.Add(item);
                }
            }

            ReplaceItems(selectedItems);
        }

        // 原来的ReplaceItems方法可以删除，替换为：
        private void ReplaceItems(List<LocalizationItem> items)
        {
            if (items == null || items.Count == 0)
            {
                _logService?.LogMessage("没有可替换的项");
                return;
            }

            // 直接调用功能块的方法，主程序不再包含实施逻辑
            if (_replacementService is null)
            {
                _logService?.LogMessage("替换服务未初始化，无法执行替换操作");
                return;
            }
            var (successCount, failCount) = _replacementService.ReplaceItems(items);

            // 显示结果
            _logService?.LogMessage($"替换完成 - 成功: {successCount}, 失败: {failCount}");
            MessageBox.Show($"替换完成\n成功: {successCount}\n失败: {failCount}", "替换结果",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MainPanel_SizeChanged(object? sender, EventArgs e)
        {
            // 处理面板大小变化
        }

        // 添加搜索文件的异步方法
        private async Task SearchFilesAsync()
        {
            if (string.IsNullOrEmpty(_modDirTextBox?.Text))
                return;

            try
            {
                // 直接调用服务，无需在主程序处理细节
                var results = await Task.Run(() =>
                    _searchService.SearchFiles(
                        _modDirTextBox.Text,
                        _configService.GetFilters().CfgFilters,
                        _configService.GetFilters().CsFilters,
                        _locDirTextBox?.Text ?? string.Empty,
                        _searchCts?.Token ?? CancellationToken.None
                    )
                );

                _searchResults = results;
                _logService.LogMessage($"搜索完成，找到 {results.Count} 项");
            }
            catch (Exception ex)
            {
                _logService?.LogMessage($"搜索失败: {ex.Message}");
            }
        }
    }
}