using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValheimCatManager;
using ValheimCatManager.Config;

namespace ValheimCatManager.Config;

/// <summary>注：需求材料配置，定义材料预制名、需求数量、升级数量和拆除返还</summary>
public class RequirementConfig
{
    /// <summary>注：构造函数，设置需求材料的预制件名</summary>
    public RequirementConfig(string prefabName) => _prefabName = prefabName;

    private string _prefabName = string.Empty; // 需求材料预制件名

    /// <summary>注：材料需求数量（默认1）</summary>
    public int Amount { get; set; } = 1;

    /// <summary>注：升级需求数量（默认1）</summary>
    public int AmountPerLevel { get; set; } = 1;

    public bool UpgraderResource { get; set; }

    /// <summary>注：拆除后是否返还材料（默认true）</summary>
    public bool IsRecoverable { get; set; } = true;

    /// <summary>注：获取需求材料的预制件名</summary>
    public string GetPrefabName() => _prefabName;
}



