using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager;
using ValheimCatManager.Config;
using ValheimCatManager.Managers;

namespace ValheimCatManager.Managers;

/// <summary>注：配方管理器，注册自定义配方到游戏ObjectDB</summary>
public class RecipeManager
{
    private List<RecipeConfig> _recipeList = []; // 待注册的配方配置列表

    static RecipeManager _instance;

    public static RecipeManager Instance => _instance ?? (_instance = new RecipeManager());

    private RecipeManager() => PatchManager.OnObjectDBAwakeModify += RegisterRecipe;

    /// <summary>注：添加配方配置到待注册列表</summary>
    public void AddRecipe(RecipeConfig recipeConfig)
    {
        if (recipeConfig == null) return;
        _recipeList.Add(recipeConfig);
    }

    /// <summary>注：注册自定义配方到ObjectDB，确保配方可在游戏中使用（如制作台显示）</summary>
    private void RegisterRecipe(ObjectDB instance)
    {
        foreach (var recipeConfig in _recipeList)
        {
            if (recipeConfig.GetRecipe() is not Recipe recipe)
            {
                Debug.LogError($"[RecipeManager.RegisterRecipe] 配方 [{recipeConfig.ItemName}] 配置有误，已跳过");
                continue;
            }

            if (instance.m_recipes.Contains(recipe)) continue;
            instance.m_recipes.Add(recipe);
        }
    }
}



