using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Managers
{
    public class PrefabManager
    {

        private static PrefabManager _instance;

        public static PrefabManager Instance => _instance ?? (_instance = new PrefabManager());

        private bool HarmonyCheck = true;
        private PrefabManager() => new Harmony("ItemManagerPatch").PatchAll(typeof(PrefabPatch));



        /// <summary>
        /// 注：自定义物品的字典，准备注册给游戏
        /// </summary>
        public readonly Dictionary<int, GameObject> customItemDict = new ();
        /// <summary>
        /// 注：自定义预制件的字典，准备注册给游戏
        /// </summary>
        public  readonly Dictionary<int, GameObject> customPrefabDict = new();
        /// <summary>
        /// 注：自定义地点预制件的字典，准备注册给游戏
        /// </summary>
        public readonly Dictionary<int, GameObject> customLocationDict = new();


        /// <summary>
        /// 注：注册物品 与 自定义预制件的补丁
        /// </summary>
        private static class PrefabPatch
        {
            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(0)]
            static void RegisterItemPatch(ObjectDB __instance)
            {
                if (SceneManager.GetActiveScene().name == "main") Instance.RegisterToObjectDB(__instance, Instance.customItemDict);
            }

            [HarmonyPatch(typeof(ZNetScene), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(0)]
            static void RegisterGameObjectPatch(ZNetScene __instance)
            {

                if (SceneManager.GetActiveScene().name == "main")
                {
                    Instance.RegisterToZNetScene(__instance, Instance.customItemDict);
                    Instance.RegisterToZNetScene(__instance, Instance.customPrefabDict);
                    Instance.RegisterLocationToZNetScene(__instance, Instance.customLocationDict);
                }
            }

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), HarmonyPrefix, HarmonyPriority(-101)]
            static void CopyOtherDBPatch(ObjectDB __instance, ObjectDB other)
            {
                if (other == null) return;
                Instance.RegisterToObjectDB(other, Instance.customItemDict);
            }

        }

        /// <summary>
        /// 注：将自定义物品注册到ObjectDB
        /// </summary>
        /// <param name="instance">目标ObjectDB实例</param>
        /// <param name="itemDictionary">自定义物品字典</param>
        private void RegisterToObjectDB(ObjectDB instance, Dictionary<int, GameObject> itemDictionary)
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
        
        private void RegisterToZNetScene(ZNetScene instance, Dictionary<int, GameObject> itemDictionary)
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


        private void RegisterLocationToZNetScene(ZNetScene instance, Dictionary<int, GameObject> locationDictionary)
        {
            // 验证输入参数有效性
            if (instance == null || locationDictionary == null || locationDictionary.Count == 0)
            {
                Debug.LogWarning("注册失败：ZNetScene实例为空或地点预制件字典为空");
                return;
            }

            foreach (var locationEntry in locationDictionary)
            {
                int locationHash = locationEntry.Key;
                GameObject locationPrefab = locationEntry.Value;

                if (locationPrefab == null)
                {
                    Debug.LogError($"地点预制件哈希 {locationHash} 对应的预制件为空，已跳过");
                    continue;
                }

                // 1. 处理地点根预制件本身
                ProcessLocationObject(instance, locationPrefab);

                // 2. 递归处理所有子物体（核心：确保所有子物体的ZNetView都被注册）
                var allChildTransforms = locationPrefab.GetComponentsInChildren<Transform>(true);
                foreach (var childTransform in allChildTransforms)
                {
                    // 跳过根对象，避免重复处理
                    if (childTransform.gameObject == locationPrefab)
                        continue;

                    ProcessLocationObject(instance, childTransform.gameObject);
                }
            }
        }

        // 辅助方法：处理单个地点对象（包括根对象和子对象）
        private void ProcessLocationObject(ZNetScene instance, GameObject locationObject)
        {
            string prefabName = Utils.GetPrefabName(locationObject);
            int hashKey = prefabName.GetStableHashCode();

            // 检查是否已注册该哈希
            if (instance.m_namedPrefabs.ContainsKey(hashKey))
            {
                // 检查哈希冲突
                GameObject existingPrefab = instance.m_namedPrefabs[hashKey];
                string existingName = Utils.GetPrefabName(existingPrefab);
                if (existingName != prefabName)
                {
                    Debug.LogError($"哈希冲突：预制件 {prefabName} 与 {existingName} 具有相同哈希值 {hashKey}，已跳过注册");
                }
                return;
            }

            // 获取ZNetView组件判断是否需要网络同步
            ZNetView netView = locationObject.GetComponent<ZNetView>();

            if (netView != null)
            {
                // 有ZNetView的对象添加到网络预制件列表
                instance.m_prefabs.Add(locationObject);
            }
            else
            {
                // 无ZNetView的对象添加到非网络预制件列表
                instance.m_nonNetViewPrefabs.Add(locationObject);
            }

            // 注册到名称-预制件映射表
            instance.m_namedPrefabs[hashKey] = locationObject;
        }




    }
}
