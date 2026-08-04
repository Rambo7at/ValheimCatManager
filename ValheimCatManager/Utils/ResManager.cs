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
using ValheimCatManager.Config;
using ValheimCatManager.Managers;
using ValheimCatManager.Mock;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.Utils;

/// <summary>注：资源模块管理器，统一加载AB资源并注册自定义物品、怪物、建筑、配方等游戏资源</summary>
public class ResManager
{
    private static ResManager _instance;
    /// <summary>注：单例全局实例，懒加载模式</summary>
    public static ResManager Instance
    {
        get
        {
            // 如果实例还没创建，就创建一个（懒加载：用的时候才创建）
            if (_instance == null)
            {
                _instance = new ResManager();
            }
            return _instance;
        }
    }
    private ResManager() { }

    private Dictionary<string, AssetBundle> AssetBundleDict = [];

    /// <summary>注：加载模组AB资源包，建立模组与资源包映射</summary>
    public void LoadAssetBundle(string assetName)
    {
        var mod = Assembly.GetCallingAssembly();
        var modName = mod.GetName().Name;

        if (AssetBundleDict.TryGetValue(modName, out var _)) return;

        AssetBundleDict[modName] = LoadAssetBundleToCatAsset(assetName, mod);
    }

    /// <summary>注：从嵌入程序流读取并加载AB包</summary>
    private AssetBundle LoadAssetBundleToCatAsset(string assetName, Assembly resourceAssembly)
    {
        // 从指定程序集中查找名称以 assetName 结尾的资源
        string resourceName = Array.Find(resourceAssembly.GetManifestResourceNames(), name => name.EndsWith(assetName));

        if (string.IsNullOrEmpty(resourceName))
        {
            Debug.LogError($"在程序集 [{resourceAssembly.GetName().Name}] 中未找到资源包 [{assetName}]");
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

    /// <summary>注：根据模组名读取已缓存的AB包</summary>
    private AssetBundle GetAssetBundle(string modName)
    {
        if (!AssetBundleDict.TryGetValue(modName, out var ab))
        {
            Debug.LogError($"未找到模组 [{modName}] 的资源包，请确保已调用 LoadAssetBundle");
            return null;
        }

        return ab;
    }

    /// <summary>注：加载物品预制件，注册至自定义物品字典，可选加入模拟替换字典</summary>
    public void AddItem(string itemName, bool mockCheck)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;

        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        GameObject itemPrefab = ab.LoadAsset<GameObject>(itemName);
        if (!itemPrefab)
        {
            Debug.LogError($"执行AddItem时，从资源中未找到 Prefab：{itemName}，已跳过");
            return;
        }
        int hash = itemPrefab.name.GetStableHashCode();
        if (!PrefabManager.Instance.customItemDict.ContainsKey(hash)) PrefabManager.Instance.customItemDict.Add(hash, itemPrefab);
        if (mockCheck) if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, itemPrefab.name);
    }

    /// <summary>注：注册自定义配方配置</summary>
    public void AddRecipe(RecipeConfig recipeConfig) => RecipeManager.Instance.customRecipeDict.Add(recipeConfig.物品, recipeConfig);

    /// <summary>注：重载快速构建配方并注册</summary>
    public void AddRecipe(string itemName, string station, int stationLevel, int amount, params (string resItem, int resAmount, int levelAmount)[] resItemList)
    {
        RecipeConfig recipeConfig = new RecipeConfig(itemName, station, stationLevel, amount, resItemList);

        RecipeManager.Instance.customRecipeDict.Add(recipeConfig.物品, recipeConfig);
    }

