using HarmonyLib;
using SoftReferenceableAssets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using ValheimCatManager;
using ValheimCatManager.Data;
using static Heightmap;
using Object = UnityEngine.Object;


namespace ValheimCatManager.CatUtils;

/// <summary>注：通用工具静态类</summary>
public static class CatTool
{
    /// <summary>注：根据生物群系名称获取Heightmap.Biome枚举</summary>
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
        Debug.LogError($"[CatUtil.GetBiome] 未匹配生物群系枚举：{biomeName}");
        return Heightmap.Biome.None;
    }

    /// <summary>注：根据名称匹配获取Room.Theme枚举</summary>
    public static Room.Theme GetTheme(string themeName)
    {
        // 使用已经被补丁的 GetNames 和 GetValues
        string[] names = Enum.GetNames(typeof(Room.Theme));
        Array values = Enum.GetValues(typeof(Room.Theme));

        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == themeName)
            {
                return (Room.Theme)values.GetValue(i);
            }
        }
        Debug.LogError($"[CatUtil.GetTheme] 未匹配主题枚举：{themeName}");
        return Room.Theme.None;
    }

    /// <summary>注：打印指定枚举全部名称与数值信息</summary>
    public static void GetEnumInfo<T>() where T : Enum
    {
        string[] names = Enum.GetNames(typeof(T));
        Array values = Enum.GetValues(typeof(T));

        for (int i = 0; i < names.Length; i++)
        {
            object value = values.GetValue(i);
            int intValue = Convert.ToInt32(value);
            Debug.LogError($"[CatUtil.GetEnumInfo] 枚举名:[{names[i]}] 整型值:[{intValue}]");
        }
    }

    /// <summary>注：检测字符串是否为目标枚举合法名称</summary>
    public static bool CheckEunm<T>(string EnumName) where T : Enum
    {
        foreach (var Eunm in Enum.GetValues(typeof(T)))
        {
            if (Enum.GetName(typeof(T), Eunm) == EnumName) return true;
        }

        return false;
    }

    /// <summary>注：获取枚举成员总数量</summary>
    public static int GetEnumLength<T>() where T : Enum => Enum.GetValues(typeof(T)).Length;

    /// <summary>注：按名称查找着色器并写入缓存，重复查询直接读取缓存</summary>
    public static Shader GetShader(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError($"[CatUtil.GetShader] 传入着色器名称为空");
            return null;
        }

        if (CatModData.m_shaderCache.TryGetValue(name, out Shader shader)) return shader;

        List<Shader> shaderList = [];
        Shader[] Shaders = Resources.FindObjectsOfTypeAll<Shader>();

        foreach (var Shaderx in Shaders)
        {
            if (Shaderx == null) continue;
            if (Shaderx.name == name)
            {
                shaderList.Add(Shaderx);
            }
        }

        if (shaderList.Count != 0)
        {
            CatModData.m_shaderCache.Add(shaderList[shaderList.Count - 1].name, shaderList[shaderList.Count - 1]);
            return shaderList[0];
        }
        Debug.LogError($"[CatUtil.GetShader] 未找到着色器：{name}");
        return null;
    }

    /// <summary>注：按名称查找材质并缓存，逻辑与着色器查询保持统一</summary>
    public static Material GetMaterial(string name)
    {
        // 1. 空值校验（和GetShader逻辑一致）
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError($"[CatUtil.GetMaterial] 传入材质名称为空");
            return null;
        }

        // 2. 先查缓存，命中直接返回（避免重复查找，提升性能）
        if (CatModData.m_materialCache.TryGetValue(name, out Material material)) return material;


        // 3. 全局查找所有已加载的材质（复用GetShader的Resources.FindObjectsOfTypeAll逻辑）
        List<Material> materialList = [];

        var allMaterials = Resources.FindObjectsOfTypeAll<Material>();

        foreach (var materialData in allMaterials)
        {
            if (materialData == null) continue;
            if (materialData.name == name)
            {
                materialList.Add(materialData);
            }
        }

        // 4. 处理查找结果：有匹配项则缓存并返回，无则返回null
        if (materialList.Count > 0)
        {
            // 缓存最后一个匹配项（和GetShader的缓存逻辑保持一致，兼容你的设计）
            var targetMaterial = materialList[materialList.Count - 1];
            CatModData.m_materialCache.Add(targetMaterial.name, targetMaterial);
            // 返回第一个匹配项（和GetShader返回逻辑一致，保持统一）
            return materialList[0];
        }
        // 未找到材质时输出日志（方便调试，和GetShader的错误反馈一致）
        Debug.LogWarning($"[CatUtil.GetMaterial] 未找到名称为「{name}」的材质");
        return null;
    }

    /// <summary>注：打印Piece.PieceCategory枚举全部键值，用于调试校验</summary>
    public static void GetPieceCategory()
    {
        // 获取枚举的所有值和名称
        Array enumValues = Enum.GetValues(typeof(Piece.PieceCategory));
        string[] enumNames = Enum.GetNames(typeof(Piece.PieceCategory));

        Debug.LogError($"[CatUtil.GetPieceCategory] 枚举总长度：{enumValues.Length}");
        // 枚举值和名称长度一致时，打印每个枚举的键值对
        if (enumValues.Length == enumNames.Length)
        {
            for (int i = 0; i < enumValues.Length; i++)
            {
                Debug.LogError($"[CatUtil.GetPieceCategory] 键-{enumNames[i]}；值-{(Piece.PieceCategory)enumValues.GetValue(i)}");
            }
        }
    }

    /// <summary>注：按名称分级查找预制件并缓存，优先级ZNetScene→ObjectDB→Resources兜底</summary>
    public static GameObject GetGameObject(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError($"[CatUtil.GetGameObject] 传入预制件名称为空");
            return null;
        }

        if (CatModData.m_PrefabCache.TryGetValue(name, out var obj)) return obj;

        GameObject itemPrefab = ZNetScene.instance.GetPrefab(name) ?? ObjectDB.instance.GetItemPrefab(name) ?? ResourcesGetGameObject(name);

        if (itemPrefab == null)
        {
            Debug.LogError($"[CatUtil.GetGameObject] 未查询到注册预制件[{name}]");
            return null;
        }

        CatModData.m_PrefabCache[itemPrefab.name] = itemPrefab;
        return itemPrefab;
    }

    /// <summary>注：按哈希值查找预制件并缓存，优先级ZNetScene→ObjectDB</summary>
    public static GameObject GetGameObject(int hash)
    {
        GameObject itemPrefab = ZNetScene.instance.GetPrefab(hash) ?? ObjectDB.instance.GetItemPrefab(hash);

        if (itemPrefab == null)
        {
            Debug.LogError($"[CatUtil.GetGameObject] 未查询到注册预制件，哈希值：[{hash}]");
            return null;
        }

        CatModData.m_PrefabCache[itemPrefab.name] = itemPrefab;
        return itemPrefab;
    }

    /// <summary>注：Resources兜底全局查找GameObject</summary>
    private static GameObject ResourcesGetGameObject(string name)
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

    /// <summary>注：根据对象实例ID生成AssetID</summary>
    public static AssetID AssetIDFromObject(Object obj)
    {
        int id = obj.GetInstanceID();
        return new AssetID(1, 1, 1, (uint)id);
    }

    /// <summary>注：注册外部加载对象到AssetBundleLoader，生成可用软引用</summary>
    public static SoftReference<T> AddLoadedSoftReferenceAsset<T>(T obj) where T : Object
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "不能为null对象创建SoftReference");
        }

        AssetBundleLoader bundleLoader = AssetBundleLoader.Instance;
        if (bundleLoader == null)
        {
            throw new InvalidOperationException("AssetBundleLoader实例为空，无法注册资源");
        }

        // 确保有一个空的加载器索引（防止系统崩溃）
        if (!bundleLoader.m_bundleNameToLoaderIndex.ContainsKey(""))
        {
            bundleLoader.m_bundleNameToLoaderIndex[""] = 0;
        }

        // 基于对象InstanceID生成唯一AssetID
        AssetID id = AssetIDFromObject(obj);

        // 创建AssetLoader包装器，配置资源引用和加载状态
        AssetLoader loader = new AssetLoader(id, new AssetLocation("", ""))
        {
            m_asset = obj,
            m_referenceCounter = new ReferenceCounter(2),
            m_shouldBeLoaded = true,
        };

        // 扩展加载器数组容量（如需）
        int count = bundleLoader.m_assetIDToLoaderIndex.Count;
        if (count >= bundleLoader.m_assetLoaders.Length)
        {
            Array.Resize(ref bundleLoader.m_assetLoaders, bundleLoader.m_assetIDToLoaderIndex.Count + 256);
        }

        // 注册加载器到系统：数组存储 + 字典索引映射
        bundleLoader.m_assetLoaders[count] = loader;
        bundleLoader.m_assetIDToLoaderIndex[id] = count;

        //Debug.Log($"成功注册资源: {obj.name}, AssetID: {id}, 加载器索引: {count}");

        // 返回可用于游戏系统的软引用
        return new SoftReference<T>(id) { m_name = obj.name };
    }

    /// <summary>注：获取组件，不存在则自动添加</summary>
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
    }

    /// <summary>注：批量打印植被生成全部配置参数，调试用</summary>
    static void GetVegetationInfo(ZoneSystem zoneSystem)
    {
        // 打印植被总数量
        Debug.LogError($"当前植被总数量：{zoneSystem.m_vegetation.Count}");
        Debug.LogError("=============================================");
        foreach (var item in zoneSystem.m_vegetation)
        {
            // 1. 基础信息：名称、预制体
            Debug.LogError($"当前植被名称：{item.m_name}");
            Debug.LogError($"当前植被预制件名称：{item.m_prefab.name}");

            // 2. 生成开关与密度
            Debug.LogError($"当前植被生成开关（m_enable）：{item.m_enable}");
            Debug.LogError($"当前植被生成密度-最小值（m_min）：{item.m_min}");
            Debug.LogError($"当前植被生成密度-最大值（m_max）：{item.m_max}");
            Debug.LogError($"当前植被是否强制生成（m_forcePlacement）：{item.m_forcePlacement}");

            // 3. 缩放与倾斜
            Debug.LogError($"当前植被缩放-最小值（m_scaleMin）：{item.m_scaleMin}");
            Debug.LogError($"当前植被缩放-最大值（m_scaleMax）：{item.m_scaleMax}");
            Debug.LogError($"当前植被随机倾斜角度（m_randTilt）：{item.m_randTilt}°");
            Debug.LogError($"当前植被地面倾斜复用概率（m_chanceToUseGroundTilt）：{item.m_chanceToUseGroundTilt}");

            Debug.LogError($"当前植被适用生物群系（m_biome）：{item.m_biome}");
            Debug.LogError($"当前植被适用生物群系区域（m_biomeArea）：{item.m_biomeArea}");

            // 5. 地形与海拔限制
            Debug.LogError($"当前植被是否开启阻挡检测（m_blockCheck）：{item.m_blockCheck}");
            Debug.LogError($"当前植被是否吸附静态固体（m_snapToStaticSolid）：{item.m_snapToStaticSolid}");
            Debug.LogError($"当前植被生成海拔-最小值（m_minAltitude）：{item.m_minAltitude}");
            Debug.LogError($"当前植被生成海拔-最大值（m_maxAltitude）：{item.m_maxAltitude}");
            Debug.LogError($"当前植被生成地形倾斜-最小值（m_minTilt）：{item.m_minTilt}°");
            Debug.LogError($"当前植被生成地形倾斜-最大值（m_maxTilt）：{item.m_maxTilt}°");

            // 6. 组生成配置
            Debug.LogError($"当前植被组生成大小-最小值（m_groupSizeMin）：{item.m_groupSizeMin}");
            Debug.LogError($"当前植被组生成大小-最大值（m_groupSizeMax）：{item.m_groupSizeMax}");
            Debug.LogError($"当前植被组生成半径（m_groupRadius）：{item.m_groupRadius}");

            // 7. 森林内生成条件
            Debug.LogError($"当前植被是否仅在森林内生成（m_inForest）：{item.m_inForest}");
            if (item.m_inForest)
            {
                Debug.LogError($"当前植被森林生成阈值-最小值（m_forestTresholdMin）：{item.m_forestTresholdMin}");
                Debug.LogError($"当前植被森林生成阈值-最大值（m_forestTresholdMax）：{item.m_forestTresholdMax}");
            }
            // 分隔线：区分不同植被的打印信息
            Debug.LogError("---------------------------------------------");
        }
    }
}



