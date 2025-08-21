using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Data;
using ValheimCatManager.Mock;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.Tool
{
    public class CatResModManager
    {

        AssetBundle catAsset;

        public CatResModManager(AssetBundle assetBundle) => catAsset = assetBundle;


        public void AddItem(string itemName, bool mockCheck)
        {
            GameObject itemPrefab = catAsset.LoadAsset<GameObject>(itemName);
            if (!itemPrefab)
            {
                Debug.LogError($"添加_物品 方法执行时：未有找到：{itemName} ");
                return;
            }
            int hash = itemPrefab.name.GetStableHashCode();
            if (!CatModData.自定义物品_字典.ContainsKey(hash)) CatModData.自定义物品_字典.Add(hash, itemPrefab);
            if (mockCheck) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, itemPrefab.name);
        }


        /// <summary>
        /// 注：给游戏添加 自定义配方的方法<br>使用方法：new RecipeConfig</br>
        /// </summary>
        /// <param name="recipeConfig"></param>
        public void AddRecipe(string name, RecipeConfig recipeConfig)
        {

            if (string.IsNullOrEmpty(recipeConfig.物品))
            {
                Debug.LogError($"添加配方时 物品名不能为空！");
                return;
            }

            CatModData.自定义配方_字典.Add(name, recipeConfig);

        }



        /// <summary>
        /// 注：针对性添加 摆放型食物
        /// </summary>
        /// <param name="foodName">注：物品名</param>
        /// <param name="mockCheck">注：是否启用 mock功能</param>
        public void AddFood(string foodName, string groupName, bool mockCheck)
        {
            GameObject itemPrefab = catAsset.LoadAsset<GameObject>(foodName);

            if (!itemPrefab)
            {
                Debug.LogError($"添加食物摆件 执行时：未有找到：{foodName} ");
                return;
            }

            int hash = itemPrefab.name.GetStableHashCode();

            if (!CatModData.自定义物品_字典.ContainsKey(hash)) CatModData.自定义物品_字典.Add(hash, itemPrefab);

            if (!CatModData.自定义食物_字典.ContainsKey(hash)) CatModData.自定义食物_字典.Add(hash, groupName);

            if (mockCheck) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, itemPrefab.name);


        }



        public void AddVegetation(VegetationConfig vegetationConfig ,bool mock)
        {

            GameObject itemPrefab = catAsset.LoadAsset<GameObject>(vegetationConfig.预制件);
            if (!itemPrefab)
            {
                Debug.LogError($"添加_物品 方法执行时：未有找到：{vegetationConfig.预制件} ");
                return;
            }
            int hash = itemPrefab.name.GetStableHashCode();

            if (!CatModData.自定义植被_字典.ContainsKey(hash)) CatModData.自定义植被_字典.Add(hash, vegetationConfig);

            if (!CatModData.自定义预制件_字典.ContainsKey(hash)) CatModData.自定义预制件_字典.Add(hash, itemPrefab);

            if (mock) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, itemPrefab.name);

        }


        public void AddPrefab(string PrefabName, bool mock)
        {
            GameObject itemPrefab = catAsset.LoadAsset<GameObject>(PrefabName);
            if (!itemPrefab)
            {
                Debug.LogError($"添加预制件 方法执行时：未有找到：{PrefabName} ");
                return;
            }


            int hash = itemPrefab.name.GetStableHashCode();

            if (!CatModData.自定义预制件_字典.ContainsKey(hash)) CatModData.自定义预制件_字典.Add(hash, itemPrefab);

            if (mock) if (!CatModData.模拟物品_字典.ContainsKey(hash)) CatModData.模拟物品_字典.Add(hash, itemPrefab.name);

        }





    }




    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    [HarmonyPriority(Priority.Last)]
    class AddItemsPatch
    {

        static void Postfix(ObjectDB __instance)
        {
            if (SceneManager.GetActiveScene().name == "main")
            {



                CatToolManager.GetShaderToCache();

                CatToolManager.RegisterFoodCategory();

                CatToolManager.RegisterToZNetScene(CatModData.自定义物品_字典);

                CatToolManager.RegisterToZNetScene(CatModData.自定义预制件_字典);

                CatToolManager.RegisterToObjectDB(__instance, CatModData.自定义物品_字典);
            }
        }
    }


    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    [HarmonyPriority(1)]
    class AddRecipePatch
    {
        static void Postfix(ObjectDB __instance)
        {
            if (SceneManager.GetActiveScene().name == "main")
            {
                CatToolManager.RegisterRecipe(__instance, CatModData.自定义配方_字典);
            }
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    [HarmonyPriority(2)]
    class AddFoodPiecePatch
    {
        static void Postfix(ObjectDB __instance)
        {
            if (SceneManager.GetActiveScene().name == "main")
            {
                CatToolManager.RegisterFoodPiece(CatModData.自定义食物_字典);
            }
        }
    }




    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    [HarmonyPriority(Priority.First)]
    class MockePatch
    {
        static void Postfix(ObjectDB __instance)
        {

            if (SceneManager.GetActiveScene().name == "main")
            {
                MockSystem.StartMockReplacement();
                CatModData.m_PrefabCache.Clear();

            }
        }
    }













    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    [HarmonyPriority(-101)]
    class ItemsCopyOtherDB
    {
        static void Prefix(ObjectDB __instance, ObjectDB other)
        {

            if (other == null) return;
            if (CatModData.自定义物品_字典 == null || CatModData.自定义物品_字典.Count == 0) return;
            // 关键：传入目标ObjectDB（other），而非源实例（__instance）
            CatToolManager.RegisterToObjectDB(other, CatModData.自定义物品_字典);
            CatToolManager.RegisterToZNetScene(CatModData.自定义物品_字典);


        }

    }








    [HarmonyPatch(typeof(ZoneSystem), "SetupLocations")]
    [HarmonyPriority(0)]
    class ZoneSystemPatch
    {
        static void Postfix(ZoneSystem __instance)
        {

            CatToolManager.RegisterVegetation(CatModData.自定义植被_字典, __instance);

        }

    }






















    [HarmonyPatch(typeof(Enum), "GetValues")]
    [HarmonyPriority(Priority.Normal)]
    class EnumGetValuesPatch
    {
        static void Postfix(Type enumType, ref Array __result)
        {
            CatToolManager.EnumGetPieceCategoryValuesPatch(enumType, ref __result);

        }

    }



    [HarmonyPatch(typeof(Enum), "GetNames")]
    [HarmonyPriority(Priority.Normal)]
    class EnumGetNamesPatch
    {
        static void Postfix(Type enumType, ref string[] __result)
        {
            CatToolManager.EnumGetPieceCategoryNamesPatch(enumType, ref __result);
 
        }

    }





















}
