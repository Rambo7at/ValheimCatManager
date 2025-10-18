using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Config
{
    public class RecipeConfig
    {




        /// <summary>
        /// 注：构造函数，传入制作目标的预制件名、工作台、工作台等级、产量和需求材料列表
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="station"></param>
        /// <param name="stationLevel"></param>
        /// <param name="amount"></param>
        /// <param name="resItemList"></param>
        public RecipeConfig(string itemName, string station, int stationLevel, int amount, params (string resItem, int resAmount, int levelAmount)[] resItemList)
        {
            try
            {
                物品 = itemName;
                制作工作台 = station;
                最低工作台等级 = stationLevel;
                产量 = amount;

                foreach (var resItem in resItemList)
                {
                    增加材料(resItem.resItem, resItem.resAmount, resItem.levelAmount);
                }

            }
            catch (Exception ex)
            {
                Debug.LogError($"添加{itemName}配方错误，捕获异常：{ex} ");
            }
        }

        /// <summary>
        /// (重载)构造函数 增加配方名选项
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="station"></param>
        /// <param name="stationLevel"></param>
        /// <param name="amount"></param>
        /// <param name="resItemList"></param>
        public RecipeConfig(string itemName, string recipeName, string station, int stationLevel, int amount, params (string resItem, int resAmount, int levelAmount)[] resItemList)
        {
            try
            {
                物品 = itemName;
                配方名 = recipeName;
                制作工作台 = station;
                最低工作台等级 = stationLevel;
                产量 = amount;

                foreach (var resItem in resItemList)
                {
                    增加材料(resItem.resItem, resItem.resAmount, resItem.levelAmount);
                }

            }
            catch (Exception ex)
            {
                Debug.LogError($"添加{itemName}配方错误，捕获异常：{ex} ");
            }
        }


        /// <summary>
        /// 注：(重载)构造函数 仅传入必要物品名
        /// </summary>
        /// <param name="name"></param>
        public RecipeConfig(string name) => 物品 = name;


        /// <summary>
        /// 注：这是配方的名字(非必填)
        /// </summary>
        public string 配方名 { get; set; } = string.Empty;

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

        /// <summary>
        /// 注：配方列表
        /// </summary>
        private readonly List<RequirementConfig> requirementConfigs = new List<RequirementConfig>();



        /// <summary>
        /// 工具函数：给配方列表添加材料
        /// </summary>
        /// <param name="item">注：配方需求材料</param>
        /// <param name="indx">注：材料需求数量</param>
        /// <param name="level">注：升级需求的材料</param>
        /// <param name="recover">注：拆除物品后是否返还</param>
        private void 增加材料(string item, int indx, int level) => requirementConfigs.Add(new RequirementConfig(item)
        {
            数量 = indx,
            升级数量 = level,
            恢复 = true,

        });

        private CraftingStation GetStation(string name)
        {
            var prefab = CatToolManager.GetGameObject(name);
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

                GameObject gameobjetc = CatToolManager.GetGameObject(requirementConfigs[i].GetPrefabName());


                if (gameobjetc == null)
                {
                    Debug.LogError($"[GetRequirements]执行失败，原因：材料预制件为空");
                    break;
                }

                var itemdrop = gameobjetc.GetComponent<ItemDrop>();

                if (itemdrop == null)
                {
                    Debug.LogError($"[GetRequirements]执行失败，原因：材料预制件没有 itemdrop 组件");
                    break;
                }

                requirements[i] = new Piece.Requirement();
                requirements[i].m_resItem = itemdrop;
                requirements[i].m_amount = requirementConfigs[i].数量;
                requirements[i].m_amountPerLevel = requirementConfigs[i].升级数量;
                requirements[i].m_recover = requirementConfigs[i].恢复;
            }


            return requirements;
        }


        /// <summary>
        /// 注：获取存储的信息，输出成配方脚本。
        /// </summary>
        /// <returns>配方脚本</returns>
        public Recipe GetRecipe()
        {

            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();

            if (string.IsNullOrEmpty(物品))
            {
                Debug.LogError($"[GetRecipe]执行失败，原因：配方目标预制名为空");
                return null;
            }

            if (string.IsNullOrEmpty(配方名)) 配方名 = $"Recipe_{物品}";

            recipe.name = 配方名;

            GameObject prefab = CatToolManager.GetGameObject(物品);

            if (prefab == null)
            {
                Debug.LogError($"未找到对应预制件{物品}");
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




            CraftingStation Station1 = string.IsNullOrEmpty(制作工作台) ? null : GetStation(制作工作台);
            CraftingStation Station2 = string.IsNullOrEmpty(维修工作台) ? null : GetStation(维修工作台);

            recipe.m_craftingStation = Station1;
            recipe.m_repairStation = Station2 ?? Station1;

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
