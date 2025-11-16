using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.ValheimCatManager.Config;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.ValheimCatManager.Config
{
    /// <summary>
    /// 炼制站配置类，用于定义炼制站的参数设置
    /// </summary>
    public class SmeltersConfig
    {

        /// <summary>
        /// 初始化炼制站配置实例
        /// </summary>
        /// <param name="prefabName">目标炼制站的预制体名称</param>
        /// <param name="inputItem">需要炼制的物品</param>
        /// <param name="outputItem">炼制完成后输出的产物</param>
        public SmeltersConfig(string prefabName, string inputItem, string outputItem)
        {
            预制名 = prefabName;
            输入 = inputItem;
            输出 = outputItem;
        }


        public string 预制名 = string.Empty;
        public string 输入 = string.Empty;
        public string 输出 = string.Empty;
    }
}
