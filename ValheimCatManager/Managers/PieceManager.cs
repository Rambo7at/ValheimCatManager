using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValheimCatManager;
using ValheimCatManager.CatUtils;
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using ValheimCatManager.Managers;
using static Player;

namespace ValheimCatManager.Managers
{
    public class PieceManager
    {
        private static PieceManager _instance;

        public static PieceManager Instance => _instance ?? (_instance = new PieceManager());

        private PieceManager()
        {

            PatchManager.OnObjectDBAwakeModify += (OBJ) => InitPieceToolDict();

            PatchManager.OnObjectDBAwakeModify += (OBJ) => RegisterCustomPiece();
        }

        public enum E_CraftTool
        {
            Hammer = 0,
            Feaster = 1,
            Cultivator = 2,
        }

        /// <summary>注：自定义物件字典-注册用 </summary>
        public readonly Dictionary<int, PieceConfig> customPieceDict = new Dictionary<int, PieceConfig>();

        private readonly Dictionary<E_CraftTool, GameObject> PieceTableDict = [];

        private void RegisterCustomPiece()
        {
            if (PieceTableDict[E_CraftTool.Hammer].GetComponent<PieceTable>() is not PieceTable hmamerTab)
            {
                Debug.LogError("没有找到 Hammer 的 PieceTable组件");
                return;
            }


            if (PieceTableDict[E_CraftTool.Feaster].GetComponent<PieceTable>() is not PieceTable feasterTable)
            {
                Debug.LogError("没有找到 Feaster 的 PieceTable组件");
                return;
            }


            if (PieceTableDict[E_CraftTool.Cultivator].GetComponent<PieceTable>() is not PieceTable cultivatorTable)
            {
                Debug.LogError("没有找到 Cultivator 的 PieceTable组件");
                return;
            }


            foreach (var cfg in customPieceDict.Values)
            {
                if (cfg == null) continue;


                string Prefabname = cfg.GetPrefabName();

                if (CatTool.GetGameObject(Prefabname) is not GameObject piecePrefab)
                {
                    Debug.LogWarning($"[RegisterCustomPiece] 注册过程中没有找到对应预制件 {Prefabname} ,已跳过");
                    continue;
                }


                if (piecePrefab.GetComponent<Piece>() is not Piece pieceComp)
                {
                    Debug.LogWarning($"[RegisterCustomPiece] 注册过程 预制件[{Prefabname}] ,没有找到 Piece 组件 已跳过");
                    continue;
                }

                if (pieceComp.m_usage != cfg.目录)
                {
                    pieceComp.m_usage = cfg.目录;
                }


                pieceComp.m_resources = cfg.GetRequirementArr();



                switch (cfg.制作工具)
                {
                    case E_CraftTool.Hammer:
                        hmamerTab.m_pieces.Add(piecePrefab);
                        break;
                    case E_CraftTool.Feaster:
                        feasterTable.m_pieces.Add(piecePrefab);
                        break;
                    case E_CraftTool.Cultivator:
                        cultivatorTable.m_pieces.Add(piecePrefab);
                        break;
                }


            }


        }

        private void InitPieceToolDict()
        {

            var hammerTable = CatTool.GetGameObject("_HammerPieceTable");
            var feasterTable = CatTool.GetGameObject("_FeasterPieceTable");
            var cultivatorTable = CatTool.GetGameObject("_CultivatorPieceTable");


            if (hammerTable == null) Debug.LogError("hammer 表是空的");

            if (feasterTable == null) Debug.LogError("feaster 表是空的");

            if (cultivatorTable == null) Debug.LogError("cultivator 表是空的");



            PieceTableDict[E_CraftTool.Hammer] = hammerTable;
            PieceTableDict[E_CraftTool.Feaster] = feasterTable;
            PieceTableDict[E_CraftTool.Cultivator] = cultivatorTable;
        }

    }

}