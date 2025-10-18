using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Config;
using ValheimCatManager.Data;

namespace ValheimCatManager.Managers
{
    public class RecipeManager
    {
        /// <summary>
        /// 注：自定义的配方字典
        /// </summary>
        public readonly Dictionary<string, RecipeConfig> customRecipeDict = new Dictionary<string, RecipeConfig>();



        static RecipeManager _instance;

        public static RecipeManager Instance => _instance ?? (_instance = new RecipeManager());

        private RecipeManager() => new Harmony("RecipeManagerPatch").PatchAll(typeof(RecipePatch));




        /// <summary>
        /// 注：注册配方的补丁
        /// </summary>
        private static class RecipePatch
        {

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(10)]
            static void RegisterRecipe(ObjectDB __instance)
            {
                if (SceneManager.GetActiveScene().name == "main") Instance.RegisterRecipe(__instance, Instance.customRecipeDict);
            } 

        }



        /// <summary>
        /// 注：注册自定义配方到ObjectDB，确保配方可在游戏中使用（如制作台显示）
        /// </summary>
        /// <param name="instance">目标ObjectDB实例（存储游戏配方）</param>
        /// <param name="recipeDictionary">自定义配方字典（键：配方标识，值：配方配置）</param>
        private void RegisterRecipe(ObjectDB instance, Dictionary<string, RecipeConfig> recipeDictionary)
        {
            foreach (var recipeConfig in recipeDictionary)
            {
                // 从配方配置中生成游戏原生Recipe实例
                var recipe = recipeConfig.Value.GetRecipe();

                // 配方获取失败 直接跳过
                if (recipe == null)
                {
                    Debug.LogError($"添加的配方:[{recipeConfig.Key}]设置有误");
                    continue;
                }

                // 查重
                if (!instance.m_recipes.Contains(recipe)) instance.m_recipes.Add(recipe);
            }
        }



    }
}
