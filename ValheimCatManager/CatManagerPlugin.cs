using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.CatUtils;
using ValheimCatManager.Config;
using ValheimCatManager.Managers;

namespace ValheimCatManager
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class CatManagerPlugin : BaseUnityPlugin
    {

        public const string PluginGUID = "com.rambo7at.CatManager";
        public const string PluginName = "ValheimCatManager";
        public const string PluginVersion = "0.1.7.7";

        public void Awake()
        {
            new Harmony("ValheimCatManagerPatch").PatchAll(typeof(PatchManager));

            CatConfig.Instance.Load(Config);
            new GameDataExporter();
        }


    }
}
