using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Config;

namespace ValheimCatManager.Managers;

public class BiomeEnvManager
{
    private static BiomeEnvManager _instance;
    public static BiomeEnvManager Instance => _instance ?? (_instance = new BiomeEnvManager());

    /// <summary>注：地区的环境配置</summary>
    public class BiomeEnvData
    {
        public string Name = string.Empty;
        public Heightmap.Biome Biome = Heightmap.Biome.None;
        public Dictionary<string, float> Environments = [];
        public string MusicMorning = "morning";
        public string MusicEvening = "evening";
        public string MusicDay = string.Empty;
        public string MusicNight = string.Empty;
    }

    /// <summary>注：已注册的地区配置</summary>
    public List<BiomeEnvData> BiomeEnvDataList = [];

    /// <summary>注：已注册的自定义天气配置</summary>
    public List<EnvConfig> EnvConfigList = [];

    private BiomeEnvManager() => new Harmony("com.rambo7at.CatManager.BiomeEnvManager").PatchAll(typeof(BiomeEnvManagerPatch));

    /// <summary>注：注册自定义地区配置</summary>
    public void RegisterBiomeEnv(BiomeEnvData biomeEnvData)
    {
        if (biomeEnvData == null) return;
        if (biomeEnvData.Biome == Heightmap.Biome.None) return;
        BiomeEnvDataList.Add(biomeEnvData);
    }

    /// <summary>注：注册自定义天气配置</summary>
    public void RegisterEnv(EnvConfig envConfig)
    {
        if (envConfig == null) return;
        if (string.IsNullOrEmpty(envConfig.Name)) return;
        EnvConfigList.Add(envConfig);
    }

    /// <summary>注：EnvMan就绪后，先注册自定义天气，再注册地区环境配置</summary>
    private void RegisterAll()
    {
        // 先注册自定义天气
        foreach (var config in EnvConfigList)
        {
            var env = config.GetEnvSetup();
            if (env != null)
            {
                EnvMan.instance.m_environments.Add(env);
                Debug.Log($"[BiomeEnvManager.RegisterAll] 已注册天气: {env.m_name}");
            }
        }

        // 再注册地区环境配置
        foreach (var item in BiomeEnvDataList)
        {
            List<EnvEntry> environments = [];
            foreach (var kv in item.Environments)
            {
                environments.Add(new EnvEntry
                {
                    m_environment = kv.Key,
                    m_weight = kv.Value
                });
            }

            BiomeEnvSetup setup = new()
            {
                m_biome = item.Biome,
                m_environments = environments,
                m_musicMorning = item.MusicMorning,
                m_musicDay = item.MusicDay,
                m_musicEvening = item.MusicEvening,
                m_musicNight = item.MusicNight
            };

            EnvMan.instance.m_biomes.Add(setup);
            EnvMan.instance.InitializeBiomeEnvSetup(setup);
            Debug.Log($"[BiomeEnvManager.RegisterAll] 已注册 {item.Biome} ({item.Name})");
        }
    }

    private class BiomeEnvManagerPatch
    {
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Awake)), HarmonyPostfix, HarmonyPriority(0)]
        static void EnvMan_Awake_Postfix() => Instance.RegisterAll();

        /// <summary>注：用实时身份查找群系环境配置，解决自定义群系(0x800)匹配不上的问题</summary>
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetBiomeEnvSetup)), HarmonyPrefix, HarmonyPriority(0)]
        static bool GetBiomeEnvSetup_Prefix(Heightmap.Biome biome, ref BiomeEnvSetup __result)
        {
            if (Player.m_localPlayer == null) return true;
            var realtime = WorldGenerator.instance.GetBiome(Player.m_localPlayer.transform.position);
            if ((int)realtime >= 0x400)
            {
                foreach (var setup in EnvMan.instance.m_biomes)
                {
                    if (setup.m_biome == realtime)
                    {
                        __result = setup;
                        return false;
                    }
                }
            }
            return true;
        }
    }
}




