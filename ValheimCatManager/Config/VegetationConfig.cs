using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.CatUtils;
using ValheimCatManager.Config;
using static ZoneSystem;

namespace ValheimCatManager.Config;
/// <summary>注：植被配置类，对应游戏原生 ZoneVegetation 的各配置字段</summary>
public class VegetationConfig
{

    public VegetationConfig(string name)
    {
        if (!string.IsNullOrEmpty(name)) 预制件 = name;
    }




    public string 预制件 { get; set; } = string.Empty; // 注：植被预制件名（必填项）
    public string 名字 { get; set; } = "veg"; // 注：ZoneVegetation 的名字
    public bool 启用 { get; set; } = true; // 注：是否启用（默认值：true）
    public float 最小_数量 { get; set; } = 1; // 注：每区域（64m×64m）放置的最小数量（默认值：1）
    public float 最大_数量 { get; set; } = 1f; // 注：每区域（64m×64m）放置的最大数量（默认值：1）
    public bool 强制放置 { get; set; } = false; // 注：启用时对每种植被进行 50 次放置尝试（默认值：false）
    public float 最小_缩放 { get; set; } = 1f; // 注：最小缩放（默认值：1）
    public float 最大_缩放 { get; set; } = 1f; // 注：最大缩放（默认值：1）
    public float 随机倾斜 { get; set; } = 0f; // 注：随机旋转角度范围（默认值：0）
    public float 地面倾斜旋转概率 { get; set; } = 0f; // 注：按地形角度设置旋转的概率，0.0~1.0（默认值：0）
    public string 生态区域 { get; set; } = "None"; // 注：生态区域（必填项，默认值：None）
    public Heightmap.BiomeArea 区域范围 { get; set; } = Heightmap.BiomeArea.Everything; // 注：区域范围（默认值：Everything）
    public bool 清理地面 = true; // 注：障碍物检查，启用时需要平整地面（默认值：true）
    public bool 吸附固体 { get; set; } = false; // 注：吸附到静态固体顶部而非地形（默认值：false）
    public float 最低_需求高度 { get; set; } = -1000f; // 注：最低生成高度（默认值：-1000）
    public float 最高_需求高度 { get; set; } = 1000f; // 注：最高生成高度（默认值：1000）
    public float 最小掩码值 { get; set; } = 0f; // 注：最小植被掩码值（默认值：0）
    public float 最大掩码值 { get; set; } = 0f; // 注：最大植被掩码值（默认值：0）
    public bool 掩码检查 { get; set; } = false; // 注：周围植被掩码检查，置于较高掩码附近（默认值：false）
    public float 掩码检查距离 { get; set; } = 20f; // 注：植被掩码检查距离（默认值：20）
    public int 掩码检查层数 { get; set; } = 2; // 注：掩码检查层数，每层采样 6 个点（默认值：2）
    public float 掩码优于平均值 { get; set; } = 0f; // 注：平均掩码向最高掩码的调整值，需小于 1.0（默认值：0）
    public float 海洋深度小 { get; set; } = 0f; // 注：最小海洋深度（默认值：0）
    public float 海洋深度大 { get; set; } = 0f; // 注：最大海洋深度（默认值：0）
    public float 最小倾斜 { get; set; } = 0f; // 注：生成所需最小地形角度，0~90（默认值：0）
    public float 最大倾斜 { get; set; } = 35f; // 注：生成所需最大地形角度，0~90（默认值：35）
    public bool 吸附水面 { get; set; } = false; // 注：启用时放置在水位处而非地形表面（默认值：false）
    public float 地面偏移 { get; set; } = 0f; // 注：在地面上方生成的偏移量（默认值：0）
    public int 组最小 { get; set; } = 1; // 注：组合生成的最小数量（默认值：1）
    public int 组最大 { get; set; } = 1; // 注：组合生成的最大数量（默认值：1）
    public float 组间距 { get; set; } = 3f; // 注：组合生成的间距（默认值：3）
    public bool 森林内生成 { get; set; } = false; // 注：是否仅在森林中生成（默认值：false）
    public float 森林最小阈值 { get; set; } = 0f; // 注：最小森林值（若仅在森林中生成）
    public float 森林最大阈值 { get; set; } = 0f; // 注：最大森林值（若仅在森林中生成）

    /// <summary>注：根据配置生成游戏原生 ZoneVegetation 实例</summary>
    public ZoneSystem.ZoneVegetation GetZoneVegetation()
    {
        // 注：预制件名为空时视为无效配置
        if (string.IsNullOrEmpty(预制件))
        {
            Debug.LogError("[VegetationConfig.GetZoneVegetation]：植被生成传入了 【空字符串】");
            return null;
        }

        // 注：按名称查找预制件，找不到时视为无效配置
        if (CatTool.GetGameObject(预制件) is not GameObject prefab) return null;

        // 注：以"Veg_预制件名"作为 ZoneVegetation 的名字
        名字 = $"Veg_{prefab.name}";

        // 注：按生态区域名匹配生物群系枚举
        var biome = CatTool.GetBiome(生态区域);

        // 注：按配置字段生成 ZoneVegetation 实例
        return new ZoneSystem.ZoneVegetation
        {
            m_name = 名字,
            m_prefab = prefab,
            m_enable = 启用,
            m_min = 最小_数量,
            m_max = 最小_数量 > 最大_数量 ? 最小_数量 : 最大_数量,
            m_forcePlacement = 强制放置,
            m_scaleMin = 最小_缩放,
            m_scaleMax = 最小_缩放 > 最大_缩放 ? 最小_缩放 : 最大_缩放,
            m_randTilt = 随机倾斜,
            m_chanceToUseGroundTilt = 地面倾斜旋转概率,
            m_biome = biome,
            m_biomeArea = 区域范围,
            m_blockCheck = 清理地面,
            m_snapToStaticSolid = 吸附固体,
            m_minAltitude = 最低_需求高度,
            m_maxAltitude = 最低_需求高度 > 最高_需求高度 ? 最低_需求高度 : 最高_需求高度,
            m_minVegetation = 最小掩码值,
            m_maxVegetation = 最大掩码值,
            m_surroundCheckVegetation = 掩码检查,
            m_surroundCheckDistance = 掩码检查距离,
            m_surroundCheckLayers = 掩码检查层数,
            m_surroundBetterThanAverage = 掩码优于平均值,
            m_minOceanDepth = 海洋深度小,
            m_maxOceanDepth = 海洋深度小 > 海洋深度大 ? 海洋深度小 : 海洋深度大,
            m_minTilt = 最小倾斜,
            m_maxTilt = 最小倾斜 > 最大倾斜 ? 最小倾斜 : 最大倾斜,
            m_snapToWater = 吸附水面,
            m_groundOffset = 地面偏移,
            m_groupSizeMin = 组最小,
            m_groupSizeMax = 组最小 > 组最大 ? 组最小 : 组最大,
            m_groupRadius = 组间距,
            m_inForest = 森林内生成,
            m_forestTresholdMin = 森林最小阈值,
            m_forestTresholdMax = 森林最小阈值 > 森林最大阈值 ? 森林最小阈值 : 森林最大阈值
        };
    }
}







