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

    }
}
