using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Tool;
using static ZoneSystem;

namespace ValheimCatManager.Data
{
    public class VegetationConfig
    {

        /// <summary>
        /// 构造函数 传入植被预制件的名字
        /// </summary>
        /// <param name="name"></param>
        public VegetationConfig(string name)
        {
            if (!string.IsNullOrEmpty(name)) 预制件 = name;

        }


        /// <summary>
        /// 注：植被的预制件名(必填项)
        /// </summary>
        public string 预制件 { get; set; } = string.Empty;

        /// <summary>
        /// 注：指的是 ZoneVegetation 的名字
        /// </summary>
        public string 名字 { get; set; } = "veg";

        /// <summary>
        /// 注：是否启用<br>(默认值：true)</br>
        /// </summary>
        public bool 启用 { get; set; } = true;

        /// <summary>
        /// 注：每个区域（64m x 64m）要放置的最小数量。<br>(默认值：1)</br>
        /// </summary>
        public float 最小_数量 { get; set; } = 1;

        /// <summary>
        /// 注：每个区域（64m x 64m）要放置的最大数量。<br>(默认值：1)</br>
        /// </summary>
        public float 最大_数量 { get; set; } = 1f;

        /// <summary>
        /// 注：仅尝试一次为每种植被找到合适的位置。如果启用，则对每个植被进行 50 次尝试<br>(默认值：false)</br>
        /// </summary>
        public bool 强制放置 { get; set; } = false;

        /// <summary>
        /// 注：最小缩放<br>(默认值：1)</br>
        /// </summary>
        public float 最小_缩放 { get; set; } = 1f;

        /// <summary>
        /// 注：最大缩放<br>(默认值：1)</br>
        /// </summary>
        public float 最大_缩放 { get; set; } = 1f;

        /// <summary>
        /// 注：在指定度数范围内的随机旋转。<br>(默认值：0)</br>
        /// </summary>
        public float 随机倾斜 { get; set; } = 0f;

        /// <summary>
        /// 注：地面倾斜旋转概率（默认值：0.0）：基于地形角度设置旋转的概率（范围 0.0 到 1.0）。
        /// </summary>
        public float 地面倾斜旋转概率 { get; set; } = 0f;

        /// <summary>
        /// 注：生态区域(必填项) <br>(默认值：None)</br>
        /// </summary>
        public string 生态区域 { get; set; } = "None";

        /// <summary>
        /// 注：区域范围<br>(默认值：Everything)</br>
        /// </summary>
        public Heightmap.BiomeArea 区域范围 { get; set; } = Heightmap.BiomeArea.Everything;


        /// <summary>
        /// 注：障碍物检查 若启用，需要平整的地面。<br>(默认值：true)</br>
        /// </summary>
        public bool 清理地面 = true;


        /// <summary>
        /// 注：吸附到静态固体若启用，会放置在固体物体顶部而非地形上。<br>(默认值：false)</br>
        /// </summary>
        public bool 吸附固体 { get; set; } = false;


        /// <summary>
        /// 注：最低高度<br>(默认值：-1000)</br>
        /// </summary>
        public float 最低_需求高度 { get; set; } = -1000f;
        /// <summary>
        /// 注：最高高度<br>(默认值：1000)</br>
        /// </summary>
        public float 最高_需求高度 { get; set; } = 1000f;

        /// <summary>
        /// 最小植被掩码值。<br>(默认值：0)</br>
        /// </summary>
        public float 最小掩码值 { get; set; } = 0f;

        /// <summary>
        /// 最大植被掩码值。<br>(默认值：0)</br>
        /// </summary>
        public float 最大掩码值 { get; set; } = 0f;



        /// <summary>
        /// 周围植被掩码检查（默认值：false）：若启用，位置会放置在较高植被掩码的附近。<br></br>
        /// 此检查会在整个区域内采样，且至少需要 10 个样本，因此前 10 次生成尝试总会失败。<br></br>
        /// 建议在需要更多生成尝试时使用。优先规则：若周围植被掩码高于平均掩码，则放置该位置。<br>(默认值：false)</br>
        /// </summary>
        public bool 掩码检查 { get; set; } = false;

        /// <summary>
        /// 植被掩码检查的距离 <br>(默认值：20)</br>
        /// </summary>
        public float 掩码检查距离 { get; set; } = 20f;

