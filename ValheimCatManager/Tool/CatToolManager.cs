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
    /// <summary>
    /// 工具管理类（提供资源加载、游戏对象注册、配置注册等核心工具方法，支撑自定义内容接入游戏）
    /// </summary>
    public class CatToolManager
    {
        /// <summary>
        /// 注：从当前程序集（DLL）中加载指定名称的AssetBundle资源包
        /// </summary>
        /// <param name="AssetName">要加载的AssetBundle名称（含后缀，如"catmod.unity3d"）</param>
        /// <returns>加载成功的AssetBundle实例；加载失败（未找到资源/资源流异常）则返回null</returns>
        /// <remarks>
        /// <paramref name="AssetName"/> ：传入资源名 string 类型，需与DLL中嵌入的资源名完全匹配<br/>
        /// 内部逻辑：1. 获取当前执行程序集 → 2. 查找匹配名称的资源 → 3. 读取资源流 → 4. 从流加载AssetBundle
        /// </remarks>
        public static AssetBundle LoadAssetBundle(string AssetName)
        {
            // 获取当前执行的程序集（即包含该工具类的DLL）
            Assembly resourceAssembly = Assembly.GetExecutingAssembly();

            // 从程序集中查找名称以目标AssetName结尾的资源（匹配嵌入的AB包）
            string resourceName = Array.Find(resourceAssembly.GetManifestResourceNames(), name => name.EndsWith(AssetName));

            // 未找到对应资源时打印错误并返回null
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError($"Dll 中未有找到 {AssetName} 资源包");
                return null;
            }

            // 读取资源流并加载AssetBundle
            using (Stream stream = resourceAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogError($"无法获取资源流: {resourceName}");
                    return null;
                }

                // 从资源流加载AssetBundle
                return AssetBundle.LoadFromStream(stream);
            }
        }

        /// <summary>
        /// 注：将自定义物品注册到ObjectDB（游戏物品数据库），确保物品可被游戏识别
        /// </summary>
        /// <param name="instance">目标ObjectDB实例（通常为ObjectDB singleton）</param>
        /// <param name="itemDictionary">自定义物品字典（键：物品哈希值，值：物品预制件）</param>
        public static void RegisterToObjectDB(ObjectDB instance, Dictionary<int, GameObject> itemDictionary)
        {
            // 实例为空或无物品时直接返回
            if (instance == null || itemDictionary.Count == 0) return;

            foreach (var item in itemDictionary)
            {
                int hash = item.Key;
                // 避免重复注册（已存在该哈希的物品则跳过）
                if (instance.m_itemByHash.ContainsKey(hash)) continue;

                // 将物品添加到物品列表和哈希映射表
                instance.m_items.Add(item.Value);
                instance.m_itemByHash.Add(hash, item.Value);
            }
        }

        /// <summary>
        /// 注：将自定义物品/预制件注册到ZNetScene（网络场景管理器），确保网络同步和场景加载时可用
        /// </summary>
        /// <param name="instance">目标ZNetScene实例（通常为ZNetScene singleton）</param>
        /// <param name="itemDictionary">自定义预制件字典（键：预制件哈希值，值：预制件）</param>
        public static void RegisterToZNetScene(ZNetScene instance, Dictionary<int, GameObject> itemDictionary)
        {
            // 实例为空或无预制件时直接返回
            if (instance == null || itemDictionary.Count == 0) return;

            foreach (var item in itemDictionary)
            {
                // 避免重复注册（已存在该哈希的预制件则跳过）
                if (ZNetScene.instance.m_namedPrefabs.ContainsKey(item.Key)) continue;

                // 获取预制件的ZNetView组件（判断是否需要网络同步）
                var view = item.Value.GetComponent<ZNetView>();

                if (view == null)
                {
                    // 无ZNetView的预制件添加到非网络视图列表
                    instance.m_nonNetViewPrefabs.Add(item.Value);
                }
                else
                {
                    // 有ZNetView的预制件添加到网络视图列表
                    instance.m_prefabs.Add(item.Value);
                }
                // 注册预制件到名称-预制件映射表（用于快速查找）
                instance.m_namedPrefabs.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// 注：注册自定义配方到ObjectDB，确保配方可在游戏中使用（如制作台显示）
        /// </summary>
        /// <param name="instance">目标ObjectDB实例（存储游戏配方）</param>
        /// <param name="recipeDictionary">自定义配方字典（键：配方标识，值：配方配置）</param>
        public static void RegisterRecipe(ObjectDB instance, Dictionary<string, RecipeConfig> recipeDictionary)
        {
            foreach (var recipeConfig in recipeDictionary)
            {
                // 从配方配置中生成游戏原生Recipe实例
                var recipe = recipeConfig.Value.GetRecipe();

                // 配方生成失败时打印错误并跳过
                if (recipe == null)
                {
                    Debug.LogError($"添加的{recipeConfig.Key}配方设置有误");
                    continue;
                }

                // 避免重复注册（配方列表中无该配方则添加）
                if (!instance.m_recipes.Contains(recipe))
                {
                    instance.m_recipes.Add(recipe);
                }
            }
        }

        /// <summary>
        /// 注：注册自定义炼制站配方（如熔炉、锻铁炉），添加物品转换规则到炼制站组件
        /// </summary>
        /// <param name="smeltersConfigs">炼制站配置列表（含炼制站预制名、输入/输出物品等）</param>
        public static void RegisterSmeltersConfig(List<SmeltersConfig> smeltersConfigs)
        {
            foreach (var item in smeltersConfigs)
            {
                // 加载炼制站、输入物品、输出物品的预制件
                GameObject prefabSmelters = GetGameObject(item.预制名);
                GameObject prefabInputItem = GetGameObject(item.输入);
                GameObject prefabOutputItem = GetGameObject(item.输出);

                // 任一预制件为空时打印错误并跳过
                if (prefabSmelters == null || prefabInputItem == null || prefabOutputItem == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，有单个预制件是空[{item.预制名}],[{item.输入}],[{item.输出}]，已跳过");
                    continue;
                }

                // 获取炼制站组件（Smelter），无组件则跳过
                Smelter smelter = prefabSmelters.GetComponent<Smelter>();
                if (smelter == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，[{item.预制名}] 没有对应的[smelters组件]，已跳过");
                    continue;
                }

                // 获取输入/输出物品的ItemDrop组件（物品核心组件），无组件则跳过
                ItemDrop itemDropInput = prefabInputItem.GetComponent<ItemDrop>();
                ItemDrop itemDropOutput = prefabOutputItem.GetComponent<ItemDrop>();
                if (itemDropInput == null || itemDropOutput == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，物品：{item.输入}或{item.输出} 没有[ItemDrop组件]，已跳过");
                    continue;
                }

                // 向炼制站添加物品转换规则（输入→输出）
                smelter.m_conversion.Add(new Smelter.ItemConversion { m_from = itemDropInput, m_to = itemDropOutput });
            }
        }

        /// <summary>
        /// 注：注册自定义烹饪站配方（如烹饪锅），添加物品转换规则（含烹饪时间）到烹饪站组件
        /// </summary>
        /// <param name="cookingStationConfigs">烹饪站配置列表（含烹饪站预制名、输入/输出物品、烹饪时间等）</param>
        public static void RegisterCookingStationConfig(List<CookingStationConfig> cookingStationConfigs)
        {
            foreach (var item in cookingStationConfigs)
            {
                // 加载烹饪站、输入物品、输出物品的预制件
                GameObject prefabCookingStation = GetGameObject(item.预制名);
                GameObject prefabInputItem = GetGameObject(item.输入);
                GameObject prefabOutputItem = GetGameObject(item.输出);

                // 任一预制件为空时打印错误并跳过
                if (prefabCookingStation == null || prefabInputItem == null || prefabOutputItem == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，有单个预制件是空[{item.预制名}],[{item.输入}],[{item.输出}]，已跳过");
                    continue;
                }

                // 获取烹饪站组件（CookingStation），无组件则跳过
                CookingStation cookingStation = prefabCookingStation.GetComponent<CookingStation>();
                if (cookingStation == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，[{item.预制名}] 没有对应的[CookingStation组件]，已跳过");
                    continue;
                }

                // 获取输入/输出物品的ItemDrop组件（物品核心组件），无组件则跳过
                ItemDrop itemDropInput = prefabInputItem.GetComponent<ItemDrop>();
                ItemDrop itemDropOutput = prefabOutputItem.GetComponent<ItemDrop>();
                if (itemDropInput == null || itemDropOutput == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，物品：{item.输入}或{item.输出} 没有[ItemDrop组件]，已跳过");
                    continue;
                }

                // 向烹饪站添加物品转换规则（输入→输出，含自定义烹饪时间）
                cookingStation.m_conversion.Add(new CookingStation.ItemConversion
                {
                    m_from = itemDropInput,
                    m_to = itemDropOutput,
                    m_cookTime = item.时间
                });
            }
        }

        /// <summary>
        /// 注：注册自定义生成配置到SpawnSystem（生成系统），确保怪物/物品按配置在场景中生成
        /// </summary>
        /// <param name="instance">目标SpawnSystem实例（游戏生成管理器）</param>
        public static void RegisterSpawnList(SpawnSystem instance)
        {
            // 处理自定义生成列表，转换为SpawnData并添加到统一生成列表
            if (CatModData.自定义生成_列表.Count != 0)
            {
                foreach (var spawnConfig in CatModData.自定义生成_列表)
                {
                    // 从生成配置中生成游戏原生SpawnData实例
                    var spawnData = spawnConfig.GetSpawnData();
                    if (spawnData == null)
                    {
                        Debug.LogError($"添加生成列表错误，生成数据是空检查：{spawnConfig.预制件}");
                        continue;
                    }

                    // 添加到全局生成列表缓存
                    CatModData.SpawnSystemList.m_spawners.Add(spawnData);
                }
                // 清空原自定义生成列表（避免重复处理）
                CatModData.自定义生成_列表.Clear();
            }

            // 生成系统实例为空或无生成数据时直接返回
            if (!instance || CatModData.SpawnSystemList.m_spawners.Count == 0) return;

            // 将自定义生成列表添加到游戏生成系统
            instance.m_spawnLists.Add(CatModData.SpawnSystemList);
        }

        /// <summary>
        /// 注：注册自定义植被到ZoneSystem（区域系统），确保植被在指定区域生成
        /// </summary>
        /// <param name="VegetationDictionary">自定义植被字典（键：植被哈希值，值：植被配置）</param>
        /// <param name="instance">目标ZoneSystem实例（游戏区域管理器）</param>
        public static void RegisterVegetation(Dictionary<int, VegetationConfig> VegetationDictionary, ZoneSystem instance)
        {
            // 植被字典为空或区域系统实例为空时直接返回
            if (VegetationDictionary.Count == 0 || instance == null) return;

            foreach (var VegetationS in VegetationDictionary)
            {
                // 从植被配置中生成游戏原生ZoneVegetation实例
                var Vegetation = VegetationS.Value.GetZoneVegetation();
                // 避免重复注册（区域植被列表中无该植被则添加）
                if (instance.m_vegetation.Contains(Vegetation)) continue;

                instance.m_vegetation.Add(Vegetation);
            }
        }

        /// <summary>
        /// 注：注册自定义怪物配置，主要设置怪物的可食用物品列表（MonsterAI.m_consumeItems）
        /// </summary>
        /// <param name="monsterConfigs">怪物配置列表（含怪物预制名、可食用物品列表等）</param>
        public static void RegisterMonsterConfig(List<MonsterConfig> monsterConfigs)
        {
            foreach (var monster in monsterConfigs)
            {
                // 加载怪物预制件，为空则跳过
                GameObject monsterPrefab = GetGameObject(monster.预制名);
                if (monsterPrefab == null)
                {
                    Debug.LogError($"执行RegisterMonsterConfig时，预制件是空[{monster.预制名}]，已跳过");
                    continue;
                }

                // 怪物无食谱配置时跳过
                if (monster.食谱.Length == 0) continue;

                // 获取怪物的MonsterAI组件（怪物AI核心），无组件则跳过
                MonsterAI monsterAI = monsterPrefab.GetComponent<MonsterAI>();
                if (monsterAI == null)
                {
                    Debug.LogError($"执行RegisterMonsterConfig时，预制件：[{monster.预制名}]没有对应的MonsterAI组件，已跳过");
                    continue;
                }

                // 收集可食用物品的ItemDrop组件
                List<ItemDrop> itemDrops = new List<ItemDrop>();
                foreach (var item in monster.食谱)
                {
                    // 加载食谱物品预制件，为空则跳过
                    GameObject itemPrefab = GetGameObject(item);
                    if (itemPrefab == null)
                    {
                        Debug.LogError($"执行RegisterMonsterConfig遍历食谱时，预制件：[{item}]是空，对应生物[{monster.预制名}]，已跳过");
                        continue;
                    }

                    // 获取物品的ItemDrop组件，无组件则跳过
                    ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
                    if (itemDrop == null)
                    {
                        Debug.LogError($"执行RegisterMonsterConfig遍历食谱时，预制件：[{item}]没有ItemDrop组件，对应生物[{monster.预制名}]，已跳过");
                        continue;
                    }

                    itemDrops.Add(itemDrop);
                }

                // 收集到可食用物品时，设置到MonsterAI的可食用列表
                if (itemDrops.Count > 0)
                {
                    monsterAI.m_consumeItems = itemDrops;
                }
            }
        }

        /// <summary>
        /// 注：根据生物群系名称获取对应的Heightmap.Biome枚举值
        /// </summary>
        /// <param name="biomeName">生物群系名称（需与枚举名完全一致，如"Meadows"）</param>
        /// <returns>匹配的Heightmap.Biome枚举值；未找到时打印错误并返回Heightmap.Biome.None</returns>
        public static Heightmap.Biome GetBiome(string biomeName)
        {
            // 遍历Biome枚举，匹配名称
            foreach (Heightmap.Biome biome in Enum.GetValues(typeof(Heightmap.Biome)))
            {
                if (Enum.GetName(typeof(Heightmap.Biome), biome) == biomeName)
                {
                    return biome;
                }
            }

            // 未找到时打印错误
            Debug.LogError($"未找到自定义区域：{biomeName}检查一下");
            return Heightmap.Biome.None;
        }

        /// <summary>
        /// 注：获取游戏原版"Custom/Vegetation"着色器，并缓存到CatModData.m_haderCache中
        /// 依赖：通过"Turnip"（芜菁）预制件的Renderer组件提取着色器（确保基础资源存在）
        /// </summary>
        public static void GetShaderToCache()
        {
            // 加载"Turnip"预制件，获取其所有子对象的Renderer组件（用于提取着色器）
            var realShader = CatToolManager.GetGameObject("Turnip").GetComponentsInChildren<Renderer>(true);

            foreach (var item in realShader)
            {
                if (item is Renderer renderer)
                {
                    // 遍历Renderer的所有材质
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        Material material = renderer.sharedMaterials[i];
                        // 匹配目标着色器（"Custom/Vegetation"）
                        if (material.shader.name == "Custom/Vegetation")
                        {
                            // 避免重复缓存，添加到着色器缓存字典
                            if (!CatModData.m_haderCache.ContainsKey(material.shader.name))
                            {
                                CatModData.m_haderCache.Add(material.shader.name, material.shader);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 注：调试工具方法，通过反射打印Piece.PieceCategory枚举的所有名称和对应值
        /// 用途：验证枚举反射结果是否正确，排查分类相关问题
        /// </summary>
        public static void GetPieceCategory()
        {
            // 获取枚举的所有值和名称
            Array enumValues = Enum.GetValues(typeof(Piece.PieceCategory));
            string[] enumNames = Enum.GetNames(typeof(Piece.PieceCategory));

            Debug.LogError($"反射表单长度是：{enumValues.Length}");

            // 枚举值和名称长度一致时，打印每个枚举的键值对
            if (enumValues.Length == enumNames.Length)
            {
                for (int i = 0; i < enumValues.Length; i++)
                {
                    Debug.LogError($"进入表达添加循环：键-{enumNames[i]}；值-{(Piece.PieceCategory)enumValues.GetValue(i)}");
                }
            }
        }

        /// <summary>
        /// 注：按预制件名称查找已注册的预制件，并缓存到CatModData.m_PrefabCache（避免重复查询）
        /// 查找优先级：ZNetScene → ObjectDB → Resources（兜底）
        /// </summary>
        /// <param name="name">预制件名称（需与注册时的名称完全一致）</param>
        /// <returns>找到的预制件GameObject；未找到则打印错误并返回null</returns>
        public static GameObject GetGameObject(string name)
        {
            // 传入名称为空时打印错误
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("获取预制件名时，传入了空字符");
                return null;
            }

            // 缓存中已有则直接返回（优先使用缓存）
            if (CatModData.m_PrefabCache.ContainsKey(name)) return CatModData.m_PrefabCache[name];

            // 按优先级查找预制件：ZNetScene → ObjectDB → Resources兜底
            GameObject itemPrefab = ZNetScene.instance.GetPrefab(name)
                                 ?? ObjectDB.instance.GetItemPrefab(name)
                                 ?? ResourcesGetGameObject(name);

            // 未找到预制件时打印错误
            if (itemPrefab == null)
            {
                Debug.LogError($"未查询到注册 预制件[{name}]");
                return null;
            }
            else
            {
                // 预制件未缓存时，添加到缓存（键：预制件名）
                if (!CatModData.m_PrefabCache.ContainsKey(itemPrefab.name))
                {
                    CatModData.m_PrefabCache.Add(itemPrefab.name, itemPrefab);
                }
                return itemPrefab;
            }
        }

        /// <summary>
        /// 注：按预制件哈希值查找已注册的预制件，并缓存到CatModData.m_PrefabCache（避免重复查询）
        /// 查找优先级：ZNetScene → ObjectDB
        /// </summary>
        /// <param name="hash">预制件的哈希值（通常通过GameObject.name.GetStableHashCode()生成）</param>
        /// <returns>找到的预制件GameObject；未找到则打印错误并返回null</returns>
        public static GameObject GetGameObject(int hash)
        {
            // 按优先级查找预制件：ZNetScene → ObjectDB
            GameObject itemPrefab = ZNetScene.instance.GetPrefab(hash)
                                 ?? ObjectDB.instance.GetItemPrefab(hash);

            // 未找到预制件时打印错误
            if (itemPrefab == null)
            {
                Debug.LogError($"未查询到注册预制件，哈希值：[{hash}]");
                return null;
            }
            else
            {
                // 预制件未缓存时，添加到缓存（键：预制件名）
                if (!CatModData.m_PrefabCache.ContainsKey(itemPrefab.name))
                {
                    CatModData.m_PrefabCache.Add(itemPrefab.name, itemPrefab);
                }
                return itemPrefab;
            }
        }

        /// <summary>
        /// 注：兜底查找预制件的方法（遍历Resources中所有GameObject，按名称匹配）
        /// 用途：当ZNetScene和ObjectDB中未找到时，从资源中直接查找（确保兼容性）
        /// </summary>
        /// <param name="name">预制件名称</param>
        /// <returns>找到的预制件GameObject；未找到则返回null</returns>
        static GameObject ResourcesGetGameObject(string name)
        {
            // 获取Resources中所有的GameObject
            var @object = Resources.FindObjectsOfTypeAll<GameObject>();

            // 遍历查找名称匹配的预制件
            foreach (var item in @object)
            {
                if (item.name == name)
                {
                    return item;
                }
            }

            // 未找到时返回null
            return null;
        }
    }
}
