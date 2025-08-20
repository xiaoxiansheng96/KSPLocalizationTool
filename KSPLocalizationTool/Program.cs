using System;
using System.Windows.Forms;
// 添加必要的命名空间引用，解决ConfigManager和LogManager的上下文问题
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

            // 确保ConfigManager和LogManager在引用的命名空间中正确定义
            var settings = ConfigManager.LoadSettings(); // 现在可以正确找到ConfigManager

            try
            {
                LogManager.Log("应用程序启动"); // 现在可以正确找到LogManager
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                LogManager.Log($"应用程序崩溃: {ex.Message}"); // 现在可以正确找到LogManager
                MessageBox.Show($"程序发生未处理异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
