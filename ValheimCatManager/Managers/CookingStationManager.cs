using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Config;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Managers
{
    public class CookingStationManager
    {
        public readonly List<CookingStationConfig> customCookingStation = new ();

        private static CookingStationManager _instance;

        public static CookingStationManager Instance => _instance ?? (_instance = new CookingStationManager ());


        private CookingStationManager() => new Harmony("CookingStationManagerPatch").PatchAll(typeof(CookingStationPatch));

        private static class CookingStationPatch
        {
            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(10)]
            static void RegisterCookingStationConfig(ObjectDB __instance)
            {
                if (SceneManager.GetActiveScene().name == "main") Instance.RegisterCookingStationConfig(Instance.customCookingStation);
            } 
        }



        /// <summary>
        /// 注：注册自定义烹饪站配方（如烹饪锅），添加物品转换规则（含烹饪时间）到烹饪站组件
        /// </summary>
        /// <param name="cookingStationConfigs">烹饪站配置列表（含烹饪站预制名、输入/输出物品、烹饪时间等）</param>
        public  void RegisterCookingStationConfig(List<CookingStationConfig> cookingStationConfigs)
        {
            foreach (var item in cookingStationConfigs)
            {
                // 加载烹饪站、输入物品、输出物品的预制件
                GameObject prefabCookingStation = CatToolManager.GetGameObject(item.预制名);
                GameObject prefabInputItem = CatToolManager.GetGameObject(item.输入);
                GameObject prefabOutputItem = CatToolManager.GetGameObject(item.输出);

                // 任一预制件为空时打印错误并跳过
                if (prefabCookingStation == null || prefabInputItem == null || prefabOutputItem == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，有单个预制件是空[{item.预制名}],[{item.输入}],[{item.输出}]，已跳过");
                    continue;
                }

                // 获取烹饪站组件（CookingStation），无组件则跳过
                CookingStation cookingStation = prefabCookingStation.GetComponent<CookingStation>();
                if (cookingStation == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，[{item.预制名}] 没有对应的[CookingStation组件]，已跳过");
                    continue;
                }

                // 获取输入/输出物品的ItemDrop组件（物品核心组件），无组件则跳过
                ItemDrop itemDropInput = prefabInputItem.GetComponent<ItemDrop>();
                ItemDrop itemDropOutput = prefabOutputItem.GetComponent<ItemDrop>();
                if (itemDropInput == null || itemDropOutput == null)
                {
                    Debug.LogError($"执行RegisterCookingStationConfig时，物品：{item.输入}或{item.输出} 没有[ItemDrop组件]，已跳过");
                    continue;
                }

                // 向烹饪站添加物品转换规则（输入→输出，含自定义烹饪时间）
                cookingStation.m_conversion.Add(new CookingStation.ItemConversion
                {
                    m_from = itemDropInput,
                    m_to = itemDropOutput,
                    m_cookTime = item.时间
                });
            }
        }



    }
}
