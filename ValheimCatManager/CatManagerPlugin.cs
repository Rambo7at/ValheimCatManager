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
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class CatManagerPlugin : BaseUnityPlugin
    {

        public const string pluginGUID = "com.rambo7at.CatManager";
        public const string pluginName = "猫咪：Mod管理";
        public const string pluginVersion = "0.0.1";


        public static AssetBundle assetBundle;


        public void Awake()
        {
            assetBundle = CatToolManager.LoadAssetBundle("7atfood");


            assetBundle.Unload(false);

        }






    }
}
