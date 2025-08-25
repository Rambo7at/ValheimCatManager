using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimCatManager.Data
{
    class CatModData
    {


        public static readonly List<CookingStationConfig> 烹饪站配置_列表 = new List<CookingStationConfig>();
        public static readonly List<SmeltersConfig> 炼制站配置_列表 = new List<SmeltersConfig>();
        public static readonly List<SpawnConfig> 自定义生成_列表 = new List<SpawnConfig>();
        public static readonly List<MonsterConfig> 自定义怪物_列表 = new List<MonsterConfig>();











        public static readonly Dictionary<int, GameObject> 自定义物品_字典 = new Dictionary<int, GameObject>();


        public static readonly Dictionary<int, GameObject> 自定义预制件_字典 = new Dictionary<int, GameObject>();


        public static readonly Dictionary<int, PieceConfig> 自定义物件_字典 = new Dictionary<int, PieceConfig>();



        public static readonly Dictionary<string, RecipeConfig> 自定义配方_字典 = new Dictionary<string, RecipeConfig>();



        public static readonly Dictionary<int, VegetationConfig> 自定义植被_字典 = new Dictionary<int, VegetationConfig>();





        public static readonly Dictionary<string, Piece.PieceCategory> 自定义目录_字典 = new Dictionary<string, Piece.PieceCategory>();




        public static readonly Dictionary<string, GameObject> m_PrefabCache = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, Shader> m_haderCache = new Dictionary<string, Shader>();

        public static readonly Dictionary<int, string> 模拟物品_字典 = new Dictionary<int, string>();


        public static readonly Dictionary<string, PieceTable> m_PieceTableCache = new Dictionary<string, PieceTable>();


        /// <summary>
        /// 注：给 SpawnSystem 添加生成的类
        /// </summary>
        public static SpawnSystemList SpawnSystemList = new SpawnSystemList();






        public static readonly Dictionary<string, int> ValidShaderId = new Dictionary<string, int>
        {

              { "Custom/Vegetation", 26578 },
              { "Custom/Player", 50990 },
              { "Custom/Creature", 32476 },
              { "Custom/StaticRock", 12342 },
              { "Custom/SkyboxProcedural", 12614 },
              { "Custom/LitParticles", 20508 },
              { "Custom/Clouds", 27696 },
              { "Custom/Piece", 31092 },
              { "Custom/Rug", 42250 },
              { "Custom/Blob", 53332 },
              { "Custom/Decal", 54890 },
              { "Custom/Gradient Mapped Particle (Unlit)", 58322 },
              { "Custom/Trilinearmap", 73462 },
              { "Custom/ParticleDecal", 74762 },
              { "Custom/Grass", 75792 },
              { "Custom/AlphaParticle", 89034 },
              { "Custom/Bonemass", 89088 },
              { "Custom/Distortion", 90942 },
              { "Custom/Heightmap", 162756 },
              { "Custom/FlowOpaque", 167070 },
              { "Custom/Water", 170062 },
              { "Custom/SkyObject", 186528 },
              { "Custom/ShadowBlob", 211800 },
              { "Custom/WaterMask", 213220 },
              { "Custom/LitGui", 294236 },
              { "Custom/Tar", 306618 },
              { "Custom/Yggdrasil_root", 349756 },
              { "Custom/Flow", 361964 },
              { "Custom/Particle (Unlit)", 364536 },
              { "Custom/Mesh Flipbook Particle", 387474 },
              { "Custom/WaterBottom", 607840 },
              { "Custom/Yggdrasil", 623678 },
              { "Custom/UI_BGBlur", 623682 },
              { "Custom/mapshader", 632722 },
              { "Custom/icon", 632820 },

        };









    }
}
