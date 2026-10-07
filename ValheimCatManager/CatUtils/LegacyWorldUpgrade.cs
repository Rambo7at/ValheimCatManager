using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.CatUtils;

namespace ValheimCatManager.CatUtils;

/// <summary>旧世界自动升级：检测到已有生成区块时，清理旧植被/地点 ZDO 和区块标记，让新规则生效</summary>
internal class LegacyWorldUpgrade
{
    public LegacyWorldUpgrade()
    {
        if (!CatConfig.Instance.EnableLegacyWorldUpgrade.Value) return;
        new Harmony("com.rambo7at.CatManager.LegacyWorldUpgrade").PatchAll(typeof(LegacyWorldUpgradePatch));
    }


    [HarmonyPatch(typeof(ZNet), nameof(ZNet.ServerLoadWorld))]
    static class LegacyWorldUpgradePatch
    {
        static bool upgraded = false;

        /// <summary>ZNet.ServerLoadWorld 后置：世界数据加载完成后执行一次</summary>
        static void Postfix()
        {
            if (upgraded) return;
            upgraded = true;

            if (!ZNet.instance.IsServer()) return;
            if (ZNet.World == null) return;

            var zs = ZoneSystem.instance;

            if (zs.m_generatedZones.Count == 0) return;

            Debug.Log("[LegacyWorldUpgrade] 检测到旧世界，开始重建...");

            var vegHashes = new HashSet<int>();
            foreach (var v in zs.m_vegetation)
            {
                if (v.m_prefab != null)
                    vegHashes.Add(v.m_prefab.name.GetStableHashCode());
            }
            var locHashes = new HashSet<int>();
            foreach (var l in zs.m_locations)
            {
                if (l.m_prefab.IsValid)
                    locHashes.Add(l.m_prefab.Name.GetStableHashCode());
            }

            int deletedVeg = 0, deletedLoc = 0;
            var toDelete = new List<ZDOID>();
            foreach (var kv in ZDOMan.instance.m_objectsByID)
            {
                int h = kv.Value.GetPrefab();
                if (vegHashes.Contains(h)) { toDelete.Add(kv.Key); deletedVeg++; }
                else if (locHashes.Contains(h)) { toDelete.Add(kv.Key); deletedLoc++; }
            }
            foreach (var id in toDelete)
                ZDOMan.instance.HandleDestroyedZDO(id);

            zs.m_generatedZones.Clear();
            zs.m_locationInstances.Clear();
            zs.GenerateLocations();

            Debug.LogError($"[LegacyWorldUpgrade] 完成：删除植被ZDO={deletedVeg}, 地点ZDO={deletedLoc}，请等待地点生成，不要关闭游戏！！！！！！！！！！");
        }


    }

}


