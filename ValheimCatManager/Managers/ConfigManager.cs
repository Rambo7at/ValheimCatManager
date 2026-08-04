using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ValheimCatManager;
using ValheimCatManager.Managers;

namespace ValheimCatManager.Managers
{
    public class ConfigManager
    {
        private static ConfigManager _instance;
        public static ConfigManager Instance => _instance ?? (_instance = new ConfigManager());
        private ConfigManager() { }

        public ConfigEntry<bool> BindBoolSetting(ConfigFile cfg, string section, string key, bool defaultValue, string desc) => cfg.Bind(section, key, defaultValue, desc);

        public ConfigEntry<int> BindIntSetting(ConfigFile cfg, string section, string key, int defaultValue, string desc)=> cfg.Bind(section, key, defaultValue, desc);

        public ConfigEntry<float> BindFloatSetting(ConfigFile cfg, string section, string key, float defaultValue, string desc)=> cfg.Bind(section, key, defaultValue, desc);

        public ConfigEntry<string> BindStringSetting(ConfigFile cfg, string section, string key, string defaultValue, string desc)=> cfg.Bind(section, key, defaultValue, desc);

    }
}
