using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Data;
using ValheimCatManager.Mock;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.Tool
{
    /// <summary>
    /// 资源模块管理器（用于从AssetBundle加载并注册自定义资源到游戏，含物品、怪物、植被、配方等）
    /// </summary>
    public class CatResModManager
    {

        private static CatResModManager _instance;
        public static CatResModManager Instance
        {
            get
            {
                // 如果实例还没创建，就创建一个（懒加载：用的时候才创建）
                if (_instance == null)
                {
                    _instance = new CatResModManager();
                }
                return _instance;
            }
        }
        private CatResModManager()
        {
            var harmony = new Harmony("CatManager");
            harmony.PatchAll();
        }

        // 你原有的资源包字段（保留，改为私有更安全）
        private AssetBundle catAsset;

        /// <summary>
        /// 注：加载资源包（传入已加载的AssetBundle实例，初始化资源包）
        /// </summary>
        /// <param name="assetBundle"></param>
        public void LoadAssetBundle(string assetName)
        {
            if (catAsset != null)
            {
          
                Debug.LogError("CatResModManager内的资源包已经有内容，返回。");
                return;
            }

            catAsset = LoadAssetBundleToCatAsset(assetName);
        }



        /// <summary>
        /// 注：从当前程序集（DLL）中加载指定名称的AssetBundle资源包
        /// </summary>
        /// <param name="AssetName">要加载的AssetBundle名称（含后缀，如"catmod.unity3d"）</param>
        /// <returns>加载成功的AssetBundle实例；加载失败（未找到资源/资源流异常）则返回null</returns>
        /// <remarks>
        /// <paramref name="AssetName"/> ：传入资源名 string 类型，需与DLL中嵌入的资源名完全匹配<br/>
        /// 内部逻辑：1. 获取当前执行程序集 → 2. 查找匹配名称的资源 → 3. 读取资源流 → 4. 从流加载AssetBundle
        /// </remarks>
        private AssetBundle LoadAssetBundleToCatAsset(string AssetName)
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
        /// 注：给游戏添加自定义物品（从AssetBundle加载物品预制件，注册到物品字典，可选启用模拟）
        /// </summary>
        /// <param name="itemName">物品预制件名称（需与AssetBundle中的资源名一致）</param>
        /// <param name="mockCheck">是否启用Mock功能（true则将物品加入模拟物品字典，用于后续MockSystem替换）</param>
        public void AddItem(string itemName, bool mockCheck)
        {
            GameObject itemPrefab = catAsset.LoadAsset<GameObject>(itemName);
            if (!itemPrefab)
            {
                Debug.LogError($"执行AddItem时，从资源中未找到 Prefab：{itemName}，已跳过");
                return;
            }
            int hash = itemPrefab.name.GetStableHashCode();
            if (!CatModData.自定义物品_字典.ContainsKey(hash)) CatModData.自定义物品_字典.Add(hash, itemPrefab);
            if (mockCheck) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, itemPrefab.name);
        }

        /// <summary>
        /// 注：给游戏添加自定义配方（将配方配置注册到配方字典）
        /// </summary>
        /// <param name="recipeConfig">配方配置实例（包含物品、材料需求、制作条件等信息）</param>
        public void AddRecipe(RecipeConfig recipeConfig) => CatModData.自定义配方_字典.Add(recipeConfig.物品, recipeConfig);

        /// <summary>
        /// 注：给游戏添加自定义怪物（先注册怪物预制件，再将怪物配置加入怪物列表）
        /// </summary>
        /// <param name="monsterConfig">怪物配置实例（包含预制件名、属性、行为等信息）</param>
        /// <param name="mock">是否启用Mock功能（true则将怪物预制件加入模拟物品字典）</param>
        public void AddMonster(MonsterConfig monsterConfig, bool mock)
        {
            // 注册怪物预制件到预制件字典
            AddPrefab(monsterConfig.预制名, mock);
            // 将怪物配置加入自定义怪物列表
            CatModData.自定义怪物_列表.Add(monsterConfig);
        }

        /// <summary>
        /// 注：给游戏添加自定义植被（加载植被预制件，注册到植被字典和预制件字典，可选启用模拟）
        /// </summary>
        /// <param name="vegetationConfig">植被配置实例（包含预制件名、生成条件等信息）</param>
        /// <param name="mock">是否启用Mock功能（true则将植被预制件加入模拟物品字典）</param>
        public void AddVegetation(VegetationConfig vegetationConfig, bool mock)
        {
            // 从AssetBundle加载植被预制件
            GameObject itemPrefab = catAsset.LoadAsset<GameObject>(vegetationConfig.预制件);
            if (!itemPrefab)
            {
                Debug.LogError($"添加_物品 方法执行时：未有找到：{vegetationConfig.预制件} ");
                return;
            }
            // 生成预制件名的稳定哈希
            int hash = itemPrefab.name.GetStableHashCode();

            // 若植被字典中无该植被，添加到自定义植被字典
            if (!CatModData.自定义植被_字典.ContainsKey(hash)) CatModData.自定义植被_字典.Add(hash, vegetationConfig);
            // 若预制件字典中无该预制件，添加到自定义预制件字典
            if (!CatModData.自定义预制件_字典.ContainsKey(hash)) CatModData.自定义预制件_字典.Add(hash, itemPrefab);
            // 若启用Mock，且模拟物品字典中无该植被，添加到模拟物品字典
            if (mock) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, itemPrefab.name);
        }

        /// <summary>
        /// 注：最常用的预制件注册方法（仅加载预制件并注册到ZNetScene，可选启用模拟）
        /// </summary>
        /// <param name="PrefabName">预制件名称（需与AssetBundle中的资源名一致）</param>
        /// <param name="mock">是否启用Mock功能（true则将预制件加入模拟物品字典）</param>
        public void AddPrefab(string PrefabName, bool mock)
        {
            // 从AssetBundle加载预制件
            GameObject itemPrefab = catAsset.LoadAsset<GameObject>(PrefabName);
            if (!itemPrefab)
            {
                Debug.LogError($"添加预制件 方法执行时：未有找到：{PrefabName} ");
                return;
            }

            // 生成预制件名的稳定哈希
            int hash = itemPrefab.name.GetStableHashCode();

            // 若预制件字典中无该预制件，添加到自定义预制件字典
            if (!CatModData.自定义预制件_字典.ContainsKey(hash)) CatModData.自定义预制件_字典.Add(hash, itemPrefab);
            // 若启用Mock，且模拟物品字典中无该预制件，添加到模拟物品字典
            if (mock) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, itemPrefab.name);
        }

        /// <summary>
        /// 注：给游戏添加自定义生成配置（将生成配置加入生成列表）
        /// </summary>
        /// <param name="spawnConfig">生成配置实例（包含生成位置、频率、怪物/物品等信息）</param>
        public void AddSpawn(SpawnConfig spawnConfig) => CatModData.自定义生成_列表.Add(spawnConfig);

        /// <summary>
        /// 注：添加摆放型食物专用方法（自动配置食物的Piece属性，加载预制件并注册相关字典）
        /// </summary>
        /// <param name="foodName">食物预制件名称（需与AssetBundle中的资源名一致）</param>
        /// <param name="groupName">食物Piece的分组名称（用于制作菜单分类）</param>
        /// <param name="mockCheck">是否启用Mock功能（true则将食物预制件加入模拟物品字典）</param>
        public void AddFood(string foodName, string groupName, bool mockCheck)
        {
            // 从AssetBundle加载食物预制件
            GameObject piecePrefab = catAsset.LoadAsset<GameObject>(foodName);

            if (!piecePrefab)
            {
                Debug.LogError($"执行AddFood时，从资源中未找到 Prefab：{foodName}，已跳过");
                return;
            }

            // 生成预制件名的稳定哈希
            int hash = piecePrefab.name.GetStableHashCode();

            // 若物品字典中无该食物，添加到自定义物品字典
            if (!CatModData.自定义物品_字典.ContainsKey(hash)) CatModData.自定义物品_字典.Add(hash, piecePrefab);

            // 创建食物的Piece配置（设置制作工具为Feaster，分组为指定名称，需求为自身1个）
            PieceConfig pieceConfig = new PieceConfig(foodName);
            pieceConfig.制作工具 = "Feaster";
            pieceConfig.目录 = groupName;
            pieceConfig.AddRequirement(foodName, 1, true);

            // 若物件字典中无该食物配置，添加到自定义物件字典
            if (!CatModData.自定义物件_字典.ContainsKey(hash)) CatModData.自定义物件_字典.Add(hash, pieceConfig);

            // 若启用Mock，且模拟物品字典中无该食物，添加到模拟物品字典
            if (mockCheck) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, piecePrefab.name);
        }

        /// <summary>
        /// 注：添加自定义Piece物件（根据传入的Piece配置加载预制件，注册到预制件和物件字典）
        /// </summary>
        /// <param name="pieceConfig">Piece配置实例（包含预制件名、制作工具、分组、材料需求等信息）</param>
        /// <param name="mockCheck">是否启用Mock功能（true则将Piece预制件加入模拟物品字典）</param>
        public void AddPiece(PieceConfig pieceConfig, bool mockCheck)
        {
            // 从Piece配置中获取预制件名称
            string name = pieceConfig.GetPrefabName();
            // 从AssetBundle加载Piece预制件
            GameObject piecePrefab = catAsset.LoadAsset<GameObject>(name);
            if (!piecePrefab)
            {
                Debug.LogError($"AddPiece 执行时未有找到对应预制件：【{name}】");
                return;
            }

            // 生成预制件名的稳定哈希
            int hash = piecePrefab.name.GetStableHashCode();

            // 若预制件字典中无该Piece预制件，添加到自定义预制件字典
            if (!CatModData.自定义预制件_字典.ContainsKey(hash)) CatModData.自定义预制件_字典.Add(hash, piecePrefab);
            // 若物件字典中无该Piece配置，添加到自定义物件字典
            if (!CatModData.自定义物件_字典.ContainsKey(hash)) CatModData.自定义物件_字典.Add(hash, pieceConfig);

            // 若启用Mock，且模拟物品字典中无该Piece，添加到模拟物品字典
            if (mockCheck) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, piecePrefab.name);
        }

        /// <summary>
        /// 注：添加自定义烹饪站配方（将烹饪站配置加入烹饪站配置列表）
        /// </summary>
        /// <param name="cookingStationConfig">烹饪站配置实例（包含烹饪站类型、可烹饪食物、烹饪时间等信息）</param>
        public void AddCookingStation(CookingStationConfig cookingStationConfig) => CatModData.烹饪站配置_列表.Add(cookingStationConfig);

        /// <summary>
        /// 注：添加自定义炼制站配方（将炼制站配置加入炼制站配置列表）
        /// </summary>
        /// <param name="smeltersConfig">炼制站配置实例（包含炼制站类型、可炼制物品、炼制时间等信息）</param>
        public void AddSmelters(SmeltersConfig smeltersConfig) => CatModData.炼制站配置_列表.Add(smeltersConfig);
    }


    /// <summary>
    /// 注：对ObjectDB.Awake方法的Postfix补丁（优先级0，早于配方注册），主场景加载时注册基础资源
    /// 功能：加载着色器缓存、注册物品/预制件到ZNetScene、注册物品到ObjectDB、注册怪物配置
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    [HarmonyPriority(0)]
    class AddItemsObjectDBPatch
    {
        static void Postfix(ObjectDB __instance)
        {
            // 仅在主场景（"main"）执行（避免非游戏场景误触发）
            if (SceneManager.GetActiveScene().name == "main")
            {
                CatToolManager.RegisterToObjectDB(__instance, CatModData.自定义物品_字典); // 注册自定义物品到ObjectDB
                CatToolManager.RegisterMonsterConfig(CatModData.自定义怪物_列表); // 注册自定义怪物配置
            }
        }
    }


    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    [HarmonyPriority(0)]
    class AddItemsZNetScenePatch
    {
        static void Postfix(ZNetScene __instance)
        {
            // 仅在主场景（"main"）执行（避免非游戏场景误触发）
            if (SceneManager.GetActiveScene().name == "main")
            {
                CatToolManager.RegisterToZNetScene(__instance, CatModData.自定义物品_字典); // 注册自定义物品到ZNetScene
                CatToolManager.RegisterToZNetScene(__instance, CatModData.自定义预制件_字典); // 注册自定义预制件到ZNetScene
            }
        }
    }

    /// <summary>
    /// 注：对ObjectDB.Awake方法的Postfix补丁（优先级1，晚于物品注册），主场景加载时注册配方类资源
    /// 功能：注册自定义配方、烹饪站配置、炼制站配置（确保物品先注册再注册依赖物品的配方）
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    [HarmonyPriority(1)]
    class AddRecipePatch
    {
        static void Postfix(ObjectDB __instance)
        {
            // 仅在主场景（"main"）执行
            if (SceneManager.GetActiveScene().name == "main")
            {
                CatToolManager.RegisterRecipe(__instance, CatModData.自定义配方_字典); // 注册自定义配方到ObjectDB
                CatToolManager.RegisterCookingStationConfig(CatModData.烹饪站配置_列表); // 注册自定义烹饪站配置
                CatToolManager.RegisterSmeltersConfig(CatModData.炼制站配置_列表); // 注册自定义炼制站配置
            }
        }
    }


    /// <summary>
    /// 注：对ZoneSystem.Start方法的Postfix补丁（优先级First，最早执行），主场景加载时执行Mock替换
    /// 功能：启动MockSystem替换占位资源，清理预制件缓存，统计替换耗时
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    [HarmonyPriority(Priority.First)]
    class MockePatch
    {
        static void Postfix(ObjectDB __instance)
        {
            // 仅在主场景（"main"）执行
            if (SceneManager.GetActiveScene().name == "main")
            {
                var startTime1 = DateTime.Now; // 记录开始时间（用于统计耗时）

                MockSystem.StartMockReplacement(); // 执行占位预制件/着色器替换
                CatModData.m_PrefabCache.Clear(); // 清理预制件缓存，释放资源

                var elapsed1 = DateTime.Now - startTime1; // 计算耗时
                Debug.LogError($"mock 完成耗时: {elapsed1.TotalMilliseconds / 1000}秒"); // 打印耗时日志
            }
        }
    }


    /// <summary>
    /// 注：对ObjectDB.CopyOtherDB方法的Prefix补丁（优先级-101，极低优先级确保先执行），同步自定义物品到目标DB
    /// 关键逻辑：将自定义物品注册到「目标ObjectDB（other）」，因CopyOtherDB会将other的数据复制到__instance
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    [HarmonyPriority(-101)]
    class ItemsCopyOtherDB
    {
        static void Prefix(ObjectDB __instance, ObjectDB other)
        {
            // 若目标DB（other）为空，或无自定义物品，直接返回
            if (other == null) return;
            if (CatModData.自定义物品_字典 == null || CatModData.自定义物品_字典.Count == 0) return;

            // 关键：将自定义物品注册到目标DB（other），确保后续复制时能同步到__instance
            CatToolManager.RegisterToObjectDB(other, CatModData.自定义物品_字典);
        }
    }


    /// <summary>
    /// 注：对SpawnSystem.Awake方法的Postfix补丁（优先级VeryLow，晚执行），注册自定义生成配置
    /// 功能：将自定义生成列表（怪物/物品生成）注册到生成系统
    /// </summary>
    [HarmonyPatch(typeof(SpawnSystem), "Awake")]
    [HarmonyPriority(Priority.VeryLow)]
    class SpawnPatch
    {
        // 注册自定义生成配置到SpawnSystem
        static void Postfix(SpawnSystem __instance) => CatToolManager.RegisterSpawnList(__instance);
    }


    /// <summary>
    /// 注：对ZoneSystem.SetupLocations方法的Postfix补丁（优先级0），注册自定义植被到区域系统
    /// 功能：将自定义植被配置添加到区域系统，确保植被能在对应区域生成
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "SetupLocations")]
    [HarmonyPriority(0)]
    class ZoneSystemPatch
    {
        static void Postfix(ZoneSystem __instance)
        {
            CatToolManager.RegisterVegetation(CatModData.自定义植被_字典, __instance); // 注册自定义植被到ZoneSystem
        }
    }
}
