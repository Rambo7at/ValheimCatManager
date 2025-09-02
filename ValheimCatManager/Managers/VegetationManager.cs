using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Managers
{
    public class VegetationManager
    {

        private static VegetationManager _instance;

        public static VegetationManager Instance => _instance ?? (_instance = new VegetationManager());
        private VegetationManager() => new Harmony("VegetationManagerPatch").PatchAll(typeof(VegetationPatch));

        /// <summary>
        /// 注：自定义植被的字典
        /// </summary>
        public readonly Dictionary<int, VegetationConfig> customVegetationDict = new Dictionary<int, VegetationConfig>();

        /// <summary>
        /// 注：注册植被的补丁
        /// </summary>
        private static class VegetationPatch
        {
            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations)), HarmonyPostfix, HarmonyPriority(0)]
            static void RegisterVegetation(ZoneSystem __instance) => Instance.RegisterVegetation(Instance.customVegetationDict, __instance);
        }


        /// <summary>
        /// 注：注册自定义植被到ZoneSystem（区域系统），确保植被在指定区域生成
        /// </summary>
        /// <param name="VegetationDictionary">自定义植被字典（键：植被哈希值，值：植被配置）</param>
        /// <param name="instance">目标ZoneSystem实例（游戏区域管理器）</param>
        private void RegisterVegetation(Dictionary<int, VegetationConfig> VegetationDictionary, ZoneSystem instance)
        {
            // 植被字典为空或区域系统实例为空时直接返回
            if (VegetationDictionary.Count == 0 || instance == null) return;

            foreach (var VegetationS in VegetationDictionary)
            {
                // 从植被配置中生成游戏原生ZoneVegetation实例
                var Vegetation = VegetationS.Value.GetZoneVegetation();
                // 避免重复注册（区域植被列表中无该植被则添加）
                if (instance.m_vegetation.Contains(Vegetation)) continue;

                instance.m_vegetation.Add(Vegetation);
            }
        }


    }
}
