using System;
using System.Windows.Forms;
using KSPLocalizationTool.Services;

namespace KSPLocalizationTool
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 初始化日志
            LogManager.Log("程序启动");

            Application.Run(new MainForm());

            // 程序退出时保存日志
            LogManager.SaveLogToFile();
        }
    }
}