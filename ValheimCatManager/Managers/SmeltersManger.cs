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
    public class SmeltersManger
    {
        /// <summary>
        /// 注：自定义炼制站配方列表
        /// </summary>
        public readonly List<SmeltersConfig> customSmelters = new List<SmeltersConfig>();

        private static SmeltersManger _instance;

        public static SmeltersManger Instance => _instance ?? (_instance = new SmeltersManger());

        private SmeltersManger() => new Harmony("SmeltersMangerPatch").PatchAll(typeof(SmeltersPatch));


        private static class SmeltersPatch
        {

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(10)]
            static void RegisterSmeltersConfig(ObjectDB __instance)
            {
                if (SceneManager.GetActiveScene().name == "main") Instance.RegisterSmeltersConfig(Instance.customSmelters);
            } 

        }



        /// <summary>
        /// 注：注册自定义炼制站配方（如熔炉、锻铁炉）
        /// </summary>
        /// <param name="smeltersConfigs">炼制站配置列表（含炼制站预制名、输入/输出物品等）</param>
        private void RegisterSmeltersConfig(List<SmeltersConfig> smeltersConfigs)
        {
            foreach (var item in smeltersConfigs)
            {
                // 加载炼制站、输入物品、输出物品的预制件
                GameObject prefabSmelters = CatToolManager.GetGameObject(item.预制名);
                GameObject prefabInputItem = CatToolManager.GetGameObject(item.输入);
                GameObject prefabOutputItem = CatToolManager.GetGameObject(item.输出);

                // 任一预制件为空时打印错误并跳过
                if (prefabSmelters == null || prefabInputItem == null || prefabOutputItem == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，有单个预制件是空[{item.预制名}],[{item.输入}],[{item.输出}]，已跳过");
                    continue;
                }

                // 获取炼制站组件（Smelter），无组件则跳过
                Smelter smelter = prefabSmelters.GetComponent<Smelter>();
                if (smelter == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，[{item.预制名}] 没有对应的[smelters组件]，已跳过");
                    continue;
                }

                // 获取输入/输出物品的ItemDrop组件（物品核心组件），无组件则跳过
                ItemDrop itemDropInput = prefabInputItem.GetComponent<ItemDrop>();
                ItemDrop itemDropOutput = prefabOutputItem.GetComponent<ItemDrop>();
                if (itemDropInput == null || itemDropOutput == null)
                {
                    Debug.LogError($"执行RegisterSmeltersConfig时，物品：{item.输入}或{item.输出} 没有[ItemDrop组件]，已跳过");
                    continue;
                }

                // 向炼制站添加物品转换规则（输入→输出）
                smelter.m_conversion.Add(new Smelter.ItemConversion { m_from = itemDropInput, m_to = itemDropOutput });
            }
        }

    }
}
