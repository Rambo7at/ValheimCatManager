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
using ValheimCatManager.Data;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Managers
{
    internal class PieceManager
    {
        // 内部字典，存储vanilla（原生）分类的标签
        private static readonly Dictionary<Piece.PieceCategory, string> vanillaLabels = new Dictionary<Piece.PieceCategory, string>();

        private static bool categoryRefreshNeeded { set; get; } = true;

        //这个补丁注释 餐盘会正常 但是耕地耙不正常
        [HarmonyPatch(typeof(Player), nameof(Player.SetPlaceMode))]
        [HarmonyPriority(Priority.Low)]
        class RefreshCategoriesPatch
        {

            static void Postfix()
            {

                RefreshCategories();
                Debug.LogError($"执行了：RefreshCategories");
            }


        }



        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        [HarmonyPriority(Priority.Low)]
        class CreateCategoryTabsPatch
        {
            static void Postfix()
            {
                RefreshCategories();
                Debug.LogError($"执行了：RefreshCategories");
            }
        }


        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBuild))]
        [HarmonyPriority(Priority.Low)]
        class Hud_UpdateBuild
        {
            static void Postfix()
            {
                RefreshCategoriesIfNeeded();
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.LateUpdate))]
        [HarmonyPriority(Priority.Low)]
        class Hud_LateUpdate
        {
            static void Postfix()
            {
                RefreshCategoriesIfNeeded();
            }
        }




        [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
        [HarmonyPriority(Priority.Normal)] // 可根据需要调整优先级
        class UpdateAvailableTranspilerPatch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // 保持原有的转译逻辑，调用TranspileMaxCategory方法
                return TranspileMaxCategory(instructions, 0);
            }
        }







        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        [HarmonyPriority(1)]
        class AddPiecePatch
        {
            static void Postfix(ObjectDB __instance)
            {
                if (SceneManager.GetActiveScene().name == "main")
                {
                    RegisterCategory(); // 注册Category方法
                    RegisterPiece(CatModData.自定义物件_字典); // 注册Piece方法
                    RefreshCategories();
                    PieceTableCheck(); // 检测方法
                }
            }
        }

        [HarmonyPatch(typeof(PieceTable), "UpdateAvailable")]
        class ExpandAvailablePiecesPatch1
        {
            static void Prefix(PieceTable __instance)
            {


                ExpandAvailablePieces(__instance);
                Debug.LogError($"执行了：ExpandAvailablePieces");
                PieceTableCheck(); // 检测方法
            }

        }

        [HarmonyPatch(typeof(PieceTable), "UpdateAvailable")]
        class ExpandAvailablePiecesPatch2
        {
            static void Postfix(PieceTable __instance)
            {

                //AdjustPieceTableArray(__instance);
                //ReorderAllCategoryPieces(__instance);
                Debug.LogError($"执行了：AdjustPieceTableArray + ReorderAllCategoryPieces");
            }
        }


        private static void RefreshCategoriesIfNeeded()
        {
            if (categoryRefreshNeeded)
            {
                categoryRefreshNeeded = false;
                RefreshCategories();
            }
        }


        // 获取最大分类值
        private static int MaxCategory()
        {
            var cats = Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1;
            // 偏移所有在Piece.PieceCategory.All=100之后的分类
            return cats < (int)GetCategory("All") ? cats : cats + 1;
        }




        // 转换最大分类值的transpile方法
        private static IEnumerable<CodeInstruction> TranspileMaxCategory(IEnumerable<CodeInstruction> instructions, int maxOffset)
        {
            int number = (int)GetCategory("Max") + maxOffset;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo && methodInfo.Name.Contains("MaxCategory"))
                {
                    // 其他模组可能有旧的MaxCategory实现，覆盖它们
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PieceManager), nameof(MaxCategory)));
                }
                else if (instruction.LoadsConstant(number))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PieceManager), nameof(MaxCategory)));

                    if (maxOffset != 0)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4, maxOffset);
                        yield return new CodeInstruction(OpCodes.Add);
                    }
                }
                else
                {
                    yield return instruction;
                }
            }
        }











        // 扩展部件表的可用部件列表
        public static void ExpandAvailablePieces(PieceTable __instance)
        {
            if (__instance.m_availablePieces.Count > 0)
            {

                //Debug.LogError($"目录的最大值是：{GetCategoryMax()}");
                //Debug.LogError($"m_availablePieces最大值是：{__instance.m_availablePieces.Count}");
                int indx = (Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1) - __instance.m_availablePieces.Count;
                //Debug.LogError($"差值是：{indx}");

                for (int i = 0; i < indx; i++) __instance.m_availablePieces.Add(new List<Piece>());
            }
        }

        // 调整部件表数组大小以匹配可用部件数量
        public static void AdjustPieceTableArray(PieceTable __instance)
        {

            Array.Resize(ref __instance.m_selectedPiece, __instance.m_availablePieces.Count);
            Array.Resize(ref __instance.m_lastSelectedPiece, __instance.m_availablePieces.Count);
        }


        // 重新排序所有分类的部件
        public static void ReorderAllCategoryPieces(PieceTable __instance)
        {

            List<Piece> pieces = __instance.m_pieces.Select(i => i.GetComponent<Piece>()).ToList();

            List<Piece> piecesWithAllCategory = pieces.FindAll(i => i && i.m_category == GetCategory("All"));

            foreach (List<Piece> availablePieces in __instance.m_availablePieces)
            {
                int listPosition = 0;

                foreach (var piece in piecesWithAllCategory)
                {
                    // m_availablePieces已填充。将部件添加到列表开头，以复制vanilla行为
                    availablePieces.Remove(piece);
                    availablePieces.Insert(Mathf.Min(listPosition, __instance.m_availablePieces.Count), piece);
                    listPosition++;
                }
            }
        }










        private static GameObject CreateCategoryTab()
        {
            // 复制第一个标签作为模板
            GameObject firstTab = Hud.instance.m_pieceCategoryTabs[0];
            GameObject newTab = UnityEngine.Object.Instantiate(firstTab, firstTab.transform.parent);
            newTab.SetActive(false);

            //// 添加输入处理组件
            //UIInputHandler handler = newTab.GetOrAddComponent<UIInputHandler>();
            //handler.m_onLeftDown += Hud.instance.OnLeftClickCategory;

            // 调整文本组件属性
            foreach (var text in newTab.GetComponentsInChildren<TMP_Text>(true))
            {
                text.rectTransform.offsetMin = new Vector2(3, 1);
                text.rectTransform.offsetMax = new Vector2(-3, -1);
                text.enableAutoSizing = true;
                text.fontSizeMin = 10;
                text.fontSizeMax = 20;
                text.lineSpacing = 0.8f;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Truncate;
            }

            return newTab;
        }


        private static void CreateCategoryTabs()
        {
            if (!Hud.instance)
            {
                return;
            }

            int maxCategory = Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1;

            // 为每个尚未添加的自定义分类添加标签及其名称到GUI
            for (int i = Hud.instance.m_pieceCategoryTabs.Length; i < maxCategory; i++)
            {
                GameObject tab = CreateCategoryTab();
                Hud.instance.m_pieceCategoryTabs = Hud.instance.m_pieceCategoryTabs.AddItem(tab).ToArray();
            }

            // 如果本地玩家存在且有建筑部件表，更新可用部件列表
            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces)
            {
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }
        }


        private static HashSet<Piece.PieceCategory> CategoriesInPieceTable(PieceTable pieceTable)
        {
            HashSet<Piece.PieceCategory> categories = new HashSet<Piece.PieceCategory>();

            foreach (GameObject piece in pieceTable.m_pieces)
            {
                categories.Add(piece.GetComponent<Piece>().m_category);
            }

            return categories;
        }


        private static void RefreshCategories()
        {
            // 确保所有分类标签都已正确创建
            CreateCategoryTabs();

            if (!Player.m_localPlayer)
            {
                Debug.LogError("执行RefreshCategories时，Player.m_localPlayer是空");
                return;
            }

            PieceTable pieceTable = Player.m_localPlayer.m_buildPieces;

            if (!pieceTable)
            {
                Debug.LogError("执行RefreshCategories时，pieceTable是空");
                return;
            }


            // 获取UI元素引用
            RectTransform firstTab = (RectTransform)Hud.instance.m_pieceCategoryTabs[0].transform;
            RectTransform categoryRoot = (RectTransform)Hud.instance.m_pieceCategoryRoot.transform;
            RectTransform selectionWindow = (RectTransform)Hud.instance.m_pieceSelectionWindow.transform;

            // 禁用水平布局组（使用自定义布局）
            if (firstTab.parent.TryGetComponent<HorizontalLayoutGroup>(out var layoutGroup))
            {
                layoutGroup.enabled = false;
            }

            const int verticalSpacing = 1;
            Vector2 tabSize = firstTab.rect.size;

            // 获取可见分类并更新部件表分类
            var visibleCategories = CategoriesInPieceTable(pieceTable);
            UpdatePieceTableCategories(pieceTable, visibleCategories);

            // 计算最大水平标签数量和可见标签数量
            int maxHorizontalTabs = Mathf.Max((int)(categoryRoot.rect.width / tabSize.x), 1);
            int visibleTabs = pieceTable.m_categories.Count;

            // 配置网格布局
            if (firstTab.parent.TryGetComponent<GridLayoutGroup>(out var gridLayoutGroup))
            {
                gridLayoutGroup.constraintCount = maxHorizontalTabs;
            }

            // 计算标签锚点位置
            float tabAnchorX = (-tabSize.x * maxHorizontalTabs) / 2f + tabSize.x / 2f;
            float tabAnchorY = (tabSize.y + verticalSpacing) * Mathf.Floor((float)(visibleTabs - 1) / maxHorizontalTabs) + 5f;
            Vector2 tabAnchor = new Vector2(tabAnchorX, tabAnchorY);

            int tabIndex = 0;

            // 定位每个标签
            for (int i = 0; i < pieceTable.m_categories.Count; ++i)
            {
                GameObject tab = Hud.instance.m_pieceCategoryTabs[i];
                RectTransform rect = tab.GetComponent<RectTransform>();
                float x = tabSize.x * (tabIndex % maxHorizontalTabs);
                float y = -(tabSize.y + verticalSpacing) * (Mathf.Floor((float)tabIndex / maxHorizontalTabs) + 0.5f);
                rect.anchoredPosition = tabAnchor + new Vector2(x, y);
                rect.anchorMin = new Vector2(0.5f, 1);
                rect.anchorMax = new Vector2(0.5f, 1);
                tabIndex++;
            }

            // 调整背景大小
            RectTransform background = (RectTransform)selectionWindow.Find("Bkg2")?.transform;

            if (background)
            {
                float height = (tabSize.y + verticalSpacing) * Mathf.Max(0, Mathf.FloorToInt((float)(tabIndex - 1) / maxHorizontalTabs));
                background.offsetMax = new Vector2(background.offsetMax.x, height);
            }
            else
            {
                Debug.LogWarning("Category Refresh: Could not find background image, skipping resize");
            }


            Hud.instance.GetComponentInParent<Localize>().RefreshLocalization();
        }




        private static void UpdatePieceTableCategories(PieceTable pieceTable, HashSet<Piece.PieceCategory> visibleCategories)
        {
            // 处理vanilla分类
            for (int i = 0; i < (int)GetCategory("Max"); i++)
            {
                Piece.PieceCategory category = (Piece.PieceCategory)i;

                // 添加可见分类
                if (visibleCategories.Contains(category) && !pieceTable.m_categories.Contains(category))
                {
                    pieceTable.m_categories.Add(category);
                    pieceTable.m_categoryLabels.Add(GetVanillaLabel(category));
                }

                // 移除不可见分类
                if (!visibleCategories.Contains(category) && pieceTable.m_categories.Contains(category))
                {
                    int index = pieceTable.m_categories.IndexOf(category);
                    pieceTable.m_categories.RemoveAt(index);
                    pieceTable.m_categoryLabels.RemoveAt(index);
                }
            }

            // 处理自定义分类
            foreach (var entry in CatModData.自定义目录_字典)
            {
                string name = entry.Key;
                Piece.PieceCategory category = entry.Value;

                // 添加可见分类
                if (visibleCategories.Contains(category) && !pieceTable.m_categories.Contains(category))
                {
                    pieceTable.m_categories.Add(category);
                    pieceTable.m_categoryLabels.Add(name);
                }

                // 移除不可见分类
                if (!visibleCategories.Contains(category) && pieceTable.m_categories.Contains(category))
                {
                    int index = pieceTable.m_categories.IndexOf(category);
                    pieceTable.m_categories.RemoveAt(index);
                    pieceTable.m_categoryLabels.RemoveAt(index);
                }
            }
        }


        // 获取vanilla分类的标签
        private static string GetVanillaLabel(Piece.PieceCategory category)
        {
            if (!vanillaLabels.ContainsKey(category))
            {
                SearchVanillaLabels();
            }

            return vanillaLabels.TryGetValue(category, out string label) ? label : string.Empty;
        }

        private static void SearchVanillaLabels()
        {
            foreach (var pieceTable in Resources.FindObjectsOfTypeAll<PieceTable>())
            {
                for (var i = 0; i < pieceTable.m_categories.Count; i++)
                {
                    var category = pieceTable.m_categories[i];

                    // 缓存未缓存的分类标签
                    if (i < pieceTable.m_categoryLabels.Count && !vanillaLabels.ContainsKey(category) && !string.IsNullOrEmpty(pieceTable.m_categoryLabels[i]))
                    {
                        vanillaLabels[category] = pieceTable.m_categoryLabels[i];
                    }
                }
            }
        }

        /// <summary>
        /// 注：工具方法---检测 PieceTable 的 m_categoryLabels 和 m_categories 长度是否匹配
        /// </summary>
        static void PieceTableCheck()
        {
            Debug.LogError($"目录缓存的长度是：{CatModData.m_PieceTableCache.Count}");

            foreach (var PieceTable in CatModData.m_PieceTableCache)
            {

                Debug.LogError($"工具：{PieceTable.Key}，m_categoryLabels长度：{PieceTable.Value.m_categoryLabels.Count}，m_categories长度：{PieceTable.Value.m_categories.Count}，m_availablePieces长度：{PieceTable.Value.m_availablePieces.Count}");

            }
        }








        #region 已经完成的功能：注册Piece和Category  

        /// <summary>
        /// 注：注册预制件 给对应的制作工具
        /// <br>优化考虑：可以准备一个 Category 的缓存</br>
        /// </summary>
        /// <param name="pieceConfigDictionary"></param>
        public static void RegisterPiece(Dictionary<int, PieceConfig> pieceConfigDictionary)
        {

            foreach (var pieceConfig in pieceConfigDictionary)
            {
                string pieceName = pieceConfig.Value.GetPrefabName();
                string categoryName = pieceConfig.Value.分组;
                PieceTable pieceTable = pieceConfig.Value.GetPieceTable();
                GameObject piecePrefab = CatToolManager.GetGameObject(pieceConfig.Key);


                if (piecePrefab == null)
                {
                    Debug.LogError($"执行RegisterPiece时，Piece：{pieceName}，预制件是空，已跳过");
                    continue;
                }

                if (pieceTable == null)
                {
                    Debug.LogError($"执行RegisterPiece时，Piece：{pieceName}，对应的 【组件：pieceTable】 是空，已跳过");
                    continue;
                }
                //EnsureAvailablePiecesLength(pieceTable);
                Piece piece = piecePrefab.GetComponent<Piece>();
                if (piece == null)
                {
                    Debug.LogError($"执行RegisterPiece时，Piece：{pieceName}，对应的 【组件：Piece】 是空，已跳过");
                    continue;
                }

                if (!pieceTable.m_pieces.Contains(piecePrefab)) pieceTable.m_pieces.Add(piecePrefab);

                if (!pieceTable.m_categoryLabels.Contains(categoryName))
                {
                    Debug.LogError($"物件：{pieceConfig.Value.制作工具}，m_categoryLabels，长度：{pieceTable.m_categoryLabels.Count}，对应的 m_categories 长度：{pieceTable.m_categories.Count}");
                    pieceTable.m_categoryLabels.Add(categoryName);
                    pieceTable.m_categories.Add(GetCategory(categoryName));
                    Debug.LogError($"物件：{pieceConfig.Value.制作工具}，m_categoryLabels，长度：{pieceTable.m_categoryLabels.Count}，对应的 m_categories 长度：{pieceTable.m_categories.Count}");
                }
                piece.m_category = GetCategory(categoryName);

                if (piece.m_resources.Length != 0) continue;

                piece.m_resources = pieceConfig.Value.GetRequirementS();

            }


        }

        /// <summary>
        /// 注：注册自定义 Category
        /// <br><paramref name="Category"></paramref></br>：
        /// 指的是 Piece上方的目录条
        /// </summary>
        public static void RegisterCategory()
        {
            foreach (var item in CatModData.自定义物件_字典)
            {
                if (!CatModData.自定义目录_字典.ContainsKey(item.Value.分组))
                {
                    int indx = Enum.GetNames(typeof(Piece.PieceCategory)).Length - 1;
                    CatModData.自定义目录_字典.Add(item.Value.分组, (Piece.PieceCategory)indx);
                    CreateCategoryTabs();

                }
            }

        }
        /// <summary>
        /// 注：通过反射 游戏枚举Piece.PieceCategory 获取枚举值
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns>Piece.PieceCategory</returns>
        private static Piece.PieceCategory GetCategory(string categoryName)
        {
            Array enumValues = Enum.GetValues(typeof(Piece.PieceCategory));
            string[] enumNames = Enum.GetNames(typeof(Piece.PieceCategory));

            for (int i = 0; i < enumNames.Length; i++)
            {
                if (enumNames[i] == categoryName)
                {
                    return (Piece.PieceCategory)enumValues.GetValue(i);
                }
            }
            Debug.LogError($"执行GetCategory时，未找到对应名 枚举名：{categoryName}");
            return Piece.PieceCategory.All;
        }




        public static void EnumGetPieceCategoryValues(Type enumType, ref Array __result)
        {
            if ((enumType == typeof(Piece.PieceCategory)) && CatModData.自定义目录_字典.Count != 0)
            {
                // 创建新数组：长度 = 原枚举值数组长度 + 自定义分类数量
                // 用于存储"原生枚举值 + 自定义分类"的全部内容
                Piece.PieceCategory[] array = new Piece.PieceCategory[__result.Length + CatModData.自定义目录_字典.Count];

                // 复制原生枚举值到新数组的起始位置（从索引0开始）
                __result.CopyTo(array, 0);

                CatModData.自定义目录_字典.Values.CopyTo(array, __result.Length);

                __result = array;
            }
        }

        public static void EnumGetPieceCategoryNames(Type enumType, ref string[] __result)
        {
            if ((enumType == typeof(Piece.PieceCategory)) && CatModData.自定义目录_字典.Count != 0)
            {
                __result = __result.AddRangeToArray(CatModData.自定义目录_字典.Keys.ToArray());
            }
        }


        [HarmonyPatch(typeof(Enum), "GetValues")]
        [HarmonyPriority(Priority.Normal)]
        class EnumGetValuesPatch { static void Postfix(Type enumType, ref Array __result) => EnumGetPieceCategoryValues(enumType, ref __result); }

        [HarmonyPatch(typeof(Enum), "GetNames")]
        [HarmonyPriority(Priority.Normal)]
        class EnumGetNamesPatch { static void Postfix(Type enumType, ref string[] __result) => EnumGetPieceCategoryNames(enumType, ref __result); }

        #endregion

    }




}