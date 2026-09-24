using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.Config;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.Config;

/// <summary>注：烹饪站配置类，定义烹饪站的预制名、输入食材、输出产物和烹饪时间</summary>
public class CookingStationConfig
{
    /// <summary>注：完整构造，设置烹饪站预制名、输入食材、输出产物和烹饪时间</summary>
    public CookingStationConfig(string prefabName, string inputItem, string outputItem, int cookTime)
    {
        PrefabName = prefabName;
        InputItem = inputItem;
        OutputItem = outputItem;
        CookTime = cookTime;
    }

    /// <summary>注：默认时间构造，设置烹饪站预制名、输入食材和输出产物，时间默认25秒</summary>
    public CookingStationConfig(string prefabName, string inputItem, string outputItem)
    {
        PrefabName = prefabName;
        InputItem = inputItem;
        OutputItem = outputItem;
    }

    public string PrefabName = string.Empty; // 烹饪站预制件名称
    public string InputItem = string.Empty;  // 烹饪输入食材
    public string OutputItem = string.Empty; // 烹饪输出产物
    public int CookTime = 25;                // 烹饪所需时间（秒）
}

