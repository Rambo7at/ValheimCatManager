using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimCatManager.CatUtils;

internal class GameDataExporter
{
    private static GameDataExporter _instance;
    public static GameDataExporter Instance => _instance ?? (_instance = new GameDataExporter());

    /// <summary>注：是否已经导出过，防止重复跑</summary>
    private bool exported = false;

    /// <summary>注：导出根目录</summary>
    private string exportDir = "";

    public GameDataExporter()
    {
        new Harmony("com.rambo7at.CatManager.GameDataExporter").PatchAll(typeof(GameDataExporterPatch));
    }

    /// <summary>注：导出全部游戏配置到 txt</summary>
    private void Export()
    {
        if (exported) return;

        var cfg = CatConfig.Instance;
        if (cfg.EnableDataExport == null || !cfg.EnableDataExport.Value) return;

        exported = true;

        exportDir = Path.Combine(Paths.ConfigPath, "CatManager", "GameData");
        Directory.CreateDirectory(exportDir);

        if (cfg.ExportMonsterSpawn.Value) ExportMonsterSpawn();
        if (cfg.ExportBiomeEnvSetup.Value) ExportBiomeEnvSetup();
        if (cfg.ExportEnvData.Value) ExportEnvData();
        if (cfg.ExportLocations.Value) ExportLocations();
        if (cfg.ExportVegetation.Value) ExportVegetation();
        if (cfg.ExportClutter.Value) ExportClutter();
    }


