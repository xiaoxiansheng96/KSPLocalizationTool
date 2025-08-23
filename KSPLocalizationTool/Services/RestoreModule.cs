using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KSPLocalizationTool.Services
{
    /// <summary>
    /// 恢复模块，负责从备份恢复文件及相关UI控件
    /// </summary>
    public class RestoreModule
    {
        // UI控件
        private Label lblRestorePoint = null!;
        private ComboBox cboRestorePoints = null!;
        private Button btnRestore = null!;

        // 恢复状态
        private bool _isRestoring;

        // 事件
        public event Action<string>? StatusChanged;
        public event Action<int>? ProgressUpdated;
        public event Action<bool, string>? RestoreCompleted;

        /// <summary>
        /// 获取恢复点标签控件
        /// </summary>
        public Label RestorePointLabel => lblRestorePoint;

        /// <summary>
        /// 获取恢复点下拉框控件
        /// </summary>
        public ComboBox RestorePointsComboBox => cboRestorePoints;

        /// <summary>
        /// 获取恢复按钮控件
        /// </summary>
        public Button RestoreButton => btnRestore;

        /// <summary>
        /// 备份根目录
        /// </summary>
        public string? BackupRootDirectory { get; set; }

        /// <summary>
        /// MOD根目录（恢复目标目录）
        /// </summary>
        public string? ModRootDirectory { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public RestoreModule()
        {
            InitializeControls();
            SetupEventHandlers();
        }

        /// <summary>
        /// 初始化UI控件
        /// </summary>
        private void InitializeControls()
        {
            // 恢复点标签
            lblRestorePoint = new Label
            {
                Text = "选择恢复点:",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 10, 10, 10),
                Width = 100,
                Font = new System.Drawing.Font("微软雅黑", 10F)
            };

            // 恢复点下拉框
            cboRestorePoints = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 10, 10, 10),
                Font = new System.Drawing.Font("微软雅黑", 10F),
                Height = 30
            };

            // 恢复按钮
            btnRestore = new Button
            {
                Text = "恢复选中项",
                Width = 120,
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
            btnRestore.Click += RestoreFromBackup;
        }

        /// <summary>
        /// 加载备份目录中的所有恢复点
        /// </summary>
        public void LoadRestorePoints()
        {
            if (string.IsNullOrEmpty(BackupRootDirectory) || !Directory.Exists(BackupRootDirectory))
            {
                StatusChanged?.Invoke("备份目录不存在或未设置");
                cboRestorePoints.Items.Clear();
                return;
            }

            try
            {
                // 获取所有按日期时间命名的备份文件夹
                var backupFolders = Directory.GetDirectories(BackupRootDirectory)
                    .Where(dir => IsValidBackupFolderName(Path.GetFileName(dir)))
                    .OrderByDescending(dir => Path.GetFileName(dir)) // 最新的备份在前面
                    .ToList();

                cboRestorePoints.Items.Clear();

                if (backupFolders.Count == 0)
                {
                    StatusChanged?.Invoke("未找到任何备份");
                    return;
                }

                // 添加到下拉框
                foreach (var folder in backupFolders)
                {
                    string folderName = Path.GetFileName(folder);
                    // 格式化显示名称（添加可读性更好的日期时间格式）
                    if (DateTime.TryParseExact(folderName, "yyyyMMdd_HHmmss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime backupTime))
                    {
                        cboRestorePoints.Items.Add(new BackupItem
                        {
                            DisplayName = $"{backupTime:yyyy-MM-dd HH:mm:ss} ({folderName})",
                            FolderPath = folder
                        });
                    }
                    else
                    {
                        cboRestorePoints.Items.Add(new BackupItem
                        {
                            DisplayName = folderName,
                            FolderPath = folder
                        });
                    }
                }

                // 默认选择最新的备份
                if (cboRestorePoints.Items.Count > 0)
                {
                    cboRestorePoints.SelectedIndex = 0;
                }

                StatusChanged?.Invoke($"已加载 {backupFolders.Count} 个恢复点");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"加载恢复点失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证文件夹名称是否为有效的备份名称（yyyyMMdd_HHmmss格式）
        /// </summary>
        private static bool IsValidBackupFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName) || folderName.Length != 15)
                return false;

            return DateTime.TryParseExact(folderName, "yyyyMMdd_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _);
        }

        /// <summary>
        /// 从选中的备份恢复文件
        /// </summary>
        private void RestoreFromBackup(object? sender, EventArgs e)
        {
            if (_isRestoring) return;

            // 验证备份目录
            if (string.IsNullOrEmpty(BackupRootDirectory) || !Directory.Exists(BackupRootDirectory))
            {
                MessageBox.Show("备份目录不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 验证MOD目录
            if (string.IsNullOrEmpty(ModRootDirectory) || !Directory.Exists(ModRootDirectory))
            {
                MessageBox.Show("MOD目录不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 验证选中的恢复点
            if (cboRestorePoints.SelectedItem == null)
            {
                MessageBox.Show("请选择一个恢复点", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 确认恢复操作
            var selectedBackup = (BackupItem)cboRestorePoints.SelectedItem;
            var result = MessageBox.Show(
                $"确定要从以下备份恢复吗？\n{selectedBackup.DisplayName}\n\n此操作将覆盖现有文件！",
                "确认恢复",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            // 启动后台恢复
            var worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

            // 修复：通过DoWorkEventArgs传递结果
            worker.DoWork += (s, args) => args.Result = PerformRestore(selectedBackup.FolderPath, worker);
            worker.ProgressChanged += (s, args) => ProgressUpdated?.Invoke(args.ProgressPercentage);
            worker.RunWorkerCompleted += (s, args) =>
            {
                _isRestoring = false;
                btnRestore.Enabled = true;

                if (args.Error != null)
                {
                    string errorMsg = args.Error.Message ?? "未知错误";
                    RestoreCompleted?.Invoke(false, $"恢复失败: {errorMsg}");
                    StatusChanged?.Invoke($"恢复失败: {errorMsg}");
                }
                else
                {
                    string resultMsg = args.Result as string ?? "恢复成功";
                    RestoreCompleted?.Invoke(true, resultMsg);
                    StatusChanged?.Invoke($"恢复完成: {resultMsg}");
                }
            };

            _isRestoring = true;
            btnRestore.Enabled = false;
            StatusChanged?.Invoke("开始恢复文件...");
            worker.RunWorkerAsync();
        }

        /// <summary>
        /// 执行恢复操作（返回恢复结果字符串）
        /// </summary>
        private string PerformRestore(string backupDirectory, BackgroundWorker worker)
        {
            try
            {
                // 获取所有备份文件
                var backupFiles = Directory.GetFiles(backupDirectory, "*.*", SearchOption.AllDirectories).ToList();
                int totalFiles = backupFiles.Count;
                int completedFiles = 0;

                if (totalFiles == 0)
                {
                    // 修复：返回结果而不是设置worker.Result
                    return "备份目录中没有文件";
                }

                foreach (string backupFilePath in backupFiles)
                {
                    // 计算原始文件路径（此处省略具体实现，保持原有逻辑）
                    string relativePath = Path.GetRelativePath(backupDirectory, backupFilePath);
                    // 确保ModRootDirectory不为null
                    string targetPath = Path.Combine(ModRootDirectory ?? string.Empty, relativePath);

                    // 创建目标目录
                    string? targetDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // 复制文件
                    File.Copy(backupFilePath, targetPath, true);

                    // 更新进度
                    completedFiles++;
                    int progress = (int)((double)completedFiles / totalFiles * 100);
                    worker.ReportProgress(progress);
                }

                // 修复：返回结果而不是设置worker.Result
                return $"成功恢复 {completedFiles}/{totalFiles} 个文件";
            }
            catch (Exception)
            {
                worker.ReportProgress(0);
                // 使用throw保留原始堆栈信息
                throw;
            }
        }

        // 备份项辅助类（原代码中可能遗漏，补充定义）
        public class BackupItem
        {
            // 添加required修饰符确保属性初始化
            public required string DisplayName { get; set; }
            public required string FolderPath { get; set; }

            // 用于下拉框显示
            public override string ToString() => DisplayName;
        }
        // 在RestoreModule类中添加以下方法
        /// <summary>
        /// 执行恢复操作
        /// </summary>
        /// <param name="restorePoint">恢复点名称（备份文件夹名）</param>
        public async Task RestoreAsync(string restorePoint)
        {
            if (string.IsNullOrEmpty(restorePoint) || !Directory.Exists(BackupRootDirectory))
            {
                StatusChanged?.Invoke("恢复点不存在或备份目录无效");
                RestoreCompleted?.Invoke(false, "恢复失败：无效的恢复点");
                return;
            }

            _isRestoring = true;
            StatusChanged?.Invoke($"开始恢复到 {restorePoint}...");

            string restoreDir = Path.Combine(BackupRootDirectory, restorePoint);
            if (!Directory.Exists(restoreDir))
            {
                StatusChanged?.Invoke("恢复点目录不存在");
                RestoreCompleted?.Invoke(false, "恢复失败：恢复点目录不存在");
                _isRestoring = false;
                return;
            }

            // 获取所有备份文件
            var backupFiles = Directory.GetFiles(restoreDir, "*.*", SearchOption.AllDirectories);
            int totalFiles = backupFiles.Length;
            int completed = 0;

            // 异步恢复每个文件
            foreach (var backupFile in backupFiles)
            {
                if (!_isRestoring) break; // 支持取消

                try
                {
                    // 计算目标路径（替换备份目录为MOD目录）
                    string relativePath = backupFile.Replace(restoreDir, "");
                    // 修复第363行CS8604：确保ModRootDirectory不为null
                    string targetFile = Path.Combine(ModRootDirectory ?? throw new InvalidOperationException("ModRootDirectory不能为null"), relativePath.TrimStart(Path.DirectorySeparatorChar));

                    // 创建目标目录
                    // 修复第366行CS8604：检查Path.GetDirectoryName的返回值不为null
                    string targetDir = Path.GetDirectoryName(targetFile) ?? throw new InvalidOperationException($"无法获取文件 '{targetFile}' 的目录");
                    if (targetDir != null)
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    else
                    {
                        throw new InvalidOperationException($"无法获取文件 '{targetFile}' 的目录");
                    }

                    // 复制文件覆盖目标
                    File.Copy(backupFile, targetFile, true);
                    completed++;

                    // 更新进度
                    int progress = (int)((double)completed / totalFiles * 100);
                    ProgressUpdated?.Invoke(progress);
                    StatusChanged?.Invoke($"已恢复：{Path.GetFileName(targetFile)} ({completed}/{totalFiles})");
                }
                catch (System.Exception ex)
                {
                    StatusChanged?.Invoke($"恢复失败 {Path.GetFileName(backupFile)}：{ex.Message}");
                }

                // 避免UI卡顿
                await Task.Delay(10);
            }

            _isRestoring = false;
            if (completed == totalFiles)
            {
                RestoreCompleted?.Invoke(true, $"成功恢复 {completed} 个文件");
            }
            else
            {
                RestoreCompleted?.Invoke(false, $"部分恢复：成功 {completed}/{totalFiles} 个文件");
            }
        }

        /// <summary>
        /// 取消恢复操作
        /// </summary>
        public void CancelRestore()
        {
            _isRestoring = false;
            StatusChanged?.Invoke("恢复已取消");
        }
    }
}