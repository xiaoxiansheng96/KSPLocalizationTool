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
        private readonly List<LocalizationItem> _foundItems = new();
        private string _targetLanguageCode = "zh-cn";

        private SearchService? _searchService;
        private LocalizationFileHandler? _localizationHandler;
        private ReplacementService? _replacementService;
        private BackupManager? _backupManager;

        private IContainer? components;
        private TabControl tabControl1 = new TabControl();
        private TabPage tabPageSettings = new TabPage();
        private TabPage tabPageResults = new TabPage();
        private Button btnSelectModDir = new Button();
        private TextBox txtModDir = new TextBox();
        private Label label1 = new Label();
        private Button btnSelectLocalizationDir = new Button();
        private TextBox txtLocalizationDir = new TextBox();
        private Label label3 = new Label();
        private Button btnSelectBackupDir = new Button();
        private TextBox txtBackupDir = new TextBox();
        private Label label2 = new Label();
        private ComboBox cboTargetLanguage = new ComboBox();
        private Label label4 = new Label();
        private Button btnStartSearch = new Button();
        private DataGridView dgvResults = new DataGridView();
        private Button btnReplaceAll = new Button();
        private ProgressBar progressBar = new ProgressBar();
        private TabPage tabPageLogs = new TabPage();
        private TextBox txtLog = new TextBox();
        private StatusStrip statusStrip1 = new StatusStrip();
        private ToolStripStatusLabel toolStripStatusLabel = new ToolStripStatusLabel();

        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            LoadSettings();
            SetupDataGridView();
            SetupLanguageComboBox();
            AttachEventHandlers();

            UpdateLogDisplay();
            LogManager.Log("应用程序已启动");
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
            tabControl1.TabPages.AddRange([tabPageSettings, tabPageResults, tabPageLogs]);

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

            // 配置MOD目录相关控件
            label1.AutoSize = true;
            label1.Location = new Point(10, 20);
            label1.Name = "label1";
            label1.Size = new Size(53, 15);
            label1.TabIndex = 0;
            label1.Text = "MOD目录:";

            txtModDir.Location = new Point(80, 20);
            txtModDir.Name = "txtModDir";
            txtModDir.Size = new Size(500, 23);
            txtModDir.TabIndex = 1;

            btnSelectModDir.Location = new Point(590, 20);
            btnSelectModDir.Name = "btnSelectModDir";
            btnSelectModDir.Size = new Size(75, 23);
            btnSelectModDir.TabIndex = 2;
            btnSelectModDir.Text = "浏览...";
            btnSelectModDir.UseVisualStyleBackColor = true;

            // 配置本地化目录相关控件
            label3.AutoSize = true;
            label3.Location = new Point(10, 60);
            label3.Name = "label3";
            label3.Size = new Size(89, 15);
            label3.TabIndex = 3;
            label3.Text = "本地化文件目录:";

            txtLocalizationDir.Location = new Point(100, 60);
            txtLocalizationDir.Name = "txtLocalizationDir";
            txtLocalizationDir.Size = new Size(480, 23);
            txtLocalizationDir.TabIndex = 4;

            btnSelectLocalizationDir.Location = new Point(590, 60);
            btnSelectLocalizationDir.Name = "btnSelectLocalizationDir";
            btnSelectLocalizationDir.Size = new Size(75, 23);
            btnSelectLocalizationDir.TabIndex = 5;
            btnSelectLocalizationDir.Text = "浏览...";
            btnSelectLocalizationDir.UseVisualStyleBackColor = true;

            // 配置备份目录相关控件
            label2.AutoSize = true;
            label2.Location = new Point(10, 100);
            label2.Name = "label2";
            label2.Size = new Size(65, 15);
            label2.TabIndex = 6;
            label2.Text = "备份目录:";

            txtBackupDir.Location = new Point(80, 100);
            txtBackupDir.Name = "txtBackupDir";
            txtBackupDir.Size = new Size(500, 23);
            txtBackupDir.TabIndex = 7;

            btnSelectBackupDir.Location = new Point(590, 100);
            btnSelectBackupDir.Name = "btnSelectBackupDir";
            btnSelectBackupDir.Size = new Size(75, 23);
            btnSelectBackupDir.TabIndex = 8;
            btnSelectBackupDir.Text = "浏览...";
            btnSelectBackupDir.UseVisualStyleBackColor = true;

            // 配置目标语言相关控件
            label4.AutoSize = true;
            label4.Location = new Point(10, 140);
            label4.Name = "label4";
            label4.Size = new Size(65, 15);
            label4.TabIndex = 9;
            label4.Text = "目标语言:";

            cboTargetLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTargetLanguage.Location = new Point(80, 140);
            cboTargetLanguage.Name = "cboTargetLanguage";
            cboTargetLanguage.Size = new Size(200, 23);
            cboTargetLanguage.TabIndex = 10;

            // 配置搜索按钮
            btnStartSearch.Location = new Point(80, 180);
            btnStartSearch.Name = "btnStartSearch";
            btnStartSearch.Size = new Size(100, 30);
            btnStartSearch.TabIndex = 11;
            btnStartSearch.Text = "开始搜索";
            btnStartSearch.UseVisualStyleBackColor = true;

            // 配置tabPageResults
            tabPageResults.Controls.Add(dgvResults);
            tabPageResults.Controls.Add(btnReplaceAll);
            tabPageResults.Controls.Add(progressBar);
            tabPageResults.Location = new Point(4, 24);
            tabPageResults.Name = "tabPageResults";
            tabPageResults.Padding = new Padding(3);
            tabPageResults.Size = new Size(792, 550);
            tabPageResults.TabIndex = 1;
            tabPageResults.Text = "搜索结果";
            tabPageResults.UseVisualStyleBackColor = true;

            dgvResults.Dock = DockStyle.Top;
            dgvResults.Location = new Point(3, 3);
            dgvResults.Name = "dgvResults";
            dgvResults.Size = new Size(786, 480);
            dgvResults.TabIndex = 0;

            progressBar.Dock = DockStyle.Bottom;
            progressBar.Location = new Point(3, 520);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(786, 23);
            progressBar.TabIndex = 1;

            btnReplaceAll.Location = new Point(10, 490);
            btnReplaceAll.Name = "btnReplaceAll";
            btnReplaceAll.Size = new Size(100, 30);
            btnReplaceAll.TabIndex = 2;
            btnReplaceAll.Text = "全部替换";
            btnReplaceAll.UseVisualStyleBackColor = true;

            // 配置tabPageLogs
            tabPageLogs.Controls.Add(txtLog);
            tabPageLogs.Location = new Point(4, 24);
            tabPageLogs.Name = "tabPageLogs";
            tabPageLogs.Padding = new Padding(3);
            tabPageLogs.Size = new Size(792, 550);
            tabPageLogs.TabIndex = 2;
            tabPageLogs.Text = "日志";
            tabPageLogs.UseVisualStyleBackColor = true;

            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(786, 544);
            txtLog.TabIndex = 0;

            // 配置状态栏
            statusStrip1.Items.AddRange([toolStripStatusLabel]);
            statusStrip1.Location = new Point(0, 578);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(800, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";

            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(39, 17);
            toolStripStatusLabel.Text = "就绪";

            // 添加所有控件到窗体
            if (Controls != null)
            {
                Controls.Add(tabControl1);
                Controls.Add(statusStrip1);
            }

            // 恢复布局
            tabControl1.ResumeLayout(false);
            tabPageSettings.ResumeLayout(false);
            tabPageSettings.PerformLayout();
            tabPageResults.ResumeLayout(false);
            ((ISupportInitialize)dgvResults).EndInit();
            tabPageLogs.ResumeLayout(false);
            tabPageLogs.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        // 添加缺失的SetupDataGridView方法
        private void SetupDataGridView()
        {
            dgvResults.AutoGenerateColumns = false;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.ReadOnly = false;

            // 添加列
            var keyColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Key",
                HeaderText = "本地化键",
                ReadOnly = true,
                Width = 200
            };

            var originalColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "OriginalText",
                HeaderText = "原始文本",
                ReadOnly = true,
                Width = 250
            };

            var localizedColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LocalizedText",
                HeaderText = "本地化文本",
                ReadOnly = false,
                Width = 250
            };

            dgvResults.Columns.AddRange([keyColumn, originalColumn, localizedColumn]);

            // 设置数据源
            dgvResults.DataSource = _foundItems;
        }

        private void InitializeServices()
        {
            try
            {
                string backupDir = !string.IsNullOrEmpty(txtBackupDir.Text) ? txtBackupDir.Text :
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");

                _backupManager = new BackupManager(backupDir);
                _localizationHandler = new LocalizationFileHandler(_targetLanguageCode, GetLocalizationDirectory());
                _searchService = new SearchService(GetSearchDirectory());

                // 确保_backupManager不为null再创建ReplacementService
                if (_backupManager != null)
                {
                    _replacementService = new ReplacementService(_localizationHandler, _backupManager);

                    if (_replacementService != null)
                    {
                        _replacementService.ProgressUpdated += ProgressUpdated;
                        _replacementService.ReplacementCompleted += ReplacementCompleted;
                    }
                }

                LogManager.Log("服务初始化完成");
            }
            catch (Exception ex)
            {
                LogManager.Log($"服务初始化失败: {ex.Message}");
                MessageBox.Show($"初始化服务时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            try
            {
                var settings = ConfigManager.LoadSettings();
                if (settings != null)
                {
                    txtModDir.Text = settings.ModDirectory;
                    txtLocalizationDir.Text = settings.LocalizationDirectory;
                    txtBackupDir.Text = settings.BackupDirectory;
                    _targetLanguageCode = settings.TargetLanguage;
                }
                LogManager.Log("配置加载完成");
            }
            catch (Exception ex)
            {
                LogManager.Log($"加载配置失败: {ex.Message}");
            }
        }

        private void SetupLanguageComboBox()
        {
            var languages = new Dictionary<string, string>
            {
                {"zh-cn", "简体中文 (zh-cn)"},
                {"zh-tw", "繁体中文 (zh-tw)"},
                {"en-us", "英语 (en-us)"},
                {"ja", "日语 (ja)"},
                {"ko", "韩语 (ko)"},
                {"fr", "法语 (fr)"},
                {"de", "德语 (de)"},
                {"es", "西班牙语 (es)"},
                {"ru", "俄语 (ru)"}
            };

            var bindingSource = new BindingSource(languages, null);
            cboTargetLanguage.DataSource = bindingSource;
            cboTargetLanguage.DisplayMember = "Value";
            cboTargetLanguage.ValueMember = "Key";

            if (!string.IsNullOrEmpty(_targetLanguageCode) && languages.ContainsKey(_targetLanguageCode))
            {
                cboTargetLanguage.SelectedValue = _targetLanguageCode;
            }
            else
            {
                cboTargetLanguage.SelectedValue = "zh-cn";
            }
        }

        private void AttachEventHandlers()
        {
            FormClosing += MainForm_FormClosing;
            btnStartSearch.Click += BtnStartSearch_Click;
            btnReplaceAll.Click += BtnReplaceAll_Click;
            btnSelectModDir.Click += BtnSelectModDir_Click;
            btnSelectBackupDir.Click += BtnSelectBackupDir_Click;
            btnSelectLocalizationDir.Click += BtnSelectLocalizationDir_Click;
            cboTargetLanguage.SelectedIndexChanged += CboTargetLanguage_SelectedIndexChanged;
            dgvResults.CellEndEdit += DgvResults_CellEndEdit;
        }
        #endregion

        #region 事件处理程序
        private void BtnSelectModDir_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择MOD目录";

            if (!string.IsNullOrEmpty(txtModDir.Text) && Directory.Exists(txtModDir.Text))
            {
                dialog.SelectedPath = txtModDir.Text;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtModDir.Text = dialog.SelectedPath;

                if (string.IsNullOrEmpty(txtLocalizationDir.Text))
                {
                    string locDir = Path.Combine(dialog.SelectedPath, "Localization");
                    if (Directory.Exists(locDir))
                    {
                        txtLocalizationDir.Text = locDir;
                    }
                }

                if (string.IsNullOrEmpty(txtBackupDir.Text))
                {
                    string backupDir = Path.Combine(dialog.SelectedPath, "Backup");
                    Directory.CreateDirectory(backupDir);
                    txtBackupDir.Text = backupDir;
                }

                UpdateServices();
            }
        }

        private void BtnSelectBackupDir_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择备份目录";

            if (!string.IsNullOrEmpty(txtBackupDir.Text) && Directory.Exists(txtBackupDir.Text))
            {
                dialog.SelectedPath = txtBackupDir.Text;
            }
            else if (!string.IsNullOrEmpty(txtModDir.Text) && Directory.Exists(txtModDir.Text))
            {
                string defaultBackupDir = Path.Combine(txtModDir.Text, "Backup");
                if (Directory.Exists(defaultBackupDir))
                {
                    dialog.SelectedPath = defaultBackupDir;
                }
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtBackupDir.Text = dialog.SelectedPath;
                if (_backupManager != null)
                {
                    _backupManager.SetBackupDirectory(dialog.SelectedPath);
                }
                UpdateServices();
            }
        }

        private void BtnSelectLocalizationDir_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择本地化文件目录";

            if (!string.IsNullOrEmpty(txtLocalizationDir.Text) && Directory.Exists(txtLocalizationDir.Text))
            {
                dialog.SelectedPath = txtLocalizationDir.Text;
            }
            else if (!string.IsNullOrEmpty(txtModDir.Text) && Directory.Exists(txtModDir.Text))
            {
                string defaultLocDir = Path.Combine(txtModDir.Text, "Localization");
                if (Directory.Exists(defaultLocDir))
                {
                    dialog.SelectedPath = defaultLocDir;
                }
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtLocalizationDir.Text = dialog.SelectedPath;
                UpdateServices();
            }
        }

        private void BtnStartSearch_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtModDir.Text) || !Directory.Exists(txtModDir.Text))
            {
                MessageBox.Show("请先选择有效的MOD目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_searchService == null)
            {
                MessageBox.Show("搜索服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                toolStripStatusLabel.Text = "正在搜索本地化项...";
                btnStartSearch.Enabled = false;
                _foundItems.Clear();
                _foundItems.AddRange(_searchService.SearchLocalizationItems());
                dgvResults.Refresh();
                toolStripStatusLabel.Text = $"搜索完成，找到 {_foundItems.Count} 项";
                btnStartSearch.Enabled = true;
            }
            catch (Exception ex)
            {
                LogManager.Log($"搜索失败: {ex.Message}");
                MessageBox.Show($"搜索时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabel.Text = "搜索失败";
                btnStartSearch.Enabled = true;
            }
        }

        private void BtnReplaceAll_Click(object? sender, EventArgs e)
        {
            if (_foundItems.Count == 0)
            {
                MessageBox.Show("没有可替换的本地化项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_replacementService == null)
            {
                MessageBox.Show("替换服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"确定要替换所有 {_foundItems.Count} 项本地化内容吗？\n这将修改源文件并创建备份。",
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

                Task.Run(() =>
                {
                    _replacementService.ReplaceAll(_foundItems);
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

        private void CboTargetLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboTargetLanguage.SelectedValue != null)
            {
                _targetLanguageCode = cboTargetLanguage.SelectedValue.ToString() ?? "zh-cn";
                UpdateServices();
            }
        }

        private void DgvResults_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 2 && e.RowIndex < _foundItems.Count)
            {
                var item = _foundItems[e.RowIndex];
                var cellValue = dgvResults.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                item.LocalizedText = cellValue?.ToString() ?? "";
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                SaveCurrentSettings();
                LogManager.SaveLog();
                LogManager.Log("程序已关闭");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region 辅助方法
        private void UpdateServices()
        {
            try
            {
                string locDir = GetLocalizationDirectory();
                if (_localizationHandler != null)
                {
                    _localizationHandler.UpdateDirectory(locDir);
                    _localizationHandler.SetTargetLanguage(_targetLanguageCode);
                }
                else
                {
                    _localizationHandler = new LocalizationFileHandler(_targetLanguageCode, locDir);
                }

                if (_searchService != null)
                {
                    _searchService.UpdateSearchDirectory(GetSearchDirectory());
                }
                else
                {
                    _searchService = new SearchService(GetSearchDirectory());
                }

                if (!string.IsNullOrEmpty(txtBackupDir.Text))
                {
                    if (_backupManager != null)
                    {
                        _backupManager.SetBackupDirectory(txtBackupDir.Text);
                    }
                    else
                    {
                        _backupManager = new BackupManager(txtBackupDir.Text);
                    }
                }

                // 确保_backupManager不为null再创建ReplacementService
                if (_backupManager != null)
                {
                    _replacementService = new ReplacementService(_localizationHandler, _backupManager);
                    if (_replacementService != null)
                    {
                        _replacementService.ProgressUpdated += ProgressUpdated;
                        _replacementService.ReplacementCompleted += ReplacementCompleted;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"更新服务失败: {ex.Message}");
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
            var settings = new AppSettings
            {
                ModDirectory = txtModDir.Text,
                LocalizationDirectory = txtLocalizationDir.Text,
                BackupDirectory = txtBackupDir.Text,
                TargetLanguage = _targetLanguageCode
            };
            ConfigManager.SaveSettings(settings);
        }

        private string GetSearchDirectory()
        {
            return txtModDir.Text;
        }

        private string GetLocalizationDirectory()
        {
            return txtLocalizationDir.Text;
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
