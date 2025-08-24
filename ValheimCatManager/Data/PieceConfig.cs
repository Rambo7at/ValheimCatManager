using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Tool;
using Debug = UnityEngine.Debug;

namespace ValheimCatManager.Data
{
    public class PieceConfig
    {






        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="category"></param>
        /// <param name="tool"></param>
        /// <param name="resItemList"></param>
        public PieceConfig(string name,string category,string tool ,params (string resItem, int resAmount, bool check)[] resItemList)
        {

            预制件 = name;

            分组 = category;

            制作工具 = tool;

            foreach (var resItem in resItemList)
            {
                AddRequirement(resItem.resItem,resItem.resAmount,resItem.check);
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
        public string 制作工具 { get; set; } = string.Empty;


        /// <summary>
        /// 注：制作材料
        /// </summary>
        Piece.Requirement[] 制作材料  = new Piece.Requirement[0];


        /// <summary>
        /// 注：制作的分组标签
        /// </summary>
        public string 分组 { get; set; } = string.Empty;




        /// <summary>
        /// 注：添加 Piece 材料需求
        /// </summary>
        /// <param name="name">材料名</param>
        /// <param name="amount">需求数量</param>
        /// <param name="check">物件拆除破坏是否返还</param>
        public void AddRequirement(string name,int amount, bool check) => resList.Add(new RequirementConfig(name)
        {
            数量 = amount,
            恢复 = check

        });





        /// <summary>
        /// 注：从 列表：resList 获取 Piece.Requirement[]
        /// </summary>
        /// <returns>Piece.Requirement[]</returns>
        public Piece.Requirement[] GetRequirementArr()
        {

            if (resList.Count == 0) return null;

            Piece.Requirement[] requirementS = new Piece.Requirement[resList.Count];

            for (int i = 0; i < resList.Count; i++)
            {
                string itemName = resList[i].GetPrefabName();
                var prefab = CatToolManager.GetGameObject(itemName);
                if (prefab == null) return null;

                ItemDrop itemdrop = prefab.GetComponent<ItemDrop>();
                if (itemdrop == null) return null;

                requirementS[i] = new Piece.Requirement();
                requirementS[i].m_resItem = itemdrop;
                requirementS[i].m_amount = resList[i].数量;
                requirementS[i].m_recover = resList[i].恢复;
            }

            return requirementS;
        }




        /// <summary>
        /// 注：获取制作工具 PieceTable 组件
        /// </summary>
        /// <returns>PieceTable</returns>
        public PieceTable GetPieceTable()
        {
            if (CatModData.m_PieceTableCache.ContainsKey(制作工具)) return CatModData.m_PieceTableCache[制作工具];

            var prefab = CatToolManager.GetGameObject(制作工具);
            if (prefab == null) return null;

            PieceTable pieceTable = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
            if (pieceTable == null) return null;
            CatModData.m_PieceTableCache.Add(制作工具,pieceTable);
            return pieceTable;
        }



        /// <summary>
        /// 注：获取预制名
        /// </summary>
        /// <returns>Requirement的材料名</returns>
        public string GetPrefabName() { return 预制件; }




    }
}
