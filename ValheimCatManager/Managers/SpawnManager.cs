using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Config;
using ValheimCatManager.Data;

namespace ValheimCatManager.Managers
{
    public class SpawnManager
    {
        /// <summary>
        /// 注：自定生成列表
        /// </summary>
        public readonly List<SpawnConfig> customSpawn = new List<SpawnConfig>();

        /// <summary>
        /// 注：给 SpawnSystem 添加生成的类
        /// </summary>
        private readonly SpawnSystemList customSpawnSystem = new SpawnSystemList();

        private static SpawnManager _instance;

        public static SpawnManager Instance => _instance ?? (_instance = new SpawnManager());

        private SpawnManager() => new Harmony("SpawnManagerPatch").PatchAll(typeof(SpawnPatch));




        private static class SpawnPatch
        {
            [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Awake)), HarmonyPostfix, HarmonyPriority(Priority.VeryLow)]
            static void RegisterSpawnPatch(SpawnSystem __instance) => RegisterSpawnList(__instance);
        }


        private static void RegisterSpawnList(SpawnSystem instance)
        {
            // 处理自定义生成列表，转换为SpawnData并添加到统一生成列表
            if (Instance.customSpawn.Count != 0)
            {
                foreach (var spawnConfig in Instance.customSpawn)
                {
                    // 从生成配置中生成游戏原生SpawnData实例
                    var spawnData = spawnConfig.GetSpawnData();
                    if (spawnData == null)
                    {
                        Debug.LogError($"添加生成列表错误，生成数据是空检查：{spawnConfig.预制件}");
                        continue;
                    }

                    // 添加到全局生成列表缓存
                    Instance.customSpawnSystem.m_spawners.Add(spawnData);
                }
                // 清空原自定义生成列表（避免重复处理）
                Instance.customSpawn.Clear();
            }

            // 生成系统实例为空或无生成数据时直接返回
            if (!instance || Instance.customSpawnSystem.m_spawners.Count == 0)
            {
                return;

            }

            // 将自定义生成列表添加到游戏生成系统
            instance.m_spawnLists.Add(Instance.customSpawnSystem);
        }



    }
}
