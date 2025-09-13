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

    }
}
