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
        public static readonly Dictionary<int, GameObject> 自定义物品_字典 = new Dictionary<int, GameObject>();


        public static readonly Dictionary<int, GameObject> 自定义预制件_字典 = new Dictionary<int, GameObject>();


        public static readonly Dictionary<int, string> 自定义食物_字典 = new Dictionary<int, string>();


        public static readonly Dictionary<string, RecipeConfig> 自定义配方_字典 = new Dictionary<string, RecipeConfig>();



        public static readonly Dictionary<int, VegetationConfig> 自定义植被_字典 = new Dictionary<int, VegetationConfig>();






        public static readonly Dictionary<string, Piece.PieceCategory> 自定义目录_字典 = new Dictionary<string, Piece.PieceCategory>();



        public static readonly Dictionary<string, GameObject> m_PrefabCache = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, Shader> s_builtinShaderCache = new Dictionary<string, Shader>();

        public static readonly Dictionary<int, string> 模拟物品_字典 = new Dictionary<int, string>();








    }
}
