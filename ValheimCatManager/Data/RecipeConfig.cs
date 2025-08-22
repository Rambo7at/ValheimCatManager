using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Data
{
    public class RecipeConfig
    {
        /// <summary>
        /// 注：制作目标的预制件名
        /// </summary>
        /// <param name="Name"></param>
        public RecipeConfig(string Name) => 物品 = Name;


        /// <summary>
        /// 注：这是配方的名字(非必填)
        /// </summary>
        public string 名字 { get; set; } = string.Empty;

        /// <summary>
        /// 注：配方制作的目标物品.
        /// </summary>
        public string 物品 { get; set; } = string.Empty;

        /// <summary>
        /// 注：物品的产量<br/>(默认值：1)
        /// </summary>
        public int 产量 { get; set; } = 1;

        /// <summary>
        /// 注：是否启用<br/>(默认值：true)
        /// </summary>
        public bool 启用 { get; set; } = true;

        /// <summary>
        /// 注：一个 “乘数”，用来根据制作材料的 “品质”，调整配方最终产出的物品数量<br/>(默认值：1)
        /// </summary>
        public float 品质产出数量乘数 { get; set; } = 1f;

        /// <summary>
        /// 注：可能是 物品在制作界面的排序<br/>(默认值：100)
        /// </summary>
        public int 显示顺序 { get; set; } = 100;

        /// <summary>
        /// 注：制作的工作台<br/>(默认值：空)
        /// </summary>
        public string 制作工作台 { get; set; } = string.Empty;

        /// <summary>
        /// 注：维修的工作台<br/>(默认值：空)
        /// </summary>
        public string 维修工作台 { get; set; } = string.Empty;


        /// <summary>
        /// 注：需求的工作台等级<br/>(默认值：1)
        /// </summary>
        public int 最低工作台等级 { get; set; } = 1;

        /// <summary>
        /// 注：只需要一种成分(默认-false)<br/>(默认值：false)
        /// </summary>
        public bool 只需要一种成分 { get; set; } = false;


        private readonly List<RequirementConfig> requirementConfigs = new List<RequirementConfig>();



        /// <summary>
        /// 注：给配方 添加需求物品的方法
        /// </summary>
        /// <param name="item">注：配方需求材料</param>
        /// <param name="indx">注：材料需求数量</param>
        /// <param name="level">注：升级需求的材料</param>
        /// <param name="recover">注：拆除物品后是否返还</param>
        public void 增加材料(string item, int indx, int level) => requirementConfigs.Add(new RequirementConfig
        {
            材料物品 = item,
            数量 = indx,
            升级数量 = level,
            恢复 = true,

        });

        private CraftingStation GetStation(string name)
        {
            if (!CatModData.m_PrefabCache.TryGetValue(name, out GameObject prefab))
            {
                prefab = CatToolManager.GetGameObject(name);



            }
            if (prefab == null) return null;
            var Station = prefab.GetComponent<CraftingStation>();
            if (Station == null) return null;
            return Station;

        }


        private Piece.Requirement[] GetRequirements()
        {
            Piece.Requirement[] requirements = new Piece.Requirement[requirementConfigs.Count];
            for (int i = 0; i < requirementConfigs.Count; i++)
            {
                if (!CatModData.m_PrefabCache.TryGetValue(requirementConfigs[i].材料物品, out GameObject gameobjetc))
                {

                    gameobjetc = CatToolManager.GetGameObject(requirementConfigs[i].材料物品);


                }
                if (gameobjetc == null) return null;

                var itemdrop = gameobjetc.GetComponent<ItemDrop>();
                if (itemdrop == null) return null;

                requirements[i] = new Piece.Requirement();
                requirements[i].m_resItem = itemdrop;
                requirements[i].m_amount = requirementConfigs[i].数量;
                requirements[i].m_amountPerLevel = requirementConfigs[i].升级数量;
                requirements[i].m_recover = requirementConfigs[i].恢复;
            }


            return requirements;
        }



        public Recipe GetRecipe()
        {

            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();

            if (string.IsNullOrEmpty(物品))
            {
                Debug.LogError("配方的目标物品不能为空！");
                return null;
            }
            if (string.IsNullOrEmpty(名字)) 名字 = $"Recipe_{物品}";

            recipe.name = 名字;

            if (!CatModData.m_PrefabCache.TryGetValue(物品, out GameObject prefab))
            {
                prefab = CatToolManager.GetGameObject(物品);
            }

            if (prefab == null)
            {
                Debug.LogError($"为找到对应预制件{物品}");
                return null;

            }
            var itemDrop = prefab.GetComponent<ItemDrop>();


            if (itemDrop == null)
            {
                Debug.LogError($"{物品}：未携带ItemDrop 组件！");
                return null;
            }
            recipe.m_item = itemDrop;
            recipe.m_amount = 产量;
            recipe.m_enabled = 启用;

            recipe.m_qualityResultAmountMultiplier = 品质产出数量乘数;
            recipe.m_listSortWeight = 显示顺序;

            var Station1 = GetStation(制作工作台);
            var Station2 = GetStation(维修工作台);
            if (Station1 == null && Station2 == null)
            {
                Debug.LogError($"未获取到相关工作台组件 检查：{制作工作台}，{维修工作台}");
                return null;
            }
            recipe.m_craftingStation = Station1;
            recipe.m_repairStation = Station2;
            recipe.m_minStationLevel = 最低工作台等级;
            recipe.m_requireOnlyOneIngredient = 只需要一种成分;


            var Requirements = GetRequirements();
            if (Requirements.Length == 0)
            {
                Debug.LogError($"需求材料列表有误 检查物品：{物品}");
                return null;
            }

            recipe.m_resources = Requirements;



            return recipe;


        }






    }
}
