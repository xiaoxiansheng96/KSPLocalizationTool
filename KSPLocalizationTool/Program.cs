using System;
using System.Windows.Forms;

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
            
            // 初始化配置
            // 由于当前上下文中不存在名称“ConfigManager”，暂时移除该调用
            // ConfigManager.LoadConfig();
            // 初始化日志
            // 由于当前上下文中不存在名称“LogManager”，暂时注释掉日志初始化调用
            // LogManager.Initialize();
            
            Application.Run(new MainForm());
            
            // 程序退出时保存日志
            // 由于当前上下文中不存在名称“LogManager”，暂时移除该调用
            // LogManager.SaveLog();
        }
    }
}
