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
        // 集合初始化
        private readonly List<LocalizationItem> _foundItems = new();
        private readonly string _targetLanguageCode = "zh-cn";

        // 服务字段
        private readonly SearchService? _searchService;
        private readonly LocalizationFileHandler? _localizationHandler;
        private readonly ReplacementService? _replacementService;
        private readonly BackupManager? _backupManager;

        // 控件字段
        private TabControl? tabControl1;
        private TabPage? tabPageSettings;
        private TabPage? tabPageResults;
        private Button? btnSelectModDir;
        private TextBox? txtModDir;
        private Label? label1;
        private Button? btnSelectLocalizationDir;
        private TextBox? txtLocalizationDir;
        private Label? label3;
        private Button? btnSelectBackupDir;
        private TextBox? txtBackupDir;
        private Label? label2;
        private ComboBox? cboTargetLanguage;
        private Label? label4;
        private Button? btnStartSearch;
        private DataGridView? dgvResults;
        private Button? btnReplaceAll;
        private ProgressBar? progressBar;
        private TabPage? tabPageLogs;
        private TextBox? txtLog;
        private StatusStrip? statusStrip1;
        private ToolStripStatusLabel? toolStripStatusLabel;

        public MainForm()
        {
            InitializeComponent();

            // 初始化服务
            string backupDir = !string.IsNullOrEmpty(txtBackupDir?.Text) ? txtBackupDir.Text :
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");

            _backupManager = new BackupManager(backupDir);
            _localizationHandler = new LocalizationFileHandler(_targetLanguageCode, GetLocalizationDirectory());
            _searchService = new SearchService(GetLocalizationDirectory());

            if (_localizationHandler != null && _backupManager != null)
            {
                _replacementService = new ReplacementService(_localizationHandler, _backupManager);
                _replacementService.ProgressUpdated += ProgressUpdated;
            }

            LoadSettings();
            SetupDataGridView();
            SetupLanguageComboBox();
        }

        // 确保InitializeComponent方法包含完整的控件初始化和布局
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tabControl1 = new TabControl();
            tabPageSettings = new TabPage();
            btnStartSearch = new Button();
            label4 = new Label();
            cboTargetLanguage = new ComboBox();
            btnSelectLocalizationDir = new Button();
            txtLocalizationDir = new TextBox();
            label3 = new Label();
            btnSelectBackupDir = new Button();
            txtBackupDir = new TextBox();
            label2 = new Label();
            btnSelectModDir = new Button();
            txtModDir = new TextBox();
            label1 = new Label();
            tabPageResults = new TabPage();
            progressBar = new ProgressBar();
            btnReplaceAll = new Button();
            dgvResults = new DataGridView();
            tabPageLogs = new TabPage();
            txtLog = new TextBox();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();

            // 配置控件
            tabControl1.SuspendLayout();
            tabPageSettings.SuspendLayout();
            tabPageResults.SuspendLayout();
            ((ISupportInitialize)dgvResults).BeginInit();
            tabPageLogs.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();

            // 主窗体设置
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 600);
            Controls.Add(tabControl1);
            Controls.Add(statusStrip1);
            Name = "MainForm";
            Text = "KSP本地化工具";

            // 标签页控件
            tabControl1.Controls.Add(tabPageSettings);
            tabControl1.Controls.Add(tabPageResults);
            tabControl1.Controls.Add(tabPageLogs);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(800, 575);

            // 设置标签页
            tabPageSettings.Controls.Add(btnStartSearch);
            tabPageSettings.Controls.Add(label4);
            tabPageSettings.Controls.Add(cboTargetLanguage);
            tabPageSettings.Controls.Add(btnSelectLocalizationDir);
            tabPageSettings.Controls.Add(txtLocalizationDir);
            tabPageSettings.Controls.Add(label3);
            tabPageSettings.Controls.Add(btnSelectBackupDir);
            tabPageSettings.Controls.Add(txtBackupDir);
            tabPageSettings.Controls.Add(label2);
            tabPageSettings.Controls.Add(btnSelectModDir);
            tabPageSettings.Controls.Add(txtModDir);
            tabPageSettings.Controls.Add(label1);
            tabPageSettings.Location = new Point(4, 24);
            tabPageSettings.Name = "tabPageSettings";
            tabPageSettings.Padding = new Padding(3);
            tabPageSettings.Text = "设置";
            tabPageSettings.UseVisualStyleBackColor = true;

            // 结果标签页
            tabPageResults.Controls.Add(progressBar);
            tabPageResults.Controls.Add(btnReplaceAll);
            tabPageResults.Controls.Add(dgvResults);
            tabPageResults.Location = new Point(4, 24);
            tabPageResults.Name = "tabPageResults";
            tabPageResults.Padding = new Padding(3);
            tabPageResults.Text = "结果";
            tabPageResults.UseVisualStyleBackColor = true;

            // 日志标签页
            tabPageLogs.Controls.Add(txtLog);
            tabPageLogs.Location = new Point(4, 24);
            tabPageLogs.Name = "tabPageLogs";
            tabPageLogs.Padding = new Padding(3);
            tabPageLogs.Text = "日志";
            tabPageLogs.UseVisualStyleBackColor = true;

            // 按钮和文本框布局
            label1.Text = "MOD目录:";
            label1.Location = new Point(15, 20);
            label1.Size = new Size(70, 20);

            txtModDir.Location = new Point(90, 20);
            txtModDir.Size = new Size(500, 23);

            btnSelectModDir.Text = "浏览...";
            btnSelectModDir.Location = new Point(600, 20);
            btnSelectModDir.Size = new Size(75, 23);
            btnSelectModDir.Click += BtnSelectModDir_Click;

            // 其他控件的位置和大小设置...
            label2.Text = "备份目录:";
            label2.Location = new Point(15, 60);
            label2.Size = new Size(70, 20);

            txtBackupDir.Location = new Point(90, 60);
            txtBackupDir.Size = new Size(500, 23);

            btnSelectBackupDir.Text = "浏览...";
            btnSelectBackupDir.Location = new Point(600, 60);
            btnSelectBackupDir.Size = new Size(75, 23);

            label3.Text = "本地化目录:";
            label3.Location = new Point(15, 100);
            label3.Size = new Size(70, 20);

            txtLocalizationDir.Location = new Point(90, 100);
            txtLocalizationDir.Size = new Size(500, 23);

            btnSelectLocalizationDir.Text = "浏览...";
            btnSelectLocalizationDir.Location = new Point(600, 100);
            btnSelectLocalizationDir.Size = new Size(75, 23);

            label4.Text = "目标语言:";
            label4.Location = new Point(15, 140);
            label4.Size = new Size(70, 20);

            cboTargetLanguage.Location = new Point(90, 140);
            cboTargetLanguage.Size = new Size(200, 23);

            btnStartSearch.Text = "开始搜索";
            btnStartSearch.Location = new Point(90, 180);
            btnStartSearch.Size = new Size(100, 30);

            // 结果表格
            dgvResults.Dock = DockStyle.Top;
            dgvResults.Size = new Size(792, 400);

            btnReplaceAll.Text = "全部替换";
            btnReplaceAll.Location = new Point(10, 410);

            progressBar.Dock = DockStyle.Bottom;
            progressBar.Size = new Size(792, 23);

            // 日志文本框
            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;

            // 状态栏
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel });
            statusStrip1.Location = new Point(0, 575);
            statusStrip1.Size = new Size(800, 25);

            toolStripStatusLabel.Text = "就绪";

            // 恢复布局
            tabControl1.ResumeLayout(false);
            tabPageSettings.ResumeLayout(false);
            tabPageSettings.PerformLayout();
            tabPageResults.ResumeLayout(false);
            ((ISupportInitialize)dgvResults).EndInit();
            tabPageLogs.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private void BtnSelectModDir_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择MOD目录";

            if (!string.IsNullOrEmpty(txtModDir?.Text) && Directory.Exists(txtModDir.Text))
            {
                dialog.SelectedPath = txtModDir.Text;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtModDir!.Text = dialog.SelectedPath;
                UpdateServices();
            }
        }

        // 其他方法实现...
        private void ProgressUpdated(int progress)
        {
            if (progressBar?.InvokeRequired ?? false)
            {
                progressBar?.Invoke(new Action<int>(UpdateProgress), progress);
            }
            else
            {
                UpdateProgress(progress);
            }
        }

        private static void UpdateProgress(int progress)
        {
            // 实现更新进度逻辑
        }

        private static string GetLocalizationDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization");
        }

        private static void LoadSettings()
        {
            // 实现设置加载逻辑
        }

        private void SetupDataGridView()
        {
            if (dgvResults == null) return;

            dgvResults.AutoGenerateColumns = false;
            dgvResults.DataSource = _foundItems;

            // 添加列定义
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colKey",
                HeaderText = "键",
                DataPropertyName = "Key",
                Width = 150
            });

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colOriginal",
                HeaderText = "原始文本",
                DataPropertyName = "OriginalText",
                Width = 200
            });

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colLast",
                HeaderText = "本地化文本",
                DataPropertyName = "LocalizedText",
                Width = 200
            });
        }

        private void SetupLanguageComboBox()
        {
            if (cboTargetLanguage is null) return;

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

            cboTargetLanguage.DataSource = new BindingSource(languages, null);
            cboTargetLanguage.DisplayMember = "Value";
            cboTargetLanguage.ValueMember = "Key";

            if (languages.ContainsKey(_targetLanguageCode))
            {
                cboTargetLanguage.SelectedValue = _targetLanguageCode;
            }
        }

        private void UpdateServices()
        {
            // 实现服务更新逻辑
        }

        private IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