    /// <summary>注：导出群系环境配置（天气+音乐）</summary>
    private void ExportBiomeEnvSetup()
    {
        if (EnvMan.instance == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 群系环境配置（EnvMan.m_biomes） ===");
        sb.AppendLine();

        foreach (var data in EnvMan.instance.m_biomes)
        {
            sb.AppendLine($"[{data.m_biome}]");
            sb.AppendLine($"  音乐(白天): {data.m_musicDay}");
            sb.AppendLine($"  音乐(夜晚): {data.m_musicNight}");
            sb.AppendLine($"  音乐(早晨): {data.m_musicMorning}");
            sb.AppendLine($"  音乐(傍晚): {data.m_musicEvening}");
            sb.AppendLine($"  天气列表:");
            foreach (var entry in data.m_environments)
            {
                sb.AppendLine($"    - {entry.m_environment} (权重: {entry.m_weight})");
                sb.AppendLine($"      灰烬覆盖: {entry.m_ashlandsOverride}  北境覆盖: {entry.m_deepnorthOverride}");
            }
            sb.AppendLine();
        }

        string path = Path.Combine(exportDir, "biome_environments.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[GameDataExporter.ExportBiomeEnvSetup] 导出 {EnvMan.instance.m_biomes.Count} 个群系环境到 {path}");
    }


    /// <summary>注：导出所有环境（天气）详细配置</summary>
    private void ExportEnvData()
    {
        if (EnvMan.instance == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 环境详细配置（EnvMan.m_environments） ===");
        sb.AppendLine();

        foreach (EnvSetup data in EnvMan.instance.m_environments)
        {
            sb.AppendLine($"[{data.m_name}]");
            sb.AppendLine($"  默认: {data.m_default}");
            sb.AppendLine($"  潮湿: {data.m_isWet}  冰冻: {data.m_isFreezing}(夜晚:{data.m_isFreezingAtNight})  寒冷: {data.m_isCold}(夜晚:{data.m_isColdAtNight})  恒黑: {data.m_alwaysDark}");
            sb.AppendLine($"  积雪: {data.m_snowBuildup}");
            sb.AppendLine($"  环境色: 白天{data.m_ambColorDay} 夜晚{data.m_ambColorNight}");
            sb.AppendLine($"  雾色: 白天{data.m_fogColorDay} 夜晚{data.m_fogColorNight} 早晨{data.m_fogColorMorning} 傍晚{data.m_fogColorEvening}");
            sb.AppendLine($"  雾太阳光: 白天{data.m_fogColorSunDay} 夜晚{data.m_fogColorSunNight} 早晨{data.m_fogColorSunMorning} 傍晚{data.m_fogColorSunEvening}");
            sb.AppendLine($"  雾密度: 白天{data.m_fogDensityDay} 夜晚{data.m_fogDensityNight} 早晨{data.m_fogDensityMorning} 傍晚{data.m_fogDensityEvening}");
            sb.AppendLine($"  太阳色: 白天{data.m_sunColorDay} 夜晚{data.m_sunColorNight} 早晨{data.m_sunColorMorning} 傍晚{data.m_sunColorEvening}");
            sb.AppendLine($"  太阳角度: {data.m_sunAngle}  光照: 白天{data.m_lightIntensityDay} 夜晚{data.m_lightIntensityNight}");
            sb.AppendLine($"  风: {data.m_windMin} - {data.m_windMax}");
            sb.AppendLine($"  极光强度: 白天{data.m_auroraIntensityDay} 夜晚{data.m_auroraIntensityNight} 早晨{data.m_auroraIntensityMorning} 傍晚{data.m_auroraIntensityEvening}");
            sb.AppendLine($"  云透明度: 白天{data.m_cloudOpacityDay} 夜晚{data.m_cloudOpacityNight} 早晨{data.m_cloudOpacityMorning} 傍晚{data.m_cloudOpacityEvening}");
            sb.AppendLine($"  雨云透明度: {data.m_rainCloudAlpha}");
            sb.AppendLine($"  环境物体: {data.m_envObject?.name ?? "无"}");
            sb.AppendLine($"  粒子效果: {(data.m_psystems != null && data.m_psystems.Length > 0 ? string.Join(", ", data.m_psystems.Select(p => p?.name).Where(n => n != null)) : "无")} (仅室外: {data.m_psystemsOutsideOnly})");
            sb.AppendLine($"  AO色: {data.m_ambientOcclusionColor}  强度: 白天{data.m_aoIntensityDay} 夜晚{data.m_aoIntensityNight} 早晨{data.m_aoIntensityMorning} 傍晚{data.m_aoIntensityEvening}");
            sb.AppendLine($"  环境音效: {data.m_ambientLoop?.name ?? "无"} (音量: {data.m_ambientVol})");
            sb.AppendLine($"  音乐: 白天{data.m_musicDay} 夜晚{data.m_musicNight} 早晨{data.m_musicMorning} 傍晚{data.m_musicEvening}");
            sb.AppendLine();
        }

        string path = Path.Combine(exportDir, "environments.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[GameDataExporter.ExportEnvData] 导出 {EnvMan.instance.m_environments.Count} 个环境到 {path}");
    }

    /// <summary>注：导出地点生成配置（ZoneSystem.m_locations）</summary>
    private void ExportLocations()
    {
        if (ZoneSystem.instance == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 地点配置（ZoneSystem.m_locations） ===");
        sb.AppendLine();

        int index = 0;
        foreach (var loc in ZoneSystem.instance.m_locations)
        {
            // 注：m_prefab 是 SoftReference<GameObject>，资产名取 .Name；m_prefabName 兜底
            string prefabName = loc.m_prefabName;
            if (string.IsNullOrEmpty(prefabName)) prefabName = loc.m_prefab.Name;

            sb.AppendLine($"[({index++}) {loc.m_name}]");
            sb.AppendLine($"  预制件: {prefabName}");
            sb.AppendLine($"  启用: {loc.m_enable}");
            sb.AppendLine($"  群系: {loc.m_biome}");
            sb.AppendLine($"  群系区域: {loc.m_biomeArea}");
            sb.AppendLine($"  生成数量: {loc.m_quantity}");
            sb.AppendLine($"  优先处理: {loc.m_prioritized}  优先中心: {loc.m_centerFirst}  唯一: {loc.m_unique}");
            sb.AppendLine($"  组: {loc.m_group}  同类距离: {loc.m_minDistanceFromSimilar}  组上限: {loc.m_groupMax}  组内最大距离: {loc.m_maxDistanceFromSimilar}");
            sb.AppendLine($"  始终显示图标: {loc.m_iconAlways}  生成显示图标: {loc.m_iconPlaced}");
            sb.AppendLine($"  随机旋转: {loc.m_randomRotation}  坡度旋转: {loc.m_slopeRotation}  吸附水面: {loc.m_snapToWater}");
            sb.AppendLine($"  内部半径: {loc.m_interiorRadius}  外部半径: {loc.m_exteriorRadius}  清理区域: {loc.m_clearArea}");
            sb.AppendLine($"  地形偏差: {loc.m_minTerrainDelta} - {loc.m_maxTerrainDelta}");
            sb.AppendLine($"  植被掩码: {loc.m_minimumVegetation} - {loc.m_maximumVegetation}");
            sb.AppendLine($"  掩码检查: {loc.m_surroundCheckVegetation}  距离: {loc.m_surroundCheckDistance}  层数: {loc.m_surroundCheckLayers}  优于平均: {loc.m_surroundBetterThanAverage}");
            sb.AppendLine($"  森林内: {loc.m_inForest}  森林阈值: {loc.m_forestTresholdMin} - {loc.m_forestTresholdMax}");
            sb.AppendLine($"  距中心: {loc.m_minDistanceFromCenter} - {loc.m_maxDistanceFromCenter}");
            sb.AppendLine($"  距离: {loc.m_minDistance} - {loc.m_maxDistance}");
            sb.AppendLine($"  高度: {loc.m_minAltitude} - {loc.m_maxAltitude}");
            if (!string.IsNullOrEmpty(loc.m_altBiomeParent)) sb.AppendLine($"  自定义群系父级: {loc.m_altBiomeParent}");
            sb.AppendLine();
        }

        string path = Path.Combine(exportDir, "locations.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[GameDataExporter.ExportLocations] 导出 {ZoneSystem.instance.m_locations.Count} 个地点到 {path}");
    }

    /// <summary>注：导出植被生成配置（ZoneSystem.m_vegetation）</summary>
    private void ExportVegetation()
    {
        if (ZoneSystem.instance == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 植被配置（ZoneSystem.m_vegetation） ===");
        sb.AppendLine();

        int index = 0;
        foreach (var veg in ZoneSystem.instance.m_vegetation)
        {
            sb.AppendLine($"[({index++}) {veg.m_name}]");
            sb.AppendLine($"  预制件: {veg.m_prefab?.name ?? "无"}");
            sb.AppendLine($"  启用: {veg.m_enable}");
            sb.AppendLine($"  群系: {veg.m_biome}");
            sb.AppendLine($"  群系区域: {veg.m_biomeArea}");
            sb.AppendLine($"  数量: {veg.m_min} - {veg.m_max}  强制放置: {veg.m_forcePlacement}");
            sb.AppendLine($"  缩放: {veg.m_scaleMin} - {veg.m_scaleMax}");
            sb.AppendLine($"  随机倾斜: {veg.m_randTilt}  地形倾斜概率: {veg.m_chanceToUseGroundTilt}");
            sb.AppendLine($"  清理地面: {veg.m_blockCheck}  吸附固体: {veg.m_snapToStaticSolid}");
            sb.AppendLine($"  高度: {veg.m_minAltitude} - {veg.m_maxAltitude}");
            sb.AppendLine($"  掩码值: {veg.m_minVegetation} - {veg.m_maxVegetation}");
            sb.AppendLine($"  掩码检查: {veg.m_surroundCheckVegetation}  距离: {veg.m_surroundCheckDistance}  层数: {veg.m_surroundCheckLayers}  优于平均: {veg.m_surroundBetterThanAverage}");
            sb.AppendLine($"  海洋深度: {veg.m_minOceanDepth} - {veg.m_maxOceanDepth}");
            sb.AppendLine($"  地形倾斜: {veg.m_minTilt} - {veg.m_maxTilt}");
            sb.AppendLine($"  吸附水面: {veg.m_snapToWater}  地面偏移: {veg.m_groundOffset}");
            sb.AppendLine($"  群生: {veg.m_groupSizeMin}-{veg.m_groupSizeMax} (间距: {veg.m_groupRadius})");
            sb.AppendLine($"  森林内: {veg.m_inForest}  森林阈值: {veg.m_forestTresholdMin} - {veg.m_forestTresholdMax}");
            sb.AppendLine();
        }

        string path = Path.Combine(exportDir, "vegetation.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[GameDataExporter.ExportVegetation] 导出 {ZoneSystem.instance.m_vegetation.Count} 个植被到 {path}");
    }

    /// <summary>注：导出地面杂物配置（ClutterSystem.m_clutter）</summary>
    private void ExportClutter()
    {
        if (ClutterSystem.instance == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 地面杂物配置（ClutterSystem.m_clutter） ===");
        sb.AppendLine();

        int index = 0;
        foreach (var clutter in ClutterSystem.instance.m_clutter)
        {
            sb.AppendLine($"[({index++}) {clutter.m_name}]");
            sb.AppendLine($"  预制件: {clutter.m_prefab?.name ?? "无"}");
            sb.AppendLine($"  启用: {clutter.m_enabled}");
            sb.AppendLine($"  群系: {clutter.m_biome}");
            sb.AppendLine($"  GPU实例化: {clutter.m_instanced}");
            sb.AppendLine($"  数量: {clutter.m_amount}");
            sb.AppendLine($"  未清理地面: {clutter.m_onUncleared}  已清理地面: {clutter.m_onCleared}");
            sb.AppendLine($"  掩码值: {clutter.m_minVegetation} - {clutter.m_maxVegetation}");
            sb.AppendLine($"  缩放: {clutter.m_scaleMin} - {clutter.m_scaleMax}");
            sb.AppendLine($"  地形倾斜: {clutter.m_minTilt} - {clutter.m_maxTilt}");
            sb.AppendLine($"  高度: {clutter.m_minAlt} - {clutter.m_maxAlt}");
            sb.AppendLine($"  吸附水面: {clutter.m_snapToWater}  地形倾斜旋转: {clutter.m_terrainTilt}  随机偏移: {clutter.m_randomOffset}");
            sb.AppendLine($"  海洋深度: {clutter.m_minOceanDepth} - {clutter.m_maxOceanDepth}");
            sb.AppendLine($"  森林内: {clutter.m_inForest}  森林阈值: {clutter.m_forestTresholdMin} - {clutter.m_forestTresholdMax}");
            sb.AppendLine($"  分形: 缩放{clutter.m_fractalScale} 偏移{clutter.m_fractalOffset} 阈值{clutter.m_fractalTresholdMin}-{clutter.m_fractalTresholdMax}");
            sb.AppendLine();
        }

        string path = Path.Combine(exportDir, "clutter.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[GameDataExporter.ExportClutter] 导出 {ClutterSystem.instance.m_clutter.Count} 个杂物到 {path}");
    }







    /// <summary>注：导出怪物生成配置</summary>
    private void ExportMonsterSpawn()
    {
        if (SpawnSystem.m_instances == null || SpawnSystem.m_instances.Count == 0) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========= 怪物生成配置（SpawnSystem） =========");
        sb.AppendLine();

        int index = 0;
        foreach (var spawnList in SpawnSystem.m_instances[0].m_spawnLists)
        {
            if (spawnList.m_spawners.Count == 0) continue;
            foreach (var spawnData in spawnList.m_spawners)
            {
                sb.Append(FormatSpawnData(spawnData, index++));
            }
        }

        string path = Path.Combine(exportDir, "monster_spawn.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[GameDataExporter.ExportMonsterSpawn] 导出 {index} 个怪物到 {path}");
    }

    /// <summary>注：SpawnData 格式化成可读文本</summary>
    private string FormatSpawnData(SpawnSystem.SpawnData spawnData, int index)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[({index}) {spawnData.m_name}]");
        sb.AppendLine($"  预制件: {spawnData.m_prefab.name}");
        sb.AppendLine($"  启用: {spawnData.m_enabled}");
        sb.AppendLine($"  群系: {spawnData.m_biome}");
        sb.AppendLine($"  群系区域: {spawnData.m_biomeArea}");
        sb.AppendLine($"  最大数量: {spawnData.m_maxSpawned}");
        sb.AppendLine($"  生成间隔: {spawnData.m_spawnInterval}秒");
        sb.AppendLine($"  生成概率: {spawnData.m_spawnChance}%");
        sb.AppendLine($"  生成半径: {spawnData.m_spawnRadiusMin}-{spawnData.m_spawnRadiusMax}");
        sb.AppendLine($"  白天: {spawnData.m_spawnAtDay}  夜晚: {spawnData.m_spawnAtNight}");
        sb.AppendLine($"  高度: {spawnData.m_minAltitude} - {spawnData.m_maxAltitude}");
        sb.AppendLine($"  森林内: {spawnData.m_inForest}  森林外: {spawnData.m_outsideForest}");
        sb.AppendLine($"  等级: {spawnData.m_minLevel} - {spawnData.m_maxLevel}");
        sb.AppendLine($"  最小距离: {spawnData.m_spawnDistance}");
        sb.AppendLine($"  群生: {spawnData.m_groupSizeMin}-{spawnData.m_groupSizeMax} (半径: {spawnData.m_groupRadius})");
        sb.AppendLine($"  地形倾斜: {spawnData.m_minTilt} - {spawnData.m_maxTilt}");
        sb.AppendLine($"  岩浆内: {spawnData.m_inLava}  岩浆外: {spawnData.m_outsideLava}");
        sb.AppendLine($"  靠近玩家: {spawnData.m_canSpawnCloseToPlayer}  基地内: {spawnData.m_insidePlayerBase}");
        sb.AppendLine($"  海洋深度: {spawnData.m_minOceanDepth} - {spawnData.m_maxOceanDepth}");
        sb.AppendLine($"  追猎玩家: {spawnData.m_huntPlayer}");
        sb.AppendLine($"  地面偏移: {spawnData.m_groundOffset} +{spawnData.m_groundOffsetRandom}");
        sb.AppendLine($"  距中心: {spawnData.m_minDistanceFromCenter} - {spawnData.m_maxDistanceFromCenter}");
        sb.AppendLine($"  升级概率: {spawnData.m_overrideLevelupChance}");

        if (!string.IsNullOrEmpty(spawnData.m_requiredGlobalKey)) sb.AppendLine($"  需要全局Key: {spawnData.m_requiredGlobalKey}");

        if (spawnData.m_requiredEnvironments.Count > 0) sb.AppendLine($"  需要环境: {string.Join(", ", spawnData.m_requiredEnvironments)}");

        sb.AppendLine();
        return sb.ToString();
    }

    private class GameDataExporterPatch
    {
        /// <summary>注：玩家生成后导出游戏配置，只跑一次</summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix, HarmonyPriority(0)]
        static void Player_OnSpawned_Postfix() => Instance.Export();
    }
}


