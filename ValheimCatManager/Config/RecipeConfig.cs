using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.CatUtils;
using ValheimCatManager.Config;

namespace ValheimCatManager.Config;

/// <summary>注：配方配置类，定义自定义配方的目标物品、工作台、产量和需求材料</summary>
public class RecipeConfig
{
    /// <summary>注：配方名（非必填）</summary>
    public string RecipeName { get; set; } = string.Empty;

    /// <summary>注：配方制作的目标物品</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>注：物品产量（默认1）</summary>
    public int Amount { get; set; } = 1;

    /// <summary>注：是否启用（默认true）</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>注：根据材料品质调整产出数量的乘数（默认1）</summary>
    public float QualityResultAmountMultiplier { get; set; } = 1f;

    /// <summary>注：制作界面排序（默认100）</summary>
    public int ListSortWeight { get; set; } = 100;

    /// <summary>注：制作工作台（默认空）</summary>
    public string CraftingStationName { get; set; } = string.Empty;

    /// <summary>注：维修工作台（默认空）</summary>
    public string RepairStationName { get; set; } = string.Empty;

    /// <summary>注：需求工作台等级（默认1）</summary>
    public int MinStationLevel { get; set; } = 1;

    /// <summary>注：只需要一种成分（默认false）</summary>
    public bool RequireOnlyOneIngredient { get; set; } = false;

    private readonly List<RequirementConfig> _requirements = []; // 配方需求材料列表

    /// <summary>注：默认构造函数，创建空配方配置实例</summary>
    public RecipeConfig() { }

    /// <summary>注：构造函数，传入制作目标的预制件名、工作台、工作台等级、产量和需求材料列表</summary>
    public RecipeConfig(string itemName, string craftingStationName, int minStationLevel, int amount, params (string resItem, int resAmount, int levelAmount)[] resItemList)
    {
        ItemName = itemName;
        CraftingStationName = craftingStationName;
        MinStationLevel = minStationLevel;
        Amount = amount;

        foreach (var resItem in resItemList)
        {
            AddRequirement(resItem.resItem, resItem.resAmount, resItem.levelAmount);
        }
    }

    /// <summary>注：重载构造函数，增加配方名选项</summary>
    public RecipeConfig(string itemName, string recipeName, string craftingStationName, int minStationLevel, int amount, params (string resItem, int resAmount, int levelAmount)[] resItemList)
    {
        ItemName = itemName;
        RecipeName = recipeName;
        CraftingStationName = craftingStationName;
        MinStationLevel = minStationLevel;
        Amount = amount;

        foreach (var resItem in resItemList)
        {
            AddRequirement(resItem.resItem, resItem.resAmount, resItem.levelAmount);
        }
    }

    /// <summary>注：构造函数，传入制作目标的预制件名、工作台、工作台等级、产量、升级专用材料标记和需求材料列表</summary>
    public RecipeConfig(string itemName, string craftingStationName, int minStationLevel, int amount, params (string resItem, int resAmount, int levelAmount, bool upgraderRes)[] resItemList)
    {
        ItemName = itemName;
        CraftingStationName = craftingStationName;
        MinStationLevel = minStationLevel;
        Amount = amount;

        foreach (var resItem in resItemList)
        {
            AddRequirement(resItem.resItem, resItem.resAmount, resItem.levelAmount, resItem.upgraderRes);
        }
    }

    /// <summary>注：重载构造函数，增加配方名和升级专用材料标记选项</summary>
    public RecipeConfig(string itemName, string recipeName, string craftingStationName, int minStationLevel, int amount, params (string resItem, int resAmount, int levelAmount, bool upgraderRes)[] resItemList)
    {
        ItemName = itemName;
        RecipeName = recipeName;
        CraftingStationName = craftingStationName;
        MinStationLevel = minStationLevel;
        Amount = amount;

        foreach (var resItem in resItemList)
        {
            AddRequirement(resItem.resItem, resItem.resAmount, resItem.levelAmount, resItem.upgraderRes);
        }
    }

    /// <summary>注：将配置信息转换为游戏原生Recipe实例</summary>
    public Recipe GetRecipe()
    {
        if (CatTool.GetGameObjectComponent<ItemDrop>(ItemName) is not ItemDrop itemDrop) return null;

        var requirements = GetRequirements();
        if (requirements.Length == 0)
        {
            Debug.LogError($"[RecipeConfig.GetRecipe] 配方 [{ItemName}] 需求材料列表为空");
            return null;
        }

        if (string.IsNullOrEmpty(RecipeName)) RecipeName = $"Recipe_{ItemName}";

        Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
        recipe.name = RecipeName;
        recipe.m_item = itemDrop;
        recipe.m_amount = Amount;
        recipe.m_enabled = Enabled;
        recipe.m_qualityResultAmountMultiplier = QualityResultAmountMultiplier;
        recipe.m_listSortWeight = ListSortWeight;

        CraftingStation craftingStation = string.IsNullOrEmpty(CraftingStationName) ? null : CatTool.GetGameObjectComponent<CraftingStation>(CraftingStationName);
        CraftingStation repairStation = string.IsNullOrEmpty(RepairStationName) ? null : CatTool.GetGameObjectComponent<CraftingStation>(RepairStationName);

        recipe.m_craftingStation = craftingStation;
        recipe.m_repairStation = repairStation ?? craftingStation;

        recipe.m_minStationLevel = MinStationLevel;
        recipe.m_requireOnlyOneIngredient = RequireOnlyOneIngredient;
        recipe.m_resources = requirements;

        return recipe;
    }

    /// <summary>注：给配方需求列表添加材料</summary>
    public void AddRequirement(string item, int amount, int amountPerLevel, bool upgraderRes = false) => _requirements.Add(new RequirementConfig(item)
    {
        Amount = amount,
        AmountPerLevel = amountPerLevel,
        IsRecoverable = true,
        UpgraderResource = upgraderRes
    });

    /// <summary>注：将需求配置列表转换为游戏原生Piece.Requirement数组，跳过无效材料</summary>
    private Piece.Requirement[] GetRequirements()
    {
        var validRequirements = new List<Piece.Requirement>();

        foreach (var config in _requirements)
        {
            if (CatTool.GetGameObjectComponent<ItemDrop>(config.GetPrefabName()) is not ItemDrop itemDrop)
            {
                Debug.LogError($"[RecipeConfig.GetRequirements] 材料 [{config.GetPrefabName()}] 未找到 ItemDrop 组件，已跳过");
                continue;
            }

            validRequirements.Add(new Piece.Requirement
            {
                m_resItem = itemDrop,
                m_amount = config.Amount,
                m_amountPerLevel = config.AmountPerLevel,
                m_recover = config.IsRecoverable,
                m_upgraderResource = config.UpgraderResource
            });
        }

        return validRequirements.ToArray();
    }
}





