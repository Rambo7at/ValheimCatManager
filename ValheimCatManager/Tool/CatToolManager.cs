using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
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
            Assembly resourceAssembly = Assembly.GetExecutingAssembly();

            string resourceName = Array.Find(resourceAssembly.GetManifestResourceNames(), name => name.EndsWith(AssetName));

            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError($"Dll 中未有找到 {AssetName} 资源包");
                return null;
            }

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
        /// 注：注册配方
        /// </summary>
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



        public static void RegisterSmeltersConfig(List<SmeltersConfig> smeltersConfigs)
        {
            foreach (var item in smeltersConfigs)
            {
                GameObject prefabSmelters = GetGameObject(item.预制名);
                GameObject prefabInputItem = GetGameObject(item.输入);
                GameObject prefabOutputItem = GetGameObject(item.输出);
                if (prefabSmelters == null || prefabInputItem == null || prefabOutputItem == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，有单个预制件是空[{item.预制名}],[{item.输入}],[{item.输出}]，已跳过");
                    continue;
                }

                Smelter smelter = prefabSmelters.GetComponent<Smelter>();
                if (smelter == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，[{item.预制名}] 没有对应的[smelters组件]，已跳过");
                    continue;
                }

                ItemDrop itemDropInput = prefabInputItem.GetComponent<ItemDrop>();
                ItemDrop itemDropOutput = prefabOutputItem.GetComponent<ItemDrop>();
                if (itemDropInput == null || itemDropOutput == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，物品：{item.输入}或{item.输出} 没有[ItemDrop组件]，已跳过");
                    continue;
                }

                smelter.m_conversion.Add(new Smelter.ItemConversion {m_from = itemDropInput ,m_to = itemDropOutput});

            }

        }

        public static void RegisterCookingStationConfig(List<CookingStationConfig> cookingStationConfigs)
        {
            foreach (var item in cookingStationConfigs)
            {
                GameObject prefabCookingStation = GetGameObject(item.预制名);
                GameObject prefabInputItem = GetGameObject(item.输入);
                GameObject prefabOutputItem = GetGameObject(item.输出);
                if (prefabCookingStation == null || prefabInputItem == null || prefabOutputItem == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，有单个预制件是空[{item.预制名}],[{item.输入}],[{item.输出}]，已跳过");
                    continue;
                }
                CookingStation cookingStation = prefabCookingStation.GetComponent<CookingStation>();
                if (cookingStation == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，[{item.预制名}] 没有对应的[CookingStation组件]，已跳过");
                    continue;
                }
                ItemDrop itemDropInput = prefabInputItem.GetComponent<ItemDrop>();
                ItemDrop itemDropOutput = prefabOutputItem.GetComponent<ItemDrop>();
                if (itemDropInput == null || itemDropOutput == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，物品：{item.输入}或{item.输出} 没有[ItemDrop组件]，已跳过");
                    continue;
                }

                cookingStation.m_conversion.Add(new CookingStation.ItemConversion { m_from = itemDropInput, m_to  = itemDropOutput , m_cookTime = item.时间 });


            }
        }


        public static void RegisterSpawnList(SpawnSystem instance)
        {
            if (CatModData.自定义生成_列表.Count != 0)
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



            Debug.LogError($"长度：{CatModData.SpawnSystemList.m_spawners.Count}-----------------------");

            if (!instance || CatModData.SpawnSystemList.m_spawners.Count == 0) return;

            instance.m_spawnLists.Add(CatModData.SpawnSystemList);

        }

        public static void RegisterVegetation(Dictionary<int, VegetationConfig> VegetationDictionary, ZoneSystem instance)
        {

            if (VegetationDictionary.Count == 0 || instance == null) return;



            foreach (var VegetationS in VegetationDictionary)
            {
                var Vegetation = VegetationS.Value.GetZoneVegetation();
                if (instance.m_vegetation.Contains(Vegetation)) continue;

                instance.m_vegetation.Add(Vegetation);
            }


        }


        public static void RegisterMonsterConfig(List<MonsterConfig> monsterConfigs)
        {
            foreach (var monster in monsterConfigs)
            {
                GameObject monsterPrefab = GetGameObject(monster.预制名);
                if (monsterPrefab == null)
                {
                    Debug.LogError($"执行RegisterMonsterConfig时，预制件是空[{monster.预制名}]，已跳过");
                    continue;
                }
                if (monster.食谱.Length == 0) continue;
                MonsterAI monsterAI = monsterPrefab.GetComponent<MonsterAI>();
                if (monsterAI == null)
                {
                    Debug.LogError($"执行RegisterMonsterConfig时，预制件：[{monster.预制名}]没有对应的MonsterAI组件，已跳过");
                    continue;
                }
                List<ItemDrop> itemDrops = new List<ItemDrop>();

                foreach (var item in monster.食谱)
                {
                    GameObject itemPrefab = GetGameObject(item);
                    if (itemPrefab == null)
                    {
                        Debug.LogError($"执行RegisterMonsterConfig遍历食谱时，预制件：[{item}]是空，对应生物[{monster.预制名}]，已跳过");
                        continue;
                    }
                    ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
                    if (itemDrop == null)
                    {
                        Debug.LogError($"执行RegisterMonsterConfig遍历食谱时，预制件：[{item}]没有ItemDrop组件，对应生物[{monster.预制名}]，已跳过");
                        continue;
                    }
                    itemDrops.Add(itemDrop);
                }


                if (itemDrops.Count > 0 )
                {
                    monsterAI.m_consumeItems = itemDrops;
                }

            }
        }
















        public static Heightmap.Biome GetBiome(string biomeName)
        {
            foreach (Heightmap.Biome biome in Enum.GetValues(typeof(Heightmap.Biome))) if (Enum.GetName(typeof(Heightmap.Biome), biome) == biomeName) return biome;
            Debug.LogError($"未找到自定义区域：{biomeName}检查一下");
            return Heightmap.Biome.None;

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


        /// <summary>
        /// 注：这是一个检测方法，检测 Piece.PieceCategory 反射出来的值
        /// </summary>
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


            GameObject itemPrefab = ZNetScene.instance.GetPrefab(name) ?? ObjectDB.instance.GetItemPrefab(name)?? ResourcesGetGameObject(name);
            if (itemPrefab == null)
            {
                Debug.LogError($"未查询到注册 预制件[{name}]");
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
                Debug.LogError($"未查询到注册预制件，哈希值：[{hash}]");
                return null;
            }
            else
            {
                if (!CatModData.m_PrefabCache.ContainsKey(itemPrefab.name)) CatModData.m_PrefabCache.Add(itemPrefab.name, itemPrefab);
                return itemPrefab;
            }
        }



        static GameObject ResourcesGetGameObject(string name)
        {

            var @object = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (var item in @object)
            {
                if (item.name == name)
                {



                    return item;


                }
            }

            
            return null;



        }
    }












}
