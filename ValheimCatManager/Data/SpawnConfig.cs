using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Data
{

    public class SpawnConfig
    {
        /// <summary>
        /// 注：构造函数！名字一定要填！必填项！
        /// </summary>
        /// <param name="Name"></param>
        public SpawnConfig(string Name)
        {

            if (!string.IsNullOrEmpty(Name)) 预制件 = Name;



        }

        /// <summary>
        /// 注：这个是生成的名字，不是怪物的名字
        /// </summary>
        public string 名字 { get; set; } = string.Empty;


        /// <summary>
        /// 注：怪物的实例(必填项)
        /// </summary>
        /// 
        public string 预制件 { get; set; } = string.Empty;


        /// <summary>
        /// 注：是否启用生成<br>(默认值：true)</br>
        /// </summary>
        public bool 启用 { get; set; } = true;


        /// <summary>
        /// 注：生态区域(必填项) <br>(默认值：None)</br>
        /// </summary>
        public string 生态区域 { get; set; } =  "None";


        /// <summary>
        /// 注：区域范围<br>(默认值：Everything)</br>
        /// </summary>
        public Heightmap.BiomeArea 区域范围 { get; set; } = Heightmap.BiomeArea.Everything;


        /// <summary>
        /// 注：生物可同时 存在的实例数量。<br></br>同时也是随时间累积的生成尝试次数.
        /// <br>(默认值：1)</br>
        /// </summary>
        public int 最大生成量 { get; set; } = 1;


        /// <summary>
        /// 注：生物的生成间隔。<br>(默认值：100)</br>
        /// </summary>
        public float 生成_间隔 { get; set; } = 100f;

        /// <summary>
        /// 注：生成的概率<br>(默认值：50)</br>
        /// </summary>
        public float 生成_概率 { get; set; } = 50f;

        /// <summary>
        /// 注：与同实例的生成距离，不是对玩家的生成距离<br>(默认值：10)</br>
        /// </summary>
        public float 生成_距离 { get; set; } = 10f;

        /// <summary>
        /// 注：与玩家的最小距离。<br>(默认值：40)</br>
        /// </summary>
        public float 最小玩家间距 { get; set; }

        /// <summary>
        /// 注：与玩家的最大距离。<br>(默认值：80)</br>
        /// </summary>
        public float 最大玩家间距 { get; set; }



        /// <summary>
        /// 注：最低生物等级<br>(默认值：1)</br>
        /// </summary>
        public int 生物_最低等级 { get; set; } = 1;

        /// <summary>
        /// 注：最高生物等级<br>(默认值：1)</br>
        /// </summary>
        public int 生物_最高等级 { get; set; } = 1;


        /// <summary>
        /// 注：生成所需世界条件<br>(默认值：1)</br>
        /// </summary>
        public string 需求_条件 { get; set; }


        /// <summary>
        /// 注：生成所需的天气<br>(默认值：1)</br>
        /// </summary>
        public List<string> 需求_天气 { get; set; } = new List<string>();



        /// <summary>
        /// 注：组合生成的最小数量<br/>例：一支怪物小队，这个小队最小多少怪<br>(默认值：1)</br>
        /// </summary>
        public int 组最小 { get; set; } = 1;

        /// <summary>
        /// 注：组合生成的最大数量<br/>例：一支怪物小队，这个小队最大多少怪<br>(默认值：1)</br>
        /// </summary>
        public int 组最大 { get; set; } = 1;



        /// <summary>
        /// 例：一支怪物小队，小队里的每个怪物，距离间隔<br>(默认值：3)</br>
        /// </summary>
        public float 组间距 { get; set; } = 3f;



        /// <summary>
        /// 注：白天生成<br>(默认值：true)</br>
        /// </summary>
        public bool 白天生成 { get; set; } = true;

        /// <summary>
        /// 注：晚上生成<br>(默认值：true)</br>
        /// </summary>
        public bool 晚上生成 { get; set; } = true;

        /// <summary>
        /// 注：生成所需的最大高度<br>(默认值：1000)</br>
        /// </summary>
        public float 最大高度 { get; set; } = 1000f;

        /// <summary>
        /// 注：生成所需的最小高度<br>(默认值：-1000)</br>
        /// </summary>
        public float 最小高度 { get; set; } = -1000f;

        /// <summary>
        /// 注：生成所需的最小地形角度，范围为 0 到 90<br>(默认值：0)</br>
        /// </summary>
        public float 最小倾斜 { get; set; } = 0;

        /// <summary>
        /// 注：生成所需的最小地形角度，范围为 0 到 90 <br>(默认值：35)</br>
        /// </summary>
        public float 最大倾斜 { get; set; } = 35f;

        /// <summary>
        /// 注：生成所需的最小海洋深度<br>(默认值：0)</br>
        /// </summary>
        public float 最小海洋深度 { get; set; } = 0f;

        /// <summary>
        /// 注：生成所需的最大海洋深度<br>(默认值：0)</br>
        /// </summary>
        public float 最大海洋深度 { get; set; } = 0f;

        /// <summary>
        /// 注：是否可在森林里生成<br>(默认值：true)</br>
        /// </summary>
        public bool 森林内生成 { get; set; } = true;

        /// <summary>
        /// 注：是否可在森林外生成<br>(默认值：true)</br>
        /// </summary>
        public bool 森林外生成 { get; set; } = true;

        /// <summary>
        /// 注：怪物追杀玩家<br>(默认值：false)</br>
        /// </summary>
        public bool 追杀玩家 { get; set; } = false;

        /// <summary>
        /// 注：在地面上方生成<br>(默认值：0.5)</br>
        /// </summary>
        public float 地面偏移 { get; set; } = 0.5f;


        public SpawnSystem.SpawnData GetSpawnData()
        {
            if (string.IsNullOrEmpty(预制件))
            {
                Debug.LogError("预制件生成传入了 【空字符串】");
                return null;
            }
            var prefab = CatToolManager.GetGameObject(预制件);
            if (!prefab) return null;

            var biome = CatToolManager.GetBiome(生态区域);
            if (biome == Heightmap.Biome.None) return null;


            return new SpawnSystem.SpawnData
            {
                m_name = prefab.name,
                m_prefab = prefab,
                m_enabled = 启用,
                m_biome = biome,
                m_biomeArea = 区域范围,
                m_maxSpawned = ((最大生成量 < 1) ? 1 : 最大生成量),
                m_spawnInterval = 生成_间隔,
                m_spawnChance = 生成_概率,
                m_spawnDistance = 生成_间隔,
                m_spawnRadiusMin = 最小玩家间距,
                m_spawnRadiusMax = ((最小玩家间距 > 最大玩家间距) ? 最小玩家间距 : 最大玩家间距),
                m_requiredGlobalKey = 需求_条件,
                m_requiredEnvironments = 需求_天气,
                m_groupSizeMin = 组最小,
                m_groupSizeMax = ((组最大 < 组最小) ? 组最小 : 组最大),
                m_groupRadius = 组间距,
                m_spawnAtNight = 晚上生成,
                m_spawnAtDay = 白天生成,
                m_minAltitude = 最小高度,
                m_maxAltitude = ((最大高度 < 最小高度) ? 最小高度 : 最大高度),
                m_minTilt = 最小倾斜,
                m_maxTilt = ((最大倾斜 < 最小倾斜) ? 最小倾斜 : 最大倾斜),
                m_inForest = 森林内生成,
                m_outsideForest = 森林外生成,
                m_minOceanDepth = 最小海洋深度,
                m_maxOceanDepth = ((最大海洋深度 < 最小海洋深度) ? 最小海洋深度 : 最大海洋深度),
                m_huntPlayer = 追杀玩家,
                m_groundOffset = 地面偏移,
                m_minLevel = 生物_最低等级,
                m_maxLevel = ((生物_最高等级 < 生物_最低等级) ? 生物_最低等级 : 生物_最高等级)
            };


        }


    }
}
