using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.Config
{
    /// <summary>
    /// 烹饪站配置类，用于定义烹饪站的参数设置
    /// </summary>
    public class CookingStationConfig
    {

        /// <summary>
        /// 注：使用构造函数，直接设置，CookingStation组件的配置 (完整)
        /// </summary>
        /// <param name="name">目标烹饪站的预制体名称</param>
        /// <param name="input">需要进行烹饪处理的对象（食材）</param>
        /// <param name="delete">烹饪完成后输出的产物</param>
        /// <param name="time">完成烹饪所需的时间（单位：秒）</param>
        public CookingStationConfig(string name,string input ,string delete ,int time)
        {
            预制名 = name;
            输入 = input;
            输出 = delete;
            时间= time;
        }


        /// <summary>
        /// 注：使用构造函数，直接设置，CookingStation组件的配置 (默认时间)
        /// </summary>
        /// <param name="name">目标烹饪站的预制体名称</param>
        /// <param name="input">需要进行烹饪处理的对象（食材）</param>
        /// <param name="delete">烹饪完成后输出的产物</param>
        public CookingStationConfig(string name, string input, string delete)
        {
            预制名 = name;
            输入 = input;
            输出 = delete;
        }


        public string 预制名 = string.Empty;
        public string 输入 = string.Empty;
        public string 输出 = string.Empty;
        public int 时间 = 25;

    }
}