    /// <summary>注：加载怪物预制件，注册怪物预制件与怪物配置，可选开启模拟</summary>
    public void AddMonster(MonsterConfig monsterConfig, bool mock)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        GameObject itemPrefab = ab.LoadAsset<GameObject>(monsterConfig.预制名);
        if (!itemPrefab)
        {
            Debug.LogError($"添加预制件 方法执行时：未有找到：{monsterConfig.预制名} ");
            return;
        }
        // 注册怪物预制件到预制件字典
        AddPrefab(itemPrefab);
        if (monsterConfig.食谱.Length > 0)
        {
            // 将怪物配置加入自定义怪物列表
            MonsterManager.Instance.customMonsterSet.Add(monsterConfig);
        }
    }

    /// <summary>注：加载植被预制件，注册植被与预制字典，可选加入模拟字典</summary>
    public void AddVegetation(VegetationConfig vegetationConfig, bool mock)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        // 从AssetBundle加载植被预制件
        GameObject itemPrefab = ab.LoadAsset<GameObject>(vegetationConfig.预制件);
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

    /// <summary>注：加载预制件并注册到预制字典，可选择加入模拟字典</summary>
    public void AddPrefab(string PrefabName, bool mock)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        GameObject itemPrefab = ab.LoadAsset<GameObject>(PrefabName);
        if (!itemPrefab)
        {
            Debug.LogError($"添加预制件 方法执行时：未有找到：{PrefabName} ");
            return;
        }

        int hash = itemPrefab.name.GetStableHashCode();

        if (!PrefabManager.Instance.customPrefabDict.ContainsKey(hash)) PrefabManager.Instance.customPrefabDict.Add(hash, itemPrefab);
        if (mock) if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, itemPrefab.name);
    }

    /// <summary>注：直接传入GameObject对象快速注册预制件</summary>
    private void AddPrefab(GameObject gameObject)
    {
        int hash = gameObject.name.GetStableHashCode();

        if (!PrefabManager.Instance.customPrefabDict.ContainsKey(hash)) PrefabManager.Instance.customLocationDict.Add(hash, gameObject);
        if (!MockSystem.Instance.mockPrefabDict.ContainsKey(hash)) MockSystem.Instance.mockPrefabDict.Add(hash, gameObject.name);
    }

    /// <summary>注：注册自定义生物生成配置</summary>
    public void AddSpawn(SpawnConfig spawnConfig) => SpawnManager.Instance.customSpawn.Add(spawnConfig);

    /// <summary>注：加载摆放食物预制件，自动配置建造属性并注册相关字典</summary>
    public void AddFood(string foodName, string groupName, bool mockCheck)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        // 从AssetBundle加载食物预制件
        GameObject piecePrefab = ab.LoadAsset<GameObject>(foodName);

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

    /// <summary>注：根据配置加载建筑预制件，注册建筑与预制字典，可选开启模拟</summary>
    public void AddPiece(PieceConfig pieceConfig, bool mockCheck)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        string name = pieceConfig.GetPrefabName();
        GameObject piecePrefab = ab.LoadAsset<GameObject>(name);
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

    /// <summary>注：注册自定义烹饪站配置</summary>
    public void AddCookingStation(CookingStationConfig cookingStationConfig) => CookingStationManager.Instance.customCookingStation.Add(cookingStationConfig);

    /// <summary>注：注册自定义炼制站配置</summary>
    public void AddSmelters(SmeltersConfig smeltersConfig) => SmeltersManger.Instance.customSmelters.Add(smeltersConfig);

    /// <summary>注：加载地点预制件，注册地点配置与预制件</summary>
    public void AddLocation(string LocationName, LocationConfig locationConfig)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        GameObject LocationPrefab = ab.LoadAsset<GameObject>(LocationName);
        if (!LocationPrefab)
        {
            Debug.LogError($"执行AddLocation方法执行时：未找到预制件：[{LocationName}] ");
            return;
        }

        Instance.AddPrefab(LocationPrefab);
        locationConfig.预制件 = LocationPrefab;

        LocationManager.Instance.customLocationList.Add(locationConfig);
    }

    /// <summary>注：加载房间预制件，注册地下城房间配置</summary>
    public void AddRoom(string roomName, string themeName)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        GameObject roomPrefab = ab.LoadAsset<GameObject>(roomName);
        if (!roomPrefab)
        {
            Debug.LogError($"执行AddRoom方法执行时：未找到预制件：[{roomName}] ");
            return;
        }

        RoomConfig roomConfig = new();

        Instance.AddPrefab(roomPrefab);

        roomConfig.预制件 = roomPrefab;
        roomConfig.主题 = themeName;

        DungeonManager.Instance.roomList.Add(roomConfig);

    }

    /// <summary>注：加载地牢预制件，注册地牢主题与野外地点配置</summary>
    public void AddDungeon(string locationName, string dungeonTheme, LocationConfig locationConfig)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        GameObject LocationPrefab = ab.LoadAsset<GameObject>(locationName);
        if (!LocationPrefab)
        {
            Debug.LogError($"执行AddDungeon方法执行时：未找到预制件：[{locationName}] ");
            return;
        }
        if (string.IsNullOrEmpty(dungeonTheme))
        {
            Debug.LogError($"执行AddDungeon方法执行时地下城主题名为空！ ");
            return;
        }

        DungeonManager.Instance.RegisterDungeonTheme(LocationPrefab, dungeonTheme);

        Instance.AddPrefab(LocationPrefab);
        locationConfig.预制件 = LocationPrefab;

        LocationManager.Instance.customLocationList.Add(locationConfig);
    }

    /// <summary>注：加载图标贴图，创建精灵并注册地图地点图标</summary>
    public void AddLocationIcon(string iconName, string locationIconName)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        Texture2D texture2D = ab.LoadAsset<Texture2D>(iconName);
        if (!texture2D)
        {
            Debug.Log($"AddLocationIcon,执行时未有找到对应图片！");
            return;
        }

        var sprite = Sprite.Create(texture2D, new Rect(0, 0, 64, 64), Vector2.zero);
        if (!sprite)
        {
            Debug.Log($"AddLocationIcon,执行 Sprite.Create 对象为空！");
            return;
        }

        LocationIconManager.Instance.customLocationIconDict.Add(locationIconName, sprite);
    }

    /// <summary>注：加载动画片段，注册多段动画并加入动画组列表</summary>
    public void AddAnimation(string animationName1, string animationName2 = null, string animationName3 = null)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        List<string> attacklist = new List<string>();

        AnimationClip attack1 = ab.LoadAsset<AnimationClip>(animationName1);
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
            AnimationClip attack2 = ab.LoadAsset<AnimationClip>(animationName2);
            if (attack2 == null)
            {
                Debug.LogError($"[CatResModManager.AddAnimation] 执行失败：未有找找到动画 [{animationName2}]（检查资源）");
                return;
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
            AnimationClip attack3 = ab.LoadAsset<AnimationClip>(animationName3);
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

    /// <summary>注：加载状态效果资源，注册自定义buff效果</summary>
    public void AddStatusEffect(string seName)
    {
        var modName = Assembly.GetCallingAssembly().GetName().Name;
        if (GetAssetBundle(modName) is not AssetBundle ab) return;

        StatusEffect statusEffect = ab.LoadAsset<StatusEffect>(seName);
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
}

