using HarmonyLib;
using SoftReferenceableAssets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Config;
using ValheimCatManager.Tool;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.Managers
{
    public class LocationManager
    {

        /// <summary>
        /// 注：自定义位置的字典
        /// </summary>
        public readonly List<LocationConfig> customLocationList = new();

        private static LocationManager _instance;

        public static LocationManager Instance => _instance ?? (_instance = new LocationManager());

        private LocationManager() => new Harmony("LocationManagerPatch").PatchAll(typeof(LocationManagerPatch));


        private static class LocationManagerPatch
        {

            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations)), HarmonyPostfix, HarmonyPriority(0)]

            static void RegisterLocation(ZoneSystem __instance) => Instance.RegisterLocation(__instance, Instance.customLocationList);

        }

        // 缓存软引用，避免重复创建
        private Dictionary<LocationConfig, SoftReference<GameObject>> locationSoftReferences;

        private void RegisterLocation(ZoneSystem instance, List<LocationConfig> locationConfigs)
        {

            // 初始化软引用缓存（如果尚未初始化）
            Instance.locationSoftReferences ??= new Dictionary<LocationConfig, SoftReference<GameObject>>();

            foreach (var locationConfig in locationConfigs)
            {
                // 获取ZoneLocation（所有属性设置应在GetZoneLocation内部完成）
                var location = locationConfig.GetZoneLocation();
                if (location == null)
                {
                    Debug.LogError($"执行RegisterLocation时 [{locationConfig.预制件.name}] 有问题");
                    continue;
                }

                // 获取或创建软引用
                if (!Instance.locationSoftReferences.TryGetValue(locationConfig, out var softRef))
                {
                    softRef = CatToolManager.AddLoadedSoftReferenceAsset(locationConfig.预制件);
                    Instance.locationSoftReferences[locationConfig] = softRef;
                }

                // 设置软引用
                location.m_prefab = softRef;
                // 添加到ZoneSystem
                instance.m_locations.Add(location);
                instance.m_locationsByHash.Add(location.Hash, location);
            }

            // 处理LocationProxy组件（如果存在）
            if (instance.m_locationProxyPrefab != null)
            {
                var existingProxy = instance.m_locationProxyPrefab.GetComponent<LocationProxy>();
                if (existingProxy != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingProxy);
                }
                instance.m_locationProxyPrefab.AddComponent<LocationProxy>();
            }
        }

    }
}
