using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.Config;
using ValheimCatManager.Managers;

namespace ValheimCatManager.Managers;

/// <summary>注：植被注册管理器，负责把自定义植被注册进 ZoneSystem（区域系统）</summary>
public class VegetationManager
{
    private static VegetationManager _instance;

    public static VegetationManager Instance => _instance ?? (_instance = new VegetationManager()); // 注：单例访问入口（懒加载）

    private VegetationManager() => new Harmony("VegetationManagerPatch").PatchAll(typeof(VegetationPatch));

    public readonly List<VegetationConfig> VegetationList = [];

    private static class VegetationPatch
    {
        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations)), HarmonyPostfix, HarmonyPriority(0)]
        static void RegisterVegetation(ZoneSystem __instance) => Instance.RegisterVegetation(Instance.VegetationList, __instance);
    }


    public void RegisterVegetationSpawn(VegetationConfig data)
    {
        if (data == null) return;

        if (string.IsNullOrEmpty(data.预制件)) return;

        VegetationList.Add(data);
    }




    /// <summary>注：把自定义植被注册到 ZoneSystem，确保植被在指定区域生成</summary>
    private void RegisterVegetation(List<VegetationConfig> VegetationDictionary, ZoneSystem instance)
    {
        if (VegetationDictionary.Count == 0 || instance == null) return;

        foreach (var VegetationS in VegetationDictionary)
        {
            if (VegetationS.GetZoneVegetation() is not ZoneSystem.ZoneVegetation Vegetation) continue;
            if (instance.m_vegetation.Contains(Vegetation)) continue;
            instance.m_vegetation.Add(Vegetation);
        }
    }
}


