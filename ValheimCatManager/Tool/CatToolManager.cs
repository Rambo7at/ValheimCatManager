using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Data;
using static Piece;

namespace ValheimCatManager.Tool
{
    public class CatToolManager
    {



        /// <summary>
        /// 加载 AB包资源 的方法
        /// </summary>
        /// <param name="AssetName"></param>
        /// <remarks><paramref name="AssetName"/> ：传入资源名 string 类型 </remarks>
        /// <returns>AB包</returns>
        public static AssetBundle LoadAssetBundle(string AssetName)
        {
            // 获取本体程序集
            Assembly resourceAssembly = Assembly.GetExecutingAssembly();

            // 获取所有嵌入式资源名称 并 查找匹配的资源名称
            string resourceName = Array.Find(resourceAssembly.GetManifestResourceNames(), name => name.EndsWith(AssetName));

            // 判断资源是否找到
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError($"Dll 中未有找到 {AssetName} 资源包");
                return null;
            }

            // 从资源流加载AssetBundle
            using (Stream stream = resourceAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogError($"无法获取资源流: {resourceName}");
                    return null;
                }

                return AssetBundle.LoadFromStream(stream);
            }
        }





        public static void RegisterToObjectDB(ObjectDB instance, Dictionary<int, GameObject> itemDictionary)
        {
            if (instance == null || itemDictionary.Count == 0) return;

            var m_itemByHashField = AccessTools.Field(typeof(ObjectDB), "m_itemByHash");
            var m_itemByHash = m_itemByHashField.GetValue(instance) as Dictionary<int, GameObject>;

            foreach (var item in itemDictionary)
            {
                int hash = item.Key;
                if (m_itemByHash.ContainsKey(hash)) continue;


                instance.m_items.Add(item.Value);
                m_itemByHash.Add(hash, item.Value);
            }
        }


        public static void RegisterRecipe(ObjectDB instance ,Dictionary<string, RecipeConfig> recipeDictionary)
        {

            foreach (var recipeConfig in recipeDictionary)
            {

                var recipe = recipeConfig.Value.GetRecipe();

                
                if (recipe == null)
                {
                    Debug.LogError($"添加的{recipeConfig.Key}配方设置有误");
                    continue;

                }

                if (!instance.m_recipes.Contains(recipe))
                {
                    instance.m_recipes.Add(recipe);

                }

            }
           


        }




        public static void RegisterToZNetScene(Dictionary<int, GameObject> itemDictionary)
        {
            if (ZNetScene.instance == null || itemDictionary.Count == 0) return;

            var m_namedPrefabsField = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs");
            var m_namedPrefabs = m_namedPrefabsField.GetValue(ZNetScene.instance) as Dictionary<int, GameObject>;

            foreach (var item in itemDictionary)
            {

                if (m_namedPrefabs.ContainsKey(item.Key)) continue;


                ZNetScene.instance.m_prefabs.Add(item.Value);
                m_namedPrefabs.Add(item.Key, item.Value);
            }
        }




        /// <summary>
        /// 注：注册预制件 给对应的制作工具
        /// <br>优化考虑：可以准备一个 Category 的缓存</br>
        /// </summary>
        /// <param name="pieceConfigDictionary"></param>
        public static void RegisterPiece(Dictionary<int, PieceConfig> pieceConfigDictionary)
        {

            foreach (var pieceConfig in pieceConfigDictionary)
            {
                string pieceName = pieceConfig.Value.GetPrefabName();
                string categoryName = pieceConfig.Value.分组;
                PieceTable pieceTable = pieceConfig.Value.GetPieceTable();
                GameObject piecePrefab = CatToolManager.GetGameObject(pieceConfig.Key);


                if (piecePrefab == null)
                {
                    Debug.LogError($"执行RegisterPiece时，Piece：{pieceName}，预制件是空，已跳过");
                    continue;
                }

                if (pieceTable == null)
                {
                    Debug.LogError($"执行RegisterPiece时，Piece：{pieceName}，对应的 【组件：pieceTable】 是空，已跳过");
                    continue;
                }

                Piece piece = piecePrefab.GetComponent<Piece>();
                if (piece == null)
                {
                    Debug.LogError($"执行RegisterPiece时，Piece：{pieceName}，对应的 【组件：Piece】 是空，已跳过");
                    continue;
                }



                if (!pieceTable.m_categoryLabels.Contains(categoryName))
                {
                    pieceTable.m_categoryLabels.Add(categoryName);
                    pieceTable.m_categories.Add(GetCategory(categoryName));
                }


                if (!pieceTable.m_pieces.Contains(piecePrefab)) pieceTable.m_pieces.Add(piecePrefab);
                piece.m_category = GetCategory(categoryName);

                if(piece.m_resources.Length != 0) continue;

                piece.m_resources = pieceConfig.Value.GetRequirementS();

            }


        }






        public static void RegisterVegetation(Dictionary<int, VegetationConfig> VegetationDictionary , ZoneSystem instance)
        { 
        
             if (VegetationDictionary.Count == 0 || instance == null) return;



            foreach (var VegetationS in VegetationDictionary)
            {
                var Vegetation = VegetationS.Value.GetZoneVegetation();
                if (instance.m_vegetation.Contains(Vegetation)) continue;

                instance.m_vegetation.Add(Vegetation);
            }


        }



        public static void RegisterSpawnList( SpawnSystem instance)
        {
            if (!CatModData.自定义生成_列表.Any())
            {
                foreach (var spawnConfig in CatModData.自定义生成_列表)
                {

                    var spawnData = spawnConfig.GetSpawnData();
                    if (spawnData == null)
                    {
                        Debug.LogError($"添加生成列表错误，生成数据是空检查：{spawnConfig.预制件}");
                        continue;
                    }

                    CatModData.SpawnSystemList.m_spawners.Add(spawnData);
                }
                CatModData.自定义生成_列表.Clear();

            }


            if (!CatModData.SpawnSystemList || !instance) return;


            instance.m_spawnLists.Add(CatModData.SpawnSystemList);


        }








        /// <summary>
        /// 注：通过反射 游戏枚举Piece.PieceCategory 获取枚举值
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns>Piece.PieceCategory</returns>
        private static Piece.PieceCategory GetCategory(string categoryName)
        {
            Array enumValues = Enum.GetValues(typeof(Piece.PieceCategory));
            string[] enumNames = Enum.GetNames(typeof(Piece.PieceCategory));

            for (int i = 0; i < enumNames.Length; i++)
            {
                if (enumNames[i] == categoryName) return (Piece.PieceCategory)enumValues.GetValue(i);
            }
            Debug.LogError($"执行GetCategory时，未找到对应名 枚举名：{categoryName}");
            return Piece.PieceCategory.All;
        }





        /// <summary>
        /// 注：注册自定义 Category
        /// <br><paramref name="Category"></paramref></br>：
        /// 指的是 Piece上方的目录条
        /// </summary>
        public static void RegisterCategory()
        {
            foreach (var item in CatModData.自定义物件_字典)
            {
                if (!CatModData.自定义目录_字典.ContainsKey(item.Value.分组))
                {
                    //Debug.LogError($"有进入这里 ");
                    int indx = Enum.GetNames(typeof(Piece.PieceCategory)).Length - 1;
                    //Debug.LogError($"打点1 长度：{indx}");
                    CatModData.自定义目录_字典.Add(item.Value.分组, (Piece.PieceCategory)indx);
                    //Debug.LogError($"打点2 ");
                }
            }

        }


        public static void EnumGetPieceCategoryValuesPatch(Type enumType, ref Array __result)
        {
            if (!(enumType != typeof(Piece.PieceCategory)) && CatModData.自定义目录_字典.Count != 0)
            {
                Piece.PieceCategory[] array = new Piece.PieceCategory[__result.Length + CatModData.自定义目录_字典.Count];
                __result.CopyTo(array, 0);
                CatModData.自定义目录_字典.Values.CopyTo(array, __result.Length);
                __result = array;
            }
        }


        public static void EnumGetPieceCategoryNamesPatch(Type enumType, ref string[] __result)
        {
            if (!(enumType != typeof(Piece.PieceCategory)) && CatModData.自定义目录_字典.Count != 0)
            {
                __result = __result.AddRangeToArray(CatModData.自定义目录_字典.Keys.ToArray());
            }
        }














        /// <summary>
        /// 注：获取游戏原版的着色器 给 <paramref name="m_haderCache"/> 缓存
        /// </summary>
        public static void GetShaderToCache()
        {

            var realShader = CatToolManager.GetGameObject("Turnip").GetComponentsInChildren<Renderer>(true);
            foreach (var item in realShader)
            {
                if (item is Renderer renderer)
                {
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        Material material = renderer.sharedMaterials[i];
                        if (material.shader.name == "Custom/Vegetation")
                        {
                            if (!CatModData.m_haderCache.ContainsKey(material.shader.name)) CatModData.m_haderCache.Add(material.shader.name, material.shader);

                            //Debug.LogError($"信息：{CatModData.m_haderCache.Count}");
                        }
                            
                    }



                }

            }

        }










        public static void GetPieceCategory()
        {
            Array enumValues = Enum.GetValues(typeof(Piece.PieceCategory));
            string[] enumNames = Enum.GetNames(typeof(Piece.PieceCategory));
            Debug.LogError($"反射表单长度是：{enumValues.Length}");

            if (enumValues.Length == enumNames.Length)
            {
                for (int i = 0; i < enumValues.Length; i++)
                {
                    Debug.LogError($"进入表达添加循环：键-{enumNames[i]}；值-{(Piece.PieceCategory)enumValues.GetValue(i)}");

                }

            }

        }






















        /// <summary>
        /// 注：获取已经完成注册的预制件，并添加给缓存(以预制件名获取)
        /// </summary>
        /// <param name="name"></param>
        /// <returns>返回 GameObject</returns>
        public static GameObject GetGameObject(string name)
        {

            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("获取预制件名时，传入了空字符");
                return null;
            }

            if (CatModData.m_PrefabCache.ContainsKey(name)) return CatModData.m_PrefabCache[name];


            GameObject itemPrefab = ZNetScene.instance.GetPrefab(name) ?? ObjectDB.instance.GetItemPrefab(name);

            if (itemPrefab == null)
            {
                Debug.LogError($"未查询到注册 预制件{name}");
                return null;
            } 
            else
            {
                if (!CatModData.m_PrefabCache.ContainsKey(itemPrefab.name))
                {
                    CatModData.m_PrefabCache.Add(itemPrefab.name, itemPrefab);
                }

                return itemPrefab; 
            }

          
        }


        /// <summary>
        /// 注：获取已经完成注册的预制件，并添加给缓存(以哈希值获取)
        /// </summary>
        /// <param name="hash">填写物品哈希值</param>
        /// <returns>返回 GameObject</returns>
        public static GameObject GetGameObject(int hash)
        {

            GameObject itemPrefab = ZNetScene.instance.GetPrefab(hash) ?? ObjectDB.instance.GetItemPrefab(hash);

            if (itemPrefab == null)
            {
                Debug.LogError($"未查询到注册预制件，哈希值：{hash}");
                return null;
            }
            else
            {
                if (!CatModData.m_PrefabCache.ContainsKey(itemPrefab.name)) CatModData.m_PrefabCache.Add(itemPrefab.name, itemPrefab);
                return itemPrefab;
            }
        }




        public static Heightmap.Biome GetBiome (string biomeName)
        {
            foreach (Heightmap.Biome biome in Enum.GetValues(typeof(Heightmap.Biome))) if (Enum.GetName(typeof(Heightmap.Biome), biome) == biomeName) return biome;
            Debug.LogError($"未找到自定义区域：{biomeName}检查一下");
            return Heightmap.Biome.None;

        }





    }
}
