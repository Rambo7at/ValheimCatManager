using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;

namespace ValheimCatManager
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class CatManagerPlugin : BaseUnityPlugin
    {

        public const string PluginGUID = "com.rambo7at.CatManager";
        public const string PluginName = "猫咪：Mod管理";
        public const string PluginVersion = "0.1.1.0";

        public static AssetBundle assetBundle;

        public void Awake()
        {



        }






    }
}
