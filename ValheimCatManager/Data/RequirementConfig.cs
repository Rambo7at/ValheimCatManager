using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimCatManager.Data
{
    public class RequirementConfig
    {

       public RequirementConfig(string name) => 预制名 = name;


        /// <summary>
        /// 注：需求材料<br>(默认值：1)</br>
        /// </summary>
        string 预制名  = string.Empty;

        /// <summary>
        /// 注：材料需求数量<br>(默认值：1)</br>
        /// </summary>
        public int 数量 { get; set; } = 1;

        /// <summary>
        /// 注：升级需求的材料<br>(默认值：1)</br>
        /// </summary>
        public int 升级数量 { get; set; } = 1;

        /// <summary>
        /// 注：拆除物品后是否返还<br>(默认值：true)</br>
        /// </summary>
        public bool 恢复 { get; set; } = true;



        /// <summary>
        /// 注：获取预制名
        /// </summary>
        /// <returns>Requirement的材料名</returns>
        public string GetPrefabName() { return 预制名; }







    }
}
