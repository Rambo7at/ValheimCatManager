using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Data
{
    public class FoodPieceConfig
    {


        /// <summary>
        /// 注：构造函数，以防忘记要填写预制件名
        /// </summary>
        /// <param name="name"></param>
       public FoodPieceConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                UnityEngine.Debug.LogError("食物摆件设置的名称不能为空！");
            }
            预制件名 = name;

        }

        /// <summary>
        /// 注：这是需要获取对应组件的预制件名
        /// </summary>
        public string 预制件名 { get; set; } = string.Empty;


        /// <summary>
        /// 注：食物的分组是哪里
        /// </summary>
        public string 分组 { get; set; } = string.Empty;


    }
}
