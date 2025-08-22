using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace KSPLocalizationTool.Services
{
    /// <summary>
    /// 备份模块，负责文件备份功能及相关UI控件
    /// </summary>
    public class BackupModule
    {
        // UI控件
        private Label lblBackupDirectory;
        private TextBox txtBackupDirectory;
        private Button btnBrowseBackup;
        private Button btnCreateBackup;

        // 备份状态
        private bool _isBackingUp;

        // 事件
        public event Action<string> StatusChanged;
        public event Action<int> ProgressUpdated;
        public event Action<bool, string> BackupCompleted;

        /// <summary>
        /// 获取备份目录文本框控件
        /// </summary>
        public TextBox BackupDirectoryTextBox => txtBackupDirectory;

        /// <summary>
        /// 获取浏览按钮控件
        /// </summary>
        public Button BrowseButton => btnBrowseBackup;

        /// <summary>
        /// 获取创建备份按钮控件
        /// </summary>
        public Button CreateBackupButton => btnCreateBackup;

        /// <summary>
        /// 获取备份目录标签控件
        /// </summary>
        public Label BackupDirectoryLabel => lblBackupDirectory;

        /// <summary>
        /// 构造函数
        /// </summary>
        public BackupModule()
        {
            InitializeControls();
            SetupEventHandlers();
        }

        /// <summary>
        /// 初始化UI控件
        /// </summary>
        private void InitializeControls()
        {
            // 备份目录标签
            lblBackupDirectory = new Label
            {
                Text = "备份目录:",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 10, 10, 10),
                Font = new System.Drawing.Font("微软雅黑", 10F)
            };

            // 备份目录文本框
            txtBackupDirectory = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 10, 10),
                Font = new System.Drawing.Font("微软雅黑", 10F),
                Height = 30
            };

            // 浏览按钮
            btnBrowseBackup = new Button
            {
                Text = "浏览...",
                Width = 100,
                Height = 35,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(8, 10, 8, 10),
                Font = new System.Drawing.Font("微软雅黑", 10F)
            };

            // 创建备份按钮
            btnCreateBackup = new Button
            {
                Text = "创建备份",
                Width = 110,
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
            btnBrowseBackup.Click += BrowseBackupDirectory;
            btnCreateBackup.Click += CreateBackup;
        }

        /// <summary>
        /// 浏览备份目录
        /// </summary>
        private void BrowseBackupDirectory(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog
            {
                Description = "选择备份目录",
                ShowNewFolderButton = true
            })
            {
                if (!string.IsNullOrEmpty(txtBackupDirectory.Text) &&
                    Directory.Exists(txtBackupDirectory.Text))
                {
                    fbd.SelectedPath = txtBackupDirectory.Text;
                }

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtBackupDirectory.Text = fbd.SelectedPath;
                    StatusChanged?.Invoke($"已选择备份目录: {fbd.SelectedPath}");
                }
            }
        }

        /// <summary>
        /// 创建备份
        /// </summary>
        public void CreateBackup(object sender, EventArgs e)
        {
            if (_isBackingUp) return;

            string backupRoot = txtBackupDirectory.Text;
            if (string.IsNullOrEmpty(backupRoot))
            {
                MessageBox.Show("请先选择备份目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 如果没有指定要备份的文件，提示用户
            if (FilesToBackup == null || FilesToBackup.Count == 0)
            {
                MessageBox.Show("没有需要备份的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 启动后台备份
            var worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

            // 修复：通过DoWorkEventArgs传递结果
            worker.DoWork += (s, args) => args.Result = PerformBackup(backupRoot, worker);
            worker.ProgressChanged += (s, args) => ProgressUpdated?.Invoke(args.ProgressPercentage);
            worker.RunWorkerCompleted += (s, args) =>
            {
                _isBackingUp = false;
                btnCreateBackup.Enabled = true;

                if (args.Error != null)
                {
                    BackupCompleted?.Invoke(false, $"备份失败: {args.Error.Message}");
                    StatusChanged?.Invoke($"备份失败: {args.Error.Message}");
                }
                else
                {
                    string result = args.Result as string;
                    BackupCompleted?.Invoke(true, result);
                    StatusChanged?.Invoke($"备份完成: {result}");
                }
            };

            _isBackingUp = true;
            btnCreateBackup.Enabled = false;
            StatusChanged?.Invoke("开始创建备份...");
            worker.RunWorkerAsync();
        }

        /// <summary>
        /// 执行备份操作（返回备份结果字符串）
        /// </summary>
        private string PerformBackup(string backupRoot, BackgroundWorker worker)
        {
            try
            {
                // 确保备份根目录存在
                if (!Directory.Exists(backupRoot))
                {
                    Directory.CreateDirectory(backupRoot);
                    StatusChanged?.Invoke($"已创建备份根目录: {backupRoot}");
                }

                // 创建按日期时间命名的备份文件夹
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupDirectory = Path.Combine(backupRoot, timestamp);
                Directory.CreateDirectory(backupDirectory);

                // 备份文件
                int totalFiles = FilesToBackup.Count;
                int completedFiles = 0;

                foreach (string filePath in FilesToBackup)
                {
                    if (!File.Exists(filePath))
                    {
                        StatusChanged?.Invoke($"跳过不存在的文件: {filePath}");
                        continue;
                    }

                    // 创建与源文件相同的目录结构
                    string relativePath = Path.GetDirectoryName(filePath).Replace(ModRootDirectory, "");
                    string targetDir = Path.Combine(backupDirectory, relativePath.TrimStart(Path.DirectorySeparatorChar));

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // 复制文件
                    string targetPath = Path.Combine(targetDir, Path.GetFileName(filePath));
                    File.Copy(filePath, targetPath, true);

                    // 更新进度
                    completedFiles++;
                    int progress = (int)((double)completedFiles / totalFiles * 100);
                    worker.ReportProgress(progress);
                    StatusChanged?.Invoke($"已备份: {Path.GetFileName(filePath)} ({completedFiles}/{totalFiles})");
                }

                // 修复：返回结果而不是设置worker.Result
                return $"成功备份 {completedFiles}/{totalFiles} 个文件到 {backupDirectory}";
            }
            catch (Exception ex)
            {
                worker.ReportProgress(0);
                throw ex;
            }
        }

        /// <summary>
        /// 需要备份的文件列表
        /// </summary>
        public List<string> FilesToBackup { get; set; } = new List<string>();

        /// <summary>
        /// MOD根目录，用于在备份中保持相对路径结构
        /// </summary>
        public string ModRootDirectory { get; set; }
    }
}