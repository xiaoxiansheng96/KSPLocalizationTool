using KSPLocalizationTool;
using KSPLocalizationTool.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Windows.Forms;

namespace KSPLocalizationTool
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 创建服务容器
            var serviceProvider = new ServiceCollection()
                .AddSingleton<AppConfig>()
                // 注册LogService并提供日志目录路径
                .AddSingleton<LogService>(provider =>
                {
                    // 使用应用程序数据目录作为日志存储位置
                    string logDirectory = Path.Combine(Application.UserAppDataPath, "Logs");
                    return new LogService(logDirectory);
                })
                .AddSingleton<FileSearchService>()
                .AddSingleton<FileBackupService>()
                .AddSingleton<LocalizationKeyGenerator>()
                .AddSingleton<ReplacementService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<RestoreModule>()
                .AddSingleton<Form1>()
                .BuildServiceProvider();

            // 从容器获取Form1实例
            var form1 = serviceProvider.GetRequiredService<Form1>();
            Application.Run(form1);
        }
    }
}