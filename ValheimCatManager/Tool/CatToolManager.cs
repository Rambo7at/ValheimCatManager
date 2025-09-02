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
using ValheimCatManager.Config;
using ValheimCatManager.Data;

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
