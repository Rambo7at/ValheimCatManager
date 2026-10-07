using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimCatManager;

internal class CatConfig
{
    private static CatConfig _instance;
    public static CatConfig Instance => _instance ?? (_instance = new CatConfig());

    public void Load(ConfigFile cfg)
    {
        // 数据导出（开发参考用，导出到 config/CatManager/GameData）
        EnableDataExport = cfg.Bind("数据导出", "启用数据导出", false, "总开关。开启后玩家进世界时导出游戏原始配置，供制作自定义内容参考；普通玩家无需开启");
        ExportMonsterSpawn = cfg.Bind("数据导出", "导出怪物生成", true, "怪物生成配置 monster_spawn.txt");
        ExportBiomeEnvSetup = cfg.Bind("数据导出", "导出群系环境", true, "各群系天气列表与音乐 biome_environments.txt");
        ExportEnvData = cfg.Bind("数据导出", "导出天气详情", true, "所有天气的颜色、雾、风、粒子、音效 environments.txt");
        ExportLocations = cfg.Bind("数据导出", "导出地点", true, "地点/建筑生成配置 locations.txt");
        ExportVegetation = cfg.Bind("数据导出", "导出植被", true, "植被配置 vegetation.txt");
        ExportClutter = cfg.Bind("数据导出", "导出地面杂物", true, "草、碎石、地面雾气配置 clutter.txt");

        EnableLegacyWorldUpgrade = cfg.Bind("旧世界升级", "启用旧世界自动升级", false,
      "【警告】开启后，检测到旧存档已生成过区块时，会自动删除所有植被和地点数据并重新生成。" +
       "此操作不可逆，执行前务必备份世界存档！！！！！" +
       "仅用于从旧版本升级到新版本，完成后建议关闭。");
    }

    /// <summary>注：数据导出总开关，默认关（开发参考用）</summary>
    public ConfigEntry<bool> EnableDataExport { get; set; }
    public ConfigEntry<bool> ExportMonsterSpawn { get; set; }
    public ConfigEntry<bool> ExportBiomeEnvSetup { get; set; }
    public ConfigEntry<bool> ExportEnvData { get; set; }
    public ConfigEntry<bool> ExportLocations { get; set; }
    public ConfigEntry<bool> ExportVegetation { get; set; }
    public ConfigEntry<bool> ExportClutter { get; set; }

    /// <summary>旧世界自动升级：检测到已有生成区块时，清理旧植被/地点 ZDO 和区块标记，让新规则生效</summary>
    public ConfigEntry<bool> EnableLegacyWorldUpgrade { get; set; }
}

