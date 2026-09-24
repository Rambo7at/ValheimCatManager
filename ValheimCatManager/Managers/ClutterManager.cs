using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Config;

namespace ValheimCatManager.Managers;

/// <summary>注：地面杂物注册管理器，负责把自定义草/碎石/地面雾气注册进 ClutterSystem（杂物系统）</summary>
public class ClutterManager
{
    private static ClutterManager _instance;

    public static ClutterManager Instance => _instance ?? (_instance = new ClutterManager());

    private ClutterManager() => new Harmony("ClutterManagerPatch").PatchAll(typeof(ClutterPatch));

    /// <summary>注：自定义杂物字典（键：预制件名哈希，值：杂物配置）</summary>
    public readonly List<ClutterConfig> ClutterList = [];

    /// <summary>注：注册一个自定义杂物，同名自动覆盖</summary>
    public void RegisterClutter(ClutterConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.Prefab)) return;
        ClutterList.Add(config);
    }

    private static class ClutterPatch
    {
        /// <summary>前置：ClutterSystem.Awake 建立渲染实例前，把自定义杂物注入 m_clutter</summary>
        [HarmonyPatch(typeof(ClutterSystem), nameof(ClutterSystem.Awake)), HarmonyPrefix, HarmonyPriority(0)]
        static void ClutterSystem_Awake_Prefix(ClutterSystem __instance) => Instance.RegisterClutter(Instance.ClutterList, __instance);
    }

    /// <summary>注：把自定义杂物注册到 ClutterSystem，确保杂物在指定区域生成</summary>
    private void RegisterClutter(List<ClutterConfig> clutterList, ClutterSystem instance)
    {
        if (clutterList.Count == 0 || instance == null || SceneManager.GetActiveScene().name != "main") return;

        foreach (var data in clutterList)
        {
            if (data.GetClutter() is not ClutterSystem.Clutter clutter) continue;
            if (instance.m_clutter.Exists(c => c.m_name == clutter.m_name)) continue;
            instance.m_clutter.Add(clutter);
        }
    }
}

