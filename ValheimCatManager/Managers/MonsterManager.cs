using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Managers
{
    public class MonsterManager
    {
        /// <summary>
        /// 注：怪物自定义设置列表
        /// </summary>
        public readonly List<MonsterConfig> customMonsterSet = new List<MonsterConfig>();


        private static MonsterManager _instance;

        public static MonsterManager Instance => _instance ?? (_instance = new MonsterManager());

        private MonsterManager() => new Harmony("MonsterManagerPatch").PatchAll(typeof(MonsterSetPatch));


        private static class MonsterSetPatch
        {
            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(10)]
            static void SetMonster(ObjectDB __instance)
            {
                if (SceneManager.GetActiveScene().name == "main")
                {
                    Instance.RegisterMonsterConfig(Instance.customMonsterSet);
                }
            }
        }




        private void RegisterMonsterConfig(List<MonsterConfig> monsterConfigs)
        {
            foreach (var monster in monsterConfigs)
            {
                // 加载怪物预制件，为空则跳过
                GameObject monsterPrefab = CatToolManager.GetGameObject(monster.预制名);

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
                    GameObject itemPrefab = CatToolManager.GetGameObject(item);
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



    }
}
