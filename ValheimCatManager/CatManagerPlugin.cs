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
        public const string pluginVersion = "0.0.5";


        public static AssetBundle assetBundle;


        public void Awake()
        {
            assetBundle = CatToolManager.LoadAssetBundle("嵌入式资源包名");

            // 加载资源包
            CatResModManager catResModManager = new CatResModManager(assetBundle);

            // 添加食物针对方法 (物品名，自定义目录，是否启用mock)
            catResModManager.AddFood("7at_米饭", "猫咪食物", true);

            // 添加物品方法 (物品名，是否启用mock)
            catResModManager.AddItem("7at_土豆", true);

            // 添加物件方法 （物件名，自定义目录，制作工具，需求材料(材料，数量，返还)，是否启用mock）
            catResModManager.AddPiece(new PieceConfig("7at_土豆幼苗", "猫咪种植", "Cultivator", ("7at_土豆", 1, true)), true);


            // 添加植被方法 (植被配置，是否启用mock)
            catResModManager.AddVegetation(new VegetationConfig("7at_土豆成熟")
            {
                生态区域 = "BlackForest",
                最小_数量 = 4f,
                最大_数量 = 6f,
                最低_需求高度 = 0f,
                最高_需求高度 = 1000f,
                最大倾斜 = 25f,
                最大_地形高度变化 = 2f,
                组最小 = 3,
                组最大 = 6,
                组间距 = 5f,
            }, true);




            var harmony = new Harmony("CatManager");
            harmony.PatchAll();

            assetBundle.Unload(false);

        }






    }
}
