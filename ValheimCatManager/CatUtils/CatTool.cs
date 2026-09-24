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
        Debug.LogWarning($"[CatUtil.GetShader] 未找到着色器，如是专用服务器，请忽略：{name}");
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

        if (LoadMaterialFromBundles(name) is Material mat1) return mat1;

        if (LoadMaterialFromDiskBundles(name) is Material mat2) return mat2;



        // 未找到材质时输出日志（方便调试，和GetShader的错误反馈一致）
        Debug.LogWarning($"[CatUtil.GetMaterial] 未找到名称为「{name}」的材质");
        return null;
    }


    /// <summary>注：从已加载的AssetBundle中按名主动加载材质，兜底官方按需加载、启动时尚未进内存的材质</summary>
    public static Material LoadMaterialFromBundles(string name)
    {
        foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (bundle == null) continue;
            if (bundle.isStreamedSceneAssetBundle) continue; // 场景bundle只能LoadScene，不能LoadAsset，直接跳过

            Material[] materials;
            try { materials = bundle.LoadAllAssets<Material>(); } // 泛型直接取出该bundle全部材质
            catch { continue; }

            foreach (Material material in materials)
            {
                if (material == null) continue;
                if (!string.Equals(material.name, name, StringComparison.OrdinalIgnoreCase)) continue;

                CatModData.m_materialCache[name] = material; // 缓存并持有引用，防止被卸载
                return material;
            }
        }
        return null;
    }


    /// <summary>注：从指定的两个磁盘bundle中按名查找材质，兜底官方出生点未预加载的室内材质</summary>
    public static Material LoadMaterialFromDiskBundles(string name)
    {
        // 只在这两个出生点未加载的bundle中查找（SoftRef/Bundles下的哈希文件名）
        string[] bundleFiles = { "147b47a2", "64010dc9" };

        foreach (string bundleFile in bundleFiles)
        {
            string bundlePath = Path.Combine(Application.streamingAssetsPath, "SoftRef", "Bundles", bundleFile);
            if (!File.Exists(bundlePath)) continue;

            // 官方已加载该bundle则直接复用，重复LoadFromFile同一文件会报already loaded错误
            AssetBundle bundle = null;
            bool loadedBySelf = false;
            foreach (AssetBundle loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (loadedBundle == null) continue;
                if (!string.Equals(Path.GetFileName(loadedBundle.name), bundleFile, StringComparison.OrdinalIgnoreCase)) continue;
                bundle = loadedBundle;
                break;
            }

            // 官方没加载才自己临时加载
            if (bundle == null)
            {
                try { bundle = AssetBundle.LoadFromFile(bundlePath); loadedBySelf = true; }
                catch { continue; }
            }
            if (bundle == null) continue;

            // 泛型取出全部材质按名匹配，不写死材质名
            Material material = null;
            try
            {
                foreach (Material item in bundle.LoadAllAssets<Material>())
                {
                    if (item == null) continue;
                    if (!string.Equals(item.name, name, StringComparison.OrdinalIgnoreCase)) continue;
                    material = item;
                    break;
                }
            }
            catch { }

            // 自己临时加载的取完即释放（Unload(false)保留已取出的材质）；官方加载的绝不动
            if (loadedBySelf) bundle.Unload(false);

            if (material != null)
            {
                CatModData.m_materialCache[name] = material; // 缓存并持有引用，防止被卸载
                return material;
            }
        }
        return null;
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

        GameObject itemPrefab = (ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null) ?? (ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(name) : null) ?? ResourcesGetGameObject(name);

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


    /// <summary>注：通过预制件名称获取指定类型组件，内部调用 GetGameObject 查找预制件，若存在则获取组件</summary>
    public static T GetGameObjectComponent<T>(string objName) where T : Component
    {
        if (string.IsNullOrEmpty(objName))
        {
            Debug.LogWarning($"[CatUtil.GetGameObjectComponent] 传入的预制件名称为空");
            return null;
        }

        if (GetGameObject(objName) is not GameObject obj)
        {
            Debug.LogWarning($"[CatUtil.GetGameObjectComponent] 未找到预制件：{objName}");
            return null;
        }

        if (obj.GetComponent<T>() is not T comp)
        {
            Debug.LogWarning($"[CatUtil.GetGameObjectComponent] 预制件 [{objName}] 上未找到组件： [{typeof(T).Name}]");
            return null;
        }

        return comp;
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

}



