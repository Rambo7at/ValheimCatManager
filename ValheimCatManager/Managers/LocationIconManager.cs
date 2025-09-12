using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Minimap;

namespace ValheimCatManager.Managers
{
    internal class LocationIconManager
    {
        private static LocationIconManager _instance;

        public static LocationIconManager Instance => _instance ?? (_instance = new LocationIconManager());

        private LocationIconManager() => new Harmony("LocationIconManagerPatch").PatchAll(typeof(LocationIconPatch));


        public readonly Dictionary<string, Sprite> customLocationIconDict = new();



        private static class LocationIconPatch
        {

            [HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake)), HarmonyPostfix, HarmonyPriority(10)]
            static void AddLocationIcon(Minimap __instance) => Instance.AddLocationIcon(__instance);


        }

        void AddLocationIcon(Minimap minimap)
        {

            foreach (var icon in Instance.customLocationIconDict)
            {
                if (!String.IsNullOrEmpty(icon.Key)) continue;
                if (!icon.Value) continue;


                minimap.m_locationIcons.Add(new LocationSpriteData() { m_name = icon.Key, m_icon = icon.Value });
            }




        }








    }
}
