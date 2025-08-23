using KSPLocalizationTool.Models;
using System;

namespace KSPLocalizationTool.Services
{
    public class ConfigService
    {
        private readonly AppConfig _appConfig;

        // 构造函数：初始化配置
        public ConfigService()
        {
            _appConfig = AppConfig.Load();
        }

        // 加载配置
        public AppConfig LoadConfig()
        {
            return _appConfig;
        }

        // 保存配置
        public void SaveConfig(string modDir, string backupDir, string locDir)
        {
            _appConfig.ModDirectory = modDir;
            _appConfig.BackupDirectory = backupDir;
            _appConfig.LocalizationDirectory = locDir;
            _appConfig.Save();
        }

        // 获取筛选参数
        public (string[] CfgFilters, string[] CsFilters) GetFilters()
        {
            return (_appConfig.CfgFilters, _appConfig.CsFilters);
        }

        // 保存筛选参数
        public void SaveFilters(string[] cfgFilters, string[] csFilters)
        {
            _appConfig.CfgFilters = cfgFilters;
            _appConfig.CsFilters = csFilters;
        }
    }
}