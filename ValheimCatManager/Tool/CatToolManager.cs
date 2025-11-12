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
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using Object = UnityEngine.Object;

namespace ValheimCatManager.Tool
{

    /// <summary>
    /// 注：工具管理类
    /// </summary>
    public static class CatToolManager
    {

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
        /// 注：按名称获取已注册的着色器，并缓存到CatModData.m_haderCache（避免重复查询）
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Shader GetShader(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("获取着色器名时，传入了空字符");
                return null;
            }

            if (CatModData.m_shaderCache.ContainsKey(name)) return CatModData.m_shaderCache[name];
            List<Shader> shaderList = new List<Shader>();
            var Shaders = Resources.FindObjectsOfTypeAll<Shader>();
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
            return null;
        }


        /// <summary>
        /// 注：按名称获取已加载的材质，并缓存到CatModData.m_materialCache（避免重复查询）
        /// 逻辑复用 GetShader 方法，保持一致性
        /// </summary>
        /// <param name="name">真实材质名称（不含JVLmock_前缀）</param>
        /// <returns>找到的材质，未找到返回null</returns>
        public static Material GetMaterial(string name)
        {
            // 1. 空值校验（和GetShader逻辑一致）
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("获取材质名时，传入了空字符");
                return null;
            }

            // 2. 先查缓存，命中直接返回（避免重复查找，提升性能）
            if (CatModData.m_materialCache.ContainsKey(name))
            {
                return CatModData.m_materialCache[name];
            }

            // 3. 全局查找所有已加载的材质（复用GetShader的Resources.FindObjectsOfTypeAll逻辑）
            List<Material> materialList = new List<Material>();
            var allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var material in allMaterials)
            {
                if (material == null) continue;

                // 精确匹配材质名称（和Shader查找逻辑一致，避免模糊匹配错误）
                if (material.name == name)
                {
                    materialList.Add(material);
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
            Debug.LogWarning($"CatToolManager.GetMaterial：未找到名称为「{name}」的材质");
            return null;
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

            // 查找预制件：ZNetScene → ObjectDB → Resources兜底
            GameObject itemPrefab = ZNetScene.instance.GetPrefab(name)
                                 ?? ObjectDB.instance.GetItemPrefab(name)
                                 ?? ResourcesGetGameObject(name);

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
            GameObject itemPrefab = ZNetScene.instance.GetPrefab(hash) ?? ObjectDB.instance.GetItemPrefab(hash);


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

        /// <summary>
        /// 注：获取资源ID
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static AssetID AssetIDFromObject(UnityEngine.Object obj)
        {
            int id = obj.GetInstanceID();
            return new AssetID(1, 1, 1, (uint)id);
        }

        /// <summary>
        /// 注：手动将已加载的Unity对象注册到游戏的AssetBundleLoader系统中，并创建对应的软引用
        /// 核心功能：通过生成唯一AssetID、创建AssetLoader并注册到系统，解决KeyNotFoundException问题
        /// 用途：为自定义地点/预制件创建可在游戏系统中正确使用的SoftReference，避免资源查找失败
        /// </summary>
        /// <typeparam name="T">Unity对象类型（通常为GameObject）</typeparam>
        /// <param name="obj">已加载的Unity对象实例（不能为null）</param>
        /// <returns>注册成功后返回对应的SoftReference<T>，用于游戏系统配置</returns>
        /// <exception cref="ArgumentNullException">当传入的obj参数为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当AssetBundleLoader实例未初始化时抛出</exception>
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
            AssetID id = CatToolManager.AssetIDFromObject(obj);

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
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
        }
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
                Debug.LogError($"当前植被生成开关（m_enable）：{(item.m_enable)}");
                Debug.LogError($"当前植被生成密度-最小值（m_min）：{item.m_min}");
                Debug.LogError($"当前植被生成密度-最大值（m_max）：{item.m_max}");
                Debug.LogError($"当前植被是否强制生成（m_forcePlacement）：{(item.m_forcePlacement)}");

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
}
