using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.CatUtils;
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using ValheimCatManager.Managers;
using static ValheimCatManager.Managers.PieceManager;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.Config
{
    public class PieceConfig
    {

        /// <summary>注：自定义 物件构造函数 </summary>
        public PieceConfig(string name, Piece.UsageTagFlags category, PieceManager.E_CraftTool tool, params (string resItem, int resAmount, bool check)[] resItemList)
        {

            预制件 = name;

            目录 = category;

            制作工具 = tool;

            foreach (var resItem in resItemList)
            {
                AddRequirement(resItem.resItem, resItem.resAmount, resItem.check);
            }

        }

        List<RequirementConfig> resList = new List<RequirementConfig>();


        /// <summary>
        /// 注：填写预制件名
        /// </summary>
        /// <param name="name"></param>
        public PieceConfig(string name) => 预制件 = name;

        /// <summary>
        /// 注：这是需要获取对应组件的预制件名
        /// </summary>
        string 预制件 { get; set; } = string.Empty;


        /// <summary>
        /// 注：制作所用的工具
        /// <br></br>原版：Cultivator(耕地耙)，Feaster(餐盘)，Hammer(锤子)，Hoe(锄头)
        /// </summary>
        public E_CraftTool 制作工具 { get; set; }


        /// <summary>
        /// 注：制作材料
        /// </summary>
        Piece.Requirement[] 制作材料 = new Piece.Requirement[0];


        /// <summary> 注：制作的分组标签 </summary>
        public Piece.UsageTagFlags 目录 { get; set; }




        /// <summary>
        /// 注：添加 Piece 材料需求
        /// </summary>
        /// <param name="name">材料名</param>
        /// <param name="amount">需求数量</param>
        /// <param name="check">物件拆除破坏是否返还</param>
        public void AddRequirement(string name, int amount, bool check) => resList.Add(new RequirementConfig(name)
        {
            Amount = amount,
            IsRecoverable = check

        });


        /// <summary>
        /// 注：从 列表：resList 获取 Piece.Requirement[]
        /// </summary>
        /// <returns>Piece.Requirement[]</returns>
        public Piece.Requirement[]? GetRequirementArr()
        {

            if (resList.Count == 0) return null;

            Piece.Requirement[] requirementS = new Piece.Requirement[resList.Count];

            for (int i = 0; i < resList.Count; i++)
            {
                string itemName = resList[i].GetPrefabName();
                var prefab = CatTool.GetGameObject(itemName);
                if (prefab == null) return null;

                ItemDrop itemdrop = prefab.GetComponent<ItemDrop>();
                if (itemdrop == null) return null;

                requirementS[i] = new Piece.Requirement();
                requirementS[i].m_resItem = itemdrop;
                requirementS[i].m_amount = resList[i].Amount;
                requirementS[i].m_recover = resList[i].IsRecoverable;
            }

            return requirementS;
        }



        /// <summary>
        /// 注：获取预制名
        /// </summary>
        /// <returns>Piece的预制件名</returns>
        public string GetPrefabName() { return 预制件; }




    }
}
