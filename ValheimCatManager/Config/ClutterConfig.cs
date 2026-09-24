using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.CatUtils;

namespace ValheimCatManager.Config;

/// <summary>注：地面杂物配置类，对应游戏原生 ClutterSystem.Clutter 的各配置字段（草、碎石、地面雾气等 GPU 贴片）</summary>
public class ClutterConfig
{
    public ClutterConfig(string prefab)
    {
        if (!string.IsNullOrEmpty(prefab)) Prefab = prefab;
    }

    /// <summary>注：杂物预制件名（必填项）</summary>
    public string Prefab { get; set; } = string.Empty;

    /// <summary>注：Clutter 的名字，留空时自动用 Clutter_预制件名</summary>
    public string Name { get; set; } = "";

    /// <summary>注：是否启用（默认值：true）</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>注：生态区域名，用 CatTool.GetBiome 解析（必填项，默认值：None）</summary>
    public string BiomeName { get; set; } = "None";

    /// <summary>注：是否用 GPU Instancing 批量渲染（默认值：false）</summary>
    public bool Instanced { get; set; } = false;

    /// <summary>注：每区块生成数量（默认值：80）</summary>
    public int Amount { get; set; } = 80;

    /// <summary>注：在未平整的自然地面生成（默认值：true）</summary>
    public bool OnUncleared { get; set; } = true;

    /// <summary>注：在玩家平整过的地面生成（默认值：false）</summary>
    public bool OnCleared { get; set; } = false;

    /// <summary>注：最小植被掩码值（默认值：0）</summary>
    public float MinVegetation { get; set; } = 0f;

    /// <summary>注：最大植被掩码值（默认值：0）</summary>
    public float MaxVegetation { get; set; } = 0f;

    /// <summary>注：最小缩放（默认值：1）</summary>
    public float ScaleMin { get; set; } = 1f;

    /// <summary>注：最大缩放（默认值：1）</summary>
    public float ScaleMax { get; set; } = 1f;

    /// <summary>注：生成所需最小地形角度，0~90（默认值：0）</summary>
    public float MinTilt { get; set; } = 0f;

    /// <summary>注：生成所需最大地形角度，0~90（默认值：18）</summary>
    public float MaxTilt { get; set; } = 18f;

    /// <summary>注：最低生成高度（默认值：27）</summary>
    public float MinAlt { get; set; } = 27f;

    /// <summary>注：最高生成高度（默认值：1000）</summary>
    public float MaxAlt { get; set; } = 1000f;

    /// <summary>注：放置在水位处而非地形表面（默认值：false）</summary>
    public bool SnapToWater { get; set; } = false;

    /// <summary>注：按地形角度旋转贴片（默认值：false）</summary>
    public bool TerrainTilt { get; set; } = false;

    /// <summary>注：位置随机偏移量（默认值：0）</summary>
    public float RandomOffset { get; set; } = 0f;

    /// <summary>注：最小海洋深度（默认值：0）</summary>
    public float MinOceanDepth { get; set; } = 0f;

    /// <summary>注：最大海洋深度（默认值：0）</summary>
    public float MaxOceanDepth { get; set; } = 0f;

    /// <summary>注：是否仅在森林中生成（默认值：false）</summary>
    public bool InForest { get; set; } = false;

    /// <summary>注：最小森林值（若仅在森林中生成，默认值：0）</summary>
    public float ForestThresholdMin { get; set; } = 0f;

    /// <summary>注：最大森林值（默认值：1）</summary>
    public float ForestThresholdMax { get; set; } = 1f;

    /// <summary>注：分形放置缩放，>0 才启用分形（默认值：0=关闭）</summary>
    public float FractalScale { get; set; } = 0f;

    /// <summary>注：分形噪声偏移（默认值：0）</summary>
    public float FractalOffset { get; set; } = 0f;

    /// <summary>注：分形放置最小阈值（默认值：0.5）</summary>
    public float FractalThresholdMin { get; set; } = 0.5f;

    /// <summary>注：分形放置最大阈值（默认值：1）</summary>
    public float FractalThresholdMax { get; set; } = 1f;

    /// <summary>注：根据配置生成游戏原生 Clutter 实例</summary>
    public ClutterSystem.Clutter GetClutter()
    {
        if (string.IsNullOrEmpty(Prefab))
        {
            Debug.LogError("[ClutterConfig.GetClutter]：杂物生成传入了【空字符串】");
            return null;
        }

        if (CatTool.GetGameObject(Prefab) is not GameObject prefab) return null;

        if (string.IsNullOrEmpty(Name)) Name = $"Clutter_{prefab.name}";

        var biome = CatTool.GetBiome(BiomeName);


        return new ClutterSystem.Clutter
        {
            m_name = Name,
            m_enabled = Enabled,
            m_biome = biome,
            m_instanced = Instanced,
            m_prefab = prefab,
            m_amount = Amount,
            m_onUncleared = OnUncleared,
            m_onCleared = OnCleared,
            m_minVegetation = MinVegetation,
            m_maxVegetation = MaxVegetation,
            m_scaleMin = ScaleMin,
            m_scaleMax = ScaleMin > ScaleMax ? ScaleMin : ScaleMax,
            m_minTilt = MinTilt,
            m_maxTilt = MinTilt > MaxTilt ? MinTilt : MaxTilt,
            m_minAlt = MinAlt,
            m_maxAlt = MinAlt > MaxAlt ? MinAlt : MaxAlt,
            m_snapToWater = SnapToWater,
            m_terrainTilt = TerrainTilt,
            m_randomOffset = RandomOffset,
            m_minOceanDepth = MinOceanDepth,
            m_maxOceanDepth = MinOceanDepth > MaxOceanDepth ? MinOceanDepth : MaxOceanDepth,
            m_inForest = InForest,
            m_forestTresholdMin = ForestThresholdMin,
            m_forestTresholdMax = ForestThresholdMin > ForestThresholdMax ? ForestThresholdMin : ForestThresholdMax,
            m_fractalScale = FractalScale,
            m_fractalOffset = FractalOffset,
            m_fractalTresholdMin = FractalThresholdMin,
            m_fractalTresholdMax = FractalThresholdMin > FractalThresholdMax ? FractalThresholdMin : FractalThresholdMax
        };
    }
}

