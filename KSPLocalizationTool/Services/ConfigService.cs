using KSPLocalizationTool.Models;
using System;

namespace KSPLocalizationTool.Services
{
    public class ConfigService
    {
        private readonly AppConfig _appConfig;

        // ���캯������ʼ������
        public ConfigService()
        {
            _appConfig = AppConfig.Load();
        }

        // ��������
        public AppConfig LoadConfig()
        {
            return _appConfig;
        }

        // ��������
        public void SaveConfig(string modDir, string backupDir, string locDir)
        {
            _appConfig.ModDirectory = modDir;
            _appConfig.BackupDirectory = backupDir;
            _appConfig.LocalizationDirectory = locDir;
            _appConfig.Save();
        }

        // ��ȡɸѡ����
        public (string[] CfgFilters, string[] CsFilters) GetFilters()
        {
            return (_appConfig.CfgFilters, _appConfig.CsFilters);
        }

        // ����ɸѡ����
        public void SaveFilters(string[] cfgFilters, string[] csFilters)
        {
            _appConfig.CfgFilters = cfgFilters;
            _appConfig.CsFilters = csFilters;
        }
    }
}