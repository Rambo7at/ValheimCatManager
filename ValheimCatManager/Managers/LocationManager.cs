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

        private void RegisterLocation(ZoneSystem instance, List<LocationConfig> locationConfigs)
        {

            foreach (var locationConfig in locationConfigs)
            {

                var location = locationConfig.GetZoneLocation();
                if (location == null)
                {
                    Debug.LogError($"执行RegisterLocation时 [{locationConfig.预制件.name}] 有问题");
                    continue;
                }

                SoftReference<GameObject> softRef = CatToolManager.AddLoadedSoftReferenceAsset(locationConfig.预制件);

                location.m_prefab = softRef;

                instance.m_locations.Add(location);
                instance.m_locationsByHash.Add(location.Hash, location);

               
            }

        }

    }
}
