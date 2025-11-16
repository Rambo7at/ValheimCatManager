using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValheimCatManager;
using ValheimCatManager.ValheimCatManager.Config;
using ValheimCatManager.ValheimCatManager.Managers;
using ValheimCatManager.ValheimCatManager.Mock;
using ValheimCatManager.ValheimCatManager.Tool;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.ValheimCatManager.Tool
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

        }

        // 资源包字段
        private AssetBundle catAsset;

        /// <summary>
        /// 注：（必要步骤）资源包加载 与 mock系统命名
        /// </summary>
        /// <param name="assetName">AB包名称</param>
        /// <param name="PluginName">mod名</param>
        public void LoadAssetBundle(string assetName, string PluginName)
        {
            if (catAsset != null)
            {

                Debug.LogError("CatResModManager内的资源包已经有内容，返回。");
                return;
            }

            catAsset = LoadAssetBundleToCatAsset(assetName);
            MockSystem.Instance.LoadmockDebugName(PluginName);
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
            if (!PrefabManager.Instance.customItemDict.ContainsKey(hash)) PrefabManager.Instance.customItemDict.Add(hash, itemPrefab);
            if (mockCheck) if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, itemPrefab.name);
        }

        /// <summary>
        /// 注：添加游戏自定义配方（将配方配置注册到配方字典）
        /// </summary>
        /// <param name="recipeConfig">配方配置实例（包含物品、材料需求、制作条件等信息）</param>
        public void AddRecipe(RecipeConfig recipeConfig) => RecipeManager.Instance.customRecipeDict.Add(recipeConfig.物品, recipeConfig);

        /// <summary>
        /// 注：(重载)添加游戏自定义配方，简化：步骤实例化
        /// </summary>
        /// <param name="recipeConfig">配方配置实例（包含物品、材料需求、制作条件等信息）</param>
        public void AddRecipe(string itemName, string station, int stationLevel, int amount, params (string resItem, int resAmount, int levelAmount)[] resItemList)
        {
            RecipeConfig recipeConfig = new RecipeConfig(itemName, station, stationLevel, amount, resItemList);

            RecipeManager.Instance.customRecipeDict.Add(recipeConfig.物品, recipeConfig);
        }

        /// <summary>
        /// 注：给游戏添加自定义怪物（先注册怪物预制件，再将怪物配置加入怪物列表）
        /// </summary>
        /// <param name="monsterConfig">怪物配置实例（包含预制件名、属性、行为等信息）</param>
        /// <param name="mock">是否启用Mock功能（true则将怪物预制件加入模拟物品字典）</param>
        public void AddMonster(MonsterConfig monsterConfig, bool mock)
        {
            // 注册怪物预制件到预制件字典
            AddPrefab(monsterConfig.预制名, mock);
            if (monsterConfig.食谱.Length > 0)
            {
                // 将怪物配置加入自定义怪物列表
                MonsterManager.Instance.customMonsterSet.Add(monsterConfig);
            }

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
            if (!VegetationManager.Instance.customVegetationDict.ContainsKey(hash)) VegetationManager.Instance.customVegetationDict.Add(hash, vegetationConfig);
            // 若预制件字典中无该预制件，添加到自定义预制件字典
            if (!PrefabManager.Instance.customPrefabDict.ContainsKey(hash)) PrefabManager.Instance.customPrefabDict.Add(hash, itemPrefab);
            // 若启用Mock，且模拟物品字典中无该植被，添加到模拟物品字典
            if (mock) if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, itemPrefab.name);
        }

        /// <summary>
        /// 注：最常用的预制件注册方法（仅加载预制件并注册到ZNetScene，可选启用模拟）
        /// </summary>
        /// <param name="PrefabName">预制件名称（需与AssetBundle中的资源名一致）</param>
        /// <param name="mock">是否启用Mock功能（true则将预制件加入模拟物品字典）</param>
        public void AddPrefab(string PrefabName, bool mock)
        {
            GameObject itemPrefab = catAsset.LoadAsset<GameObject>(PrefabName);
            if (!itemPrefab)
            {
                Debug.LogError($"添加预制件 方法执行时：未有找到：{PrefabName} ");
                return;
            }

            int hash = itemPrefab.name.GetStableHashCode();

            if (!PrefabManager.Instance.customPrefabDict.ContainsKey(hash)) PrefabManager.Instance.customPrefabDict.Add(hash, itemPrefab);
            if (mock) if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, itemPrefab.name);
        }

        /// <summary>
        /// 辅助方法：针对地点预制件注册
        /// </summary>
        /// <param name="gameObject"></param>
        private void AddPrefab(GameObject gameObject)
        {

            int hash = gameObject.name.GetStableHashCode();

            if (!PrefabManager.Instance.customPrefabDict.ContainsKey(hash)) PrefabManager.Instance.customLocationDict.Add(hash, gameObject);
            if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, gameObject.name);
        }


        /// <summary>
        /// 注：给游戏添加自定义生成配置（将生成配置加入生成列表）
        /// </summary>
        /// <param name="spawnConfig">生成配置实例（包含生成位置、频率、怪物/物品等信息）</param>
        public void AddSpawn(SpawnConfig spawnConfig) => SpawnManager.Instance.customSpawn.Add(spawnConfig);

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
            if (!PrefabManager.Instance.customItemDict.ContainsKey(hash)) PrefabManager.Instance.customItemDict.Add(hash, piecePrefab);

            // 创建食物的Piece配置（设置制作工具为Feaster，分组为指定名称，需求为自身1个）
            PieceConfig pieceConfig = new PieceConfig(foodName);
            pieceConfig.制作工具 = "Feaster";
            pieceConfig.目录 = groupName;
            pieceConfig.AddRequirement(foodName, 1, true);

            // 若物件字典中无该食物配置，添加到自定义物件字典
            if (!PieceManager.Instance.customPieceDict.ContainsKey(hash)) PieceManager.Instance.customPieceDict.Add(hash, pieceConfig);

            // 若启用Mock，且模拟物品字典中无该食物，添加到模拟物品字典
            if (mockCheck) if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, piecePrefab.name);
        }

        /// <summary>
        /// 注：添加自定义Piece物件（根据传入的Piece配置加载预制件，注册到预制件和物件字典）
        /// </summary>
        /// <param name="pieceConfig">Piece配置实例（包含预制件名、制作工具、分组、材料需求等信息）</param>
        /// <param name="mockCheck">是否启用Mock功能（true则将Piece预制件加入模拟物品字典）</param>
        public void AddPiece(PieceConfig pieceConfig, bool mockCheck)
        {
            string name = pieceConfig.GetPrefabName();
            GameObject piecePrefab = catAsset.LoadAsset<GameObject>(name);
            if (!piecePrefab)
            {
                Debug.LogError($"AddPiece 执行时未有找到对应预制件：【{name}】");
                return;
            }

            int hash = piecePrefab.name.GetStableHashCode();

            if (!PrefabManager.Instance.customPrefabDict.ContainsKey(hash)) PrefabManager.Instance.customPrefabDict.Add(hash, piecePrefab);

            if (!PieceManager.Instance.customPieceDict.ContainsKey(hash)) PieceManager.Instance.customPieceDict.Add(hash, pieceConfig);

            if (mockCheck) if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, piecePrefab.name);
        }

        /// <summary>
        /// 注：将烹饪站配置加入烹饪站配置列表
        /// </summary>
        /// <param name="cookingStationConfig">烹饪站配置实例（包含烹饪站类型、可烹饪食物、烹饪时间等信息）</param>
        public void AddCookingStation(CookingStationConfig cookingStationConfig) => CookingStationManager.Instance.customCookingStation.Add(cookingStationConfig);

        /// <summary>
        /// 注：将炼制站配置加入炼制站配置列表
        /// </summary>
        /// <param name="smeltersConfig">炼制站配置实例（包含炼制站类型、可炼制物品、炼制时间等信息）</param>
        public void AddSmelters(SmeltersConfig smeltersConfig) => SmeltersManger.Instance.customSmelters.Add(smeltersConfig);

        /// <summary>
        /// 注：给游戏添加自定义地点
        /// </summary>
        /// <param name="LocationName">地点名</param>
        /// <param name="locationConfig">设置地点生成</param>
        public void AddLocation(string LocationName, LocationConfig locationConfig)
        {

            GameObject LocationPrefab = catAsset.LoadAsset<GameObject>(LocationName);
            if (!LocationPrefab)
            {
                Debug.LogError($"执行AddLocation方法执行时：未找到预制件：[{LocationName}] ");
                return;
            }

            Instance.AddPrefab(LocationPrefab);
            locationConfig.预制件 = LocationPrefab;

            LocationManager.Instance.customLocationList.Add(locationConfig);
        }

        /// <summary>
        /// 注：给游戏添加地下城房间
        /// </summary>
        public void AddRoom(string roomName,string themeName)
        {
            GameObject roomPrefab = catAsset.LoadAsset<GameObject>(roomName);
            if (!roomPrefab)
            {
                Debug.LogError($"执行AddRoom方法执行时：未找到预制件：[{roomName}] ");
                return;
            }

            RoomConfig roomConfig = new();

            Instance.AddPrefab(roomPrefab);

            roomConfig.预制件 = roomPrefab;

          
            roomConfig.主题 = themeName;


            if (!RoomThemeManger.Instance.roomThemeList.Contains(themeName)) RoomThemeManger.Instance.roomThemeList.Add(themeName);


            DungeonManager.Instance.roomList.Add(roomConfig);

        }


        /// <summary>
        /// 注：将自定义地区图标加入游戏
        /// </summary>
        /// <param name="iconName">图标名</param>
        public void AddLocationIcon(string iconName, string locationIconName)
        {
            Texture2D texture2D = catAsset.LoadAsset<Texture2D>(iconName);
            if (!texture2D) Debug.Log($"AddLocationIcon,执行时未有找到对应图片！");

            var sprite = Sprite.Create(texture2D, new Rect(0, 0, 64, 64), Vector2.zero);
            if (!sprite) Debug.Log($"AddLocationIcon,执行 Sprite.Create 对象为空！");

            LocationIconManager.Instance.customLocationIconDict.Add(locationIconName, sprite);

        }





        /// <summary>
        /// 注：给游戏添加动作
        /// </summary>
        /// <param name="animationName"></param>
        public void AddAnimation(string animationName1, string animationName2 = null, string animationName3 = null)
        {
            List<string> attacklist = new List<string>();

            AnimationClip attack1 = catAsset.LoadAsset<AnimationClip>(animationName1);
            if (attack1 == null)
            {
                Debug.LogError($"[CatResModManager.AddAnimation] 执行失败：未有找找到动画 [{animationName1}]（检查资源）");
                return;
            }
            if (AnimationManager.Instance.animationDict.ContainsKey(animationName1))
            {
                Debug.LogWarning($"动画 {animationName1} 已存在，将覆盖原有片段");
                AnimationManager.Instance.animationDict[animationName1] = attack1;
            }
            else
            {
                AnimationManager.Instance.animationDict.Add(animationName1, attack1);
                attacklist.Add(animationName1);
            }

            if (animationName2 != null)
            {
                AnimationClip attack2 = catAsset.LoadAsset<AnimationClip>(animationName2);
                if (attack2 == null)
                {
                    Debug.LogError($"[CatResModManager.AddAnimation] 执行失败：未有找找到动画 [{animationName2}]（检查资源）");
                    return; // 若需要允许部分失败，可改为continue，但建议严格校验
                }
                if (AnimationManager.Instance.animationDict.ContainsKey(animationName2))
                {
                    Debug.LogWarning($"动画 {animationName2} 已存在，将覆盖原有片段");
                    AnimationManager.Instance.animationDict[animationName2] = attack2;
                }
                else
                {
                    AnimationManager.Instance.animationDict.Add(animationName2, attack2);
                }
                attacklist.Add(animationName2);
            }

            if (animationName3 != null)
            {
                AnimationClip attack3 = catAsset.LoadAsset<AnimationClip>(animationName3);
                if (attack3 == null)
                {
                    Debug.LogError($"[CatResModManager.AddAnimation] 执行失败：未有找找到动画 [{animationName3}]（检查资源）");
                    return; 
                }
                if (AnimationManager.Instance.animationDict.ContainsKey(animationName3))
                {
                    Debug.LogWarning($"动画 {animationName3} 已存在，将覆盖原有片段");
                    AnimationManager.Instance.animationDict[animationName3] = attack3;
                }
                else
                {
                    AnimationManager.Instance.animationDict.Add(animationName3, attack3);
                }
                attacklist.Add(animationName3);
            }

            AnimationManager.Instance.animationList.Add(attacklist);
        }






        /// <summary>
        /// 注：将自定义效果添加给游戏
        /// </summary>
        public void AddStatusEffect(string seName)
        {
            StatusEffect statusEffect = catAsset.LoadAsset<StatusEffect>(seName);
            if (!statusEffect)
            {
                Debug.LogError($"AddStatusEffect,执行时未找到对于效果：[{seName}]");
                return;
            }
            if (!StatusEffectManager.Instance.customStatusEffectDict.ContainsKey(seName))
            {
                StatusEffectManager.Instance.customStatusEffectDict.Add(seName, statusEffect);
                return;
            }
            Debug.LogError($"AddStatusEffect,执行时发现重复效果：[{seName}]");


        }


        /// <summary>
        /// 注：RPC网络版本检测
        /// </summary>
        /// <param name="version"></param>
        /// <param name="rpcName"></param>
        public void EnableVersionCheck(string version, string rpcName) => RpcVersionCheckManager.Instance.EnableVersionCheck(version, rpcName);

    }


}
