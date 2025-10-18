using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using ValheimCatManager.Managers;
using ValheimCatManager.Tool;

namespace ValheimCatManager
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class CatManagerPlugin : BaseUnityPlugin
    {

        public const string PluginGUID = "com.rambo7at.CatManager";
        public const string PluginName = "猫咪：Mod管理";
        public const string PluginVersion = "0.1.5.4";

        public static AssetBundle assetBundle;
        ConfigEntry<bool> ConfigEntry;
        public void Awake()
        {
            // AB包添加：新建文件夹(名称随意)，选择添加现项目，将资源修改成嵌入式。
            // 加载AB包资源(关键步骤)
            CatResModManager.Instance.LoadAssetBundle("AB包名称", PluginName);
            

            // 添加物品：这些物品会注册至 ObjectDB 和 ZNetScene
            // 参数： 1，预制件名，2，启用mock
            CatResModManager.Instance.AddItem("预制件名",true);


            // 添加预制件：这些物品会注册至 ZNetScene ，不会注册给 ObjectDB
            // 参数：1，预制件名，2，启用mock
            // 非物品类：SFX，VFX，怪物。。。。。
            CatResModManager.Instance.AddPrefab("预制件名", true);


            // 添加食物：针对女巫版本食物的方法。
            // 参数：1，预制件名，2，食物目录，3，启用mock
            CatResModManager.Instance.AddFood("预制件名","蔬菜类" ,true);


            


            //////////////////////////有配置的类///////////////////////////////////////////
            #region 演示类
            // 以【添加植被】为例
            // 配置：每个选项我都有默认值，并不需要全部设置。每个选项都是中文字段，鼠标悬停会有对应说明
            // 注：构造函数的信息是必填的，需要对应植被的预制名。



            // 植被添加方式-1
            VegetationConfig vegetationConfig1 = new VegetationConfig("覆盆子");
            vegetationConfig1.区域范围 = Heightmap.BiomeArea.Median;
            vegetationConfig1.生态区域 = "Meadows";  // 兼容EW 的自定义区域
            vegetationConfig1.启用 = true;
            vegetationConfig1.最小_数量 = 5;
            vegetationConfig1.最大_数量 = 10;
            CatResModManager.Instance.AddVegetation(vegetationConfig1, true);


            // 植被添加方式-2
            VegetationConfig vegetationConfig2 = new VegetationConfig("橡树")
            {
                区域范围 = Heightmap.BiomeArea.Everything,
                生态区域 = "BlackForest",
                最大_数量 = 5,
                最小_数量 = 2
            };
            CatResModManager.Instance.AddVegetation(vegetationConfig2, true);



            // 植被添加方式-3
            CatResModManager.Instance.AddVegetation(new VegetationConfig("洋葱种子")
            {
                区域范围 = Heightmap.BiomeArea.Everything,
                生态区域 = "BlackForest",
                最大_数量 = 5,
                最小_数量 = 2
            }, true);

            #endregion




            // 添加植被：针对植被的方法
            // 参数： 1，植被配置类(VegetationConfig)，2，启用mock
            CatResModManager.Instance.AddVegetation(new VegetationConfig("植被名"),true);

            // 添加生成：给预制件增加生成，生成配置会注册给 m_spawnLists
            // 参数：1，生成配置类(SpawnConfig)与Spawn That相似
            CatResModManager.Instance.AddSpawn(new SpawnConfig("生物名"));

            // 添加物品：给游戏添加 物件的类，例：木墙，火堆，椅子。。。。
            // 参数： 1，物件配置类，2，启用mock
            // 注：物件目录，如果需要空，填：None，场景：耕地耙，官方默认就是空
            CatResModManager.Instance.AddPiece(new PieceConfig("物件名"),true);

            // 炼制站转换：熔炉，高炉，提炼器
            // 参数： 构造函数：炼制站预制名，输入材料，输出材料
            CatResModManager.Instance.AddSmelters(new SmeltersConfig("熔炉","铁块","铁锭"));

            // 烹饪站转换：烤肉架
            // 参数： 构造函数：炼制站预制名，输入材料，输出材料，制作时间
            CatResModManager.Instance.AddCookingStation(new CookingStationConfig("烤肉架","猪肉","熟猪肉",30));




        }






    }
}
