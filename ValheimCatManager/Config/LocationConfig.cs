using SoftReferenceableAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Tool;
using static ZoneSystem;

namespace ValheimCatManager.Config
{
    public class LocationConfig
    {


        /// <summary>
        /// 注：位置的预制件名(必填项)
        /// </summary>
        public GameObject 预制件 { get; set; }

        /// <summary>
        /// 注：指的是 ZoneLocation 的名字
        /// </summary>
        private string 名字 { get; set; } = string.Empty;

        /// <summary>
        /// 注：是否启用
        /// <br>(默认值：true)</br>
        /// </summary>
        public bool 启用 { get; set; } = true;



        /// <summary>
        /// 注：生态区域(必填项) <br>(默认值：None)</br>
        /// </summary>
        public string 生态区域 { get; set; } = "None";

        /// <summary>
        /// 注：区域范围<br>(默认值：Everything)</br>
        /// </summary>
        public Heightmap.BiomeArea 区域范围 { get; set; } = Heightmap.BiomeArea.Everything;

        /// <summary>
        /// 注：生成数量<br>(默认值：1)</br>
        /// </summary>
        public int 最大数量 { get; set; } = 1;

        /// <summary>
        /// 注：首先生成，尝试次数更多<br>(默认值：false)</br>
        /// </summary>
        public bool 优先处理 { get; set; } = false;

        /// <summary>
        /// 注：优先尝试在世界中心生成，并逐渐向世界边缘移动
        /// </summary>
        public bool 优先中心 { get; set; }

        /// <summary>
        /// 注：保证只有一个实例。
        /// </summary>
        public bool 唯一 { get; set; }

        /// <summary>
        /// 注：用于minDistanceFromSimilar的组名称
        /// <br>(默认值：空字符串)</br>
        /// </summary>
        public string 组 { get; set; } = "";

        /// <summary>
        /// 注：相同位置之间或指定组内位置之间的最小距离。
        /// <br>(默认值：0)</br>
        /// </summary>
        public float 同类最小距离 { get; set; } = 0;


        /// <summary>
        /// 注：始终显示的位置图标。
        /// </summary>
        public bool 始终显示图标 { get; set; }

        /// <summary>
        /// 注：位置生成时显示的图标
        /// </summary>
        public bool 生成显示图标 { get; set; }

        /// <summary>
        /// 注：随机旋转位置（不受世界种子影响）。
        /// </summary>
        public bool 随机旋转 { get; set; } 

        /// <summary>
        /// 注：根据地形角度旋转。例如适用于山坡上的位置。
        /// </summary>
        public bool 坡度旋转 { get; set; }

        /// <summary>
        /// 注：放置在水位处而非地形上。
        /// </summary>
        public bool 吸附水面 { get; set; }

        /// <summary>
        /// 注：与位置相连的内部半径
        /// </summary>
        public float 内部半径 { get; set; }

        /// <summary>
        /// 注：最大建议值为 32 米。较高的值会超出区域边界，并可能导致问题。
        /// <br>(默认值：10f)</br>
        /// </summary>
        public float 外部半径 { get; set; } = 10f;

        /// <summary>
        /// 注：是否清理区域
        /// </summary>
        public bool 清理区域 { get; set; }

        /// <summary>
        /// 注：最小地形偏差
        /// </summary>
        public float 最小地形偏差 { get; set; }

        /// <summary>
        /// 注：最大地形偏差
        /// <br>(默认值：2f)</br>
        /// </summary>
        public float 最大地形偏差 { get; set; } = 2f;

        /// <summary>
        /// 注：是否在森林中分形（0-1为森林内部）
        /// <br>(默认值：false)</br>
        /// </summary>
        public bool 森林内生成 { get; set; } = false;

        /// <summary>
        /// 注：森林阈值最小值
        /// </summary>
        public float 森林最小阈值 { get; set; }

        /// <summary>
        /// 注：森林阈值最大值
        /// <br>(默认值：1f)</br>
        /// </summary>
        public float 森林最大阈值 { get; set; } = 1f;

        /// <summary>
        /// 注：距世界中心的最小距离
        /// </summary>
        public float 最小距离 { get; set; }

        /// <summary>
        /// 注：距世界中心的最大距离
        /// </summary>
        public float 最大距离 { get; set; }

        /// <summary>
        /// 注：最低高度
        /// <br>(默认值：-1000f)</br>
        /// </summary>
        public float 最低高度 { get; set; } = -1000f;

        /// <summary>
        /// 注：最高高度
        /// <br>(默认值：1000f)</br>
        /// </summary>
        public float 最高高度 { get; set; } = 1000f;

        /// <summary>
        /// 转换为ZoneLocation对象
        /// </summary>
        /// <returns>生成的ZoneLocation实例</returns>
        public ZoneLocation GetZoneLocation()
        {

            if (!预制件)
            {
                Debug.LogError($"执行GetZoneLocation时，预制件是空！");
                return null;
            }


            var biome = CatToolManager.GetBiome(生态区域);
            if (biome == Heightmap.Biome.None) return null;

            return new ZoneLocation
            {
                m_name = 预制件.name,
                m_enable = 启用,
                m_biome = biome,
                m_biomeArea = 区域范围,
                m_quantity = 最大数量,
                m_prioritized = 优先处理,
                m_centerFirst = 优先中心,
                m_unique = 唯一,
                m_group = 组,
                m_minDistanceFromSimilar = 同类最小距离,
                m_iconAlways = 始终显示图标,
                m_iconPlaced = 生成显示图标,
                m_randomRotation = 随机旋转,
                m_slopeRotation = 坡度旋转,
                m_snapToWater = 吸附水面,
                m_interiorRadius = 内部半径,
                m_exteriorRadius = 外部半径,
                m_clearArea = 清理区域,
                m_minTerrainDelta = 最小地形偏差,
                m_maxTerrainDelta = 最大地形偏差,
                m_inForest = 森林内生成,
                m_forestTresholdMin = 森林最小阈值,
                m_forestTresholdMax = 森林最大阈值,
                m_minDistance = 最小距离,
                m_maxDistance = 最大距离,
                m_minAltitude = 最低高度,
                m_maxAltitude = 最高高度
            };
        }
    }

}
