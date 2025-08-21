using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimCatManager.Data
{
    public class RequirementConfig
    {

        /// <summary>
        /// 注：配方需求材料
        /// </summary>
        public string 材料物品 { get; set; } = string.Empty;
        /// <summary>
        /// 注：材料需求数量
        /// </summary>
        public int 数量 { get; set; } = 1;

        /// <summary>
        /// 注：升级需求的材料
        /// </summary>
        public int 升级数量 { get; set; } = 1;

        /// <summary>
        /// 注：拆除物品后是否返还
        /// </summary>
        public bool 恢复 { get; set; } = true;

    }
}