        /// <summary>
        /// 检查的点数量，距离会被划分为环形层，每层检查生成点周围的 6 个点。<br>(默认值：2)</br>
        /// </summary>
        public int 掩码检查层数 { get; set; }= 2;


        /// <summary>
        /// 此参数将要求从平均掩码调整到最高掩码。<br></br>
        /// 该值必须小于 1.0，因为掩码不能高于最高掩码。<br></br>
        /// 也可以使用负值。<br>(默认值：0)</br>
        /// </summary>
        public float 掩码优于平均值 { get; set; } = 0f;

        /// <summary>
        /// 最小海洋深度<br>(默认值：0)</br>
        /// </summary>
        public float 海洋深度小 { get; set; } = 0f;

        /// <summary>
        /// 最大海洋深度<br>(默认值：0)</br>
        /// </summary>
        public float 海洋深度大 { get; set; } = 0f;

        /// <summary>
        /// 注：生成所需的最小地形角度，范围为 0 到 90<br>(默认值：0)</br>
        /// </summary>
        public float 最小倾斜 { get; set; } = 0f;

        /// <summary>
        /// 注：生成所需的最小地形角度，范围为 0 到 90 <br>(默认值：35)</br>
        /// </summary>
        public float 最大倾斜 { get; set; } = 35f;

        /// <summary>
        /// 最小地形高度变化量<br>(默认值：0)</br>
        /// </summary>
        public float 地形高度变化半径 { get; set; } = 0f;

        /// <summary>
        /// 值越低，植被越倾向于生成在平坦区域。。<br>(默认值：10)</br>
        /// </summary>
        public float 最大_地形高度变化 { get; set; } = 10f;

        /// <summary>
        /// 值越高，植被越倾向于生成在斜坡区域。<br>(默认值：0)</br>
        /// </summary>
        public float 最小_地形高度变化 { get; set; } = 0f;

        /// <summary>
        /// 若启用，植被会放置在水位处而非地形表面。 <br>(默认值：false)</br>
        /// </summary>
        public bool 吸附水面 { get; set; } = false;

        /// <summary>
        /// 注：在地面上方生成 <br>(默认值：0.5)</br>
        /// </summary>
        public float 地面偏移 { get; set; } = 0.5f;

        /// <summary>
        /// 注：组合生成的最小数量<br>(默认值：1)</br>
        /// </summary>
        public int 组最小 { get; set; } = 1;

        /// <summary>
        /// 注：组合生成的最大数量<br>(默认值：1)</br>
        /// </summary>
        public int 组最大 { get; set; } = 1;

        /// <summary>
        /// 注：组合生成的最大数量<br>(默认值：3)</br>
        /// </summary>
        public float 组间距 { get; set; } = 3f;


        /// <summary>
        /// 注：是否可在森林里生成<br>(默认值：true)</br>
        /// </summary>
        public bool 森林内生成 { get; set; } = true;

        /// <summary>
        /// 最小森林值（若仅在森林中生成）
        /// </summary>
        public float 森林最小值阈值 { get; set; } = 0f;

        /// <summary>
        /// 最大森林值（若仅在森林中生成）
        /// </summary>
        public float 森林最大阈值 { get; set; } = 0f;


        //public bool m_foldout;



        public ZoneVegetation GetZoneVegetation()
        {

            if (string.IsNullOrEmpty(预制件))
            {
                Debug.LogError("植被生成传入了 【空字符串】");
                return null;
            }

            var prefab = CatToolManager.GetGameObject(预制件);
            if (!prefab) return null;

            var biome = CatToolManager.GetBiome(生态区域);
            if (biome == Heightmap.Biome.None) return null;

            return new ZoneVegetation
            {
                m_name = prefab.name,
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
                m_terrainDeltaRadius = 地形高度变化半径,
                m_minTerrainDelta = 最小_地形高度变化,
                m_maxTerrainDelta = 最小_地形高度变化 > 最大_地形高度变化 ? 最小_地形高度变化 : 最大_地形高度变化,
                m_snapToWater = 吸附水面,
                m_groundOffset = 地面偏移,
                m_groupSizeMin = 组最小,
                m_groupSizeMax = 组最小 > 组最大 ? 组最小 : 组最大,
                m_groupRadius = 组间距,
                m_inForest = 森林内生成,
                m_forestTresholdMin = 森林最小值阈值,
                m_forestTresholdMax = 森林最小值阈值 > 森林最大阈值 ? 森林最小值阈值 : 森林最大阈值
            };
        }



















    }


   


}
