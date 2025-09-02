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
using ValheimCatManager.Config;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;
using static Player;

namespace ValheimCatManager.Managers
{
    public class PieceManager
    {

        /// <summary>
        /// 注：自定义物件字典-注册用
        /// </summary>
        public readonly Dictionary<int, PieceConfig> customPieceDict = new Dictionary<int, PieceConfig>();

        // 内部字典，存储vanilla（原生）分类的标签
        private static readonly Dictionary<Piece.PieceCategory, string> vanillaLabels = new Dictionary<Piece.PieceCategory, string>();

        /// <summary>
        /// 注：英灵神殿原目录
        /// </summary>
        private readonly List<string> valheimPieceCategory = new() { "Misc", "Crafting", "BuildingWorkbench", "BuildingWorkbench", "BuildingStonecutter", "Furniture", "Feasts", "Food", "Meads", "Max", "All" };

        /// <summary>
        /// 注：自定义目录字典-注册用
        /// </summary>
        private readonly Dictionary<string, Piece.PieceCategory> customCategoryDict = new Dictionary<string, Piece.PieceCategory>();

        private bool HarmonyCheck1 = true;

        private bool HarmonyCheck2 = true;

        private static bool RefreshCategoriesCheck { set; get; } = true;

        private static PieceManager _instance;

        public static PieceManager Instance => _instance ?? (_instance = new PieceManager());
        private Harmony harmony;

        private PieceManager()
        {
            harmony = new Harmony("PieceManager");
            harmony.PatchAll(typeof(PiecePatch));
        }



        private static class PiecePatch
        {
            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(Priority.VeryLow)]
            static void RegisterPiecePatch(ObjectDB __instance)
            {
                Instance.RegisterCategory();
                if (SceneManager.GetActiveScene().name == "main")
                {
                    Instance.RegisterPiece(Instance.customPieceDict);
                }

            }

        }


        private static class PieceCategoryPatch
        {


            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(101)]
            static void RefreshCategories()
            {
                if (SceneManager.GetActiveScene().name == "main")
                {
                    Instance.RefreshCategories();
                }

            }

            /// <summary>
            /// 注：对PieceTable.UpdateAvailable方法的Transpiler补丁，用于修改与最大分类值相关的IL指令
            /// </summary>
            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // 调用TranspileMaxCategory处理指令，最大偏移量为0
                return Instance.TranspileMaxCategory(instructions, 0);
            }

            [HarmonyPatch(typeof(Enum), nameof(Enum.GetValues)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
            static void EnumGetEnumValues(Type enumType, ref Array __result) => Instance.EnumGetPieceCategoryValues(enumType, ref __result);

            [HarmonyPatch(typeof(Enum), nameof(Enum.GetNames)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
            static void EnumGetEnumNames(Type enumType, ref string[] __result) => Instance.EnumGetPieceCategoryNames(enumType, ref __result);


            /// <summary>
            /// 注：对PieceTable.UpdateAvailable方法的Prefix补丁，用于在更新可用部件前扩展可用部件列表
            /// </summary>
            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), HarmonyPrefix]
            /// <summary>
            /// 注：在UpdateAvailable执行前调用，扩展部件表的可用部件列表
            /// </summary>
            /// <param name="__instance">当前PieceTable实例</param>
            static void PiecesTableUpdateAvailablePrefix(PieceTable __instance) => Instance.ExpandAvailablePieces(__instance);


            /// <summary>
            /// 注：对PieceTable.UpdateAvailable方法的Postfix补丁，用于在更新可用部件后调整数组和重排序部件
            /// </summary>
            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), HarmonyPostfix]
            /// <summary>
            /// 注：在UpdateAvailable执行后调用，调整部件表数组大小并重新排序所有分类的部件
            /// </summary>
            /// <param name="__instance">当前PieceTable实例</param>
            static void PiecesTableUpdateAvailablePostfix(PieceTable __instance)
            {
                Instance.AdjustPieceTableArray(__instance);
                Instance.ReorderAllCategoryPieces(__instance);
            }


            /// <summary>
            /// 注：对Player.SetPlaceMode方法的Postfix补丁，用于在切换放置模式后刷新分类标签
            /// </summary>
            [HarmonyPatch(typeof(Player), nameof(Player.SetPlaceMode)), HarmonyPostfix, HarmonyPriority(Priority.Low)]
            static void PlayerSetPlaceModePostfix() => Instance.RefreshCategories();

            /// <summary>
            /// 注：对Hud.Awake方法的Postfix补丁，用于在HUD初始化时刷新分类标签
            /// </summary>
            [HarmonyPatch(typeof(Hud), nameof(Hud.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Low)]
            static void HudAwakePostfix() => Instance.RefreshCategories();

            /// <summary>
            /// 注：对Hud.UpdateBuild方法的Postfix补丁，用于在构建界面更新时检查是否需要刷新分类
            /// </summary>
            [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBuild)), HarmonyPostfix, HarmonyPriority(Priority.Low)]
            static void HudUpdateBuildPostfix() => Instance.RefreshCategoriesIfNeeded();

            /// <summary>
            /// 注：对Hud.LateUpdate方法的Postfix补丁，用于在HUD晚更新时检查是否需要刷新分类
            /// </summary>
            [HarmonyPatch(typeof(Hud), nameof(Hud.LateUpdate)), HarmonyPostfix, HarmonyPriority(Priority.Low)]
            static void HudLateUpdatePostfix() => Instance.RefreshCategoriesIfNeeded();


        }





        #region 第一部分：注册 Category + Piece

        /// <summary>
        /// 注：读取PieceConfig配置，注册Piece到对应的PieceTable，并设置分类和资源需求
        /// </summary>
        /// <param name="pieceConfigDictionary">包含Piece配置的字典，键为配置ID，值为PieceConfig实例</param>
        private void RegisterPiece(Dictionary<int, PieceConfig> pieceConfigDictionary)
        {
            foreach (var pieceConfig in pieceConfigDictionary)
            {
                string pieceName = pieceConfig.Value.GetPrefabName();
                string categoryName = pieceConfig.Value.目录;
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

                Piece piece = piecePrefab.GetComponent<Piece>();

                if (piece == null)
                {
                    Debug.LogError($"执行RegisterPiece时，Piece：{pieceName}，对应的 【组件：Piece】 是空，已跳过");
                    continue;
                }


                if (!pieceTable.m_pieces.Contains(piecePrefab)) pieceTable.m_pieces.Add(piecePrefab);



                if (Instance.customCategoryDict.ContainsKey((categoryName)))
                {
                    if (!pieceTable.m_categoryLabels.Contains(categoryName))
                    {
                        pieceTable.m_categoryLabels.Add(categoryName);
                        pieceTable.m_categories.Add(GetPieceCategory(categoryName));
                    }
                }


                // 设置Piece的分类
                piece.m_category = GetPieceCategory(categoryName);

                // 若资源需求为空，则从配置中获取并设置
                if (piece.m_resources.Length != 0) continue;
                piece.m_resources = pieceConfig.Value.GetRequirementArr();
            }
        }





        /// <summary>
        /// 注：注册自定义物件的分类标签，避免重复注册
        /// </summary>
        private void RegisterCategory()
        {

            foreach (var item in Instance.customPieceDict)
            {
                if (!Instance.valheimPieceCategory.Contains(item.Value.目录))
                {
                    if (!Instance.customCategoryDict.ContainsKey(item.Value.目录))
                    {
                        int indx = Enum.GetNames(typeof(Piece.PieceCategory)).Length - 1;

                        Instance.customCategoryDict.Add(item.Value.目录, (Piece.PieceCategory)indx);

                        CreateCategoryTabs();

                        if (HarmonyCheck2)
                        {
                            harmony.PatchAll(typeof(PieceCategoryPatch));
                            HarmonyCheck2 = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 注：处理Piece.PieceCategory枚举的GetValues方法，添加自定义分类值到结果中
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="__result">枚举值数组的引用，用于输出包含自定义分类的结果</param>
        private void EnumGetPieceCategoryValues(Type enumType, ref Array __result)
        {
            if ((enumType == typeof(Piece.PieceCategory)) && Instance.customCategoryDict.Count != 0)
            {
                // 创建新数组：长度 = 原枚举值数组长度 + 自定义分类数量
                // 用于存储"原生枚举值 + 自定义分类"的全部内容
                Piece.PieceCategory[] array = new Piece.PieceCategory[__result.Length + Instance.customCategoryDict.Count];

                // 复制原生枚举值到新数组的起始位置（从索引0开始）
                __result.CopyTo(array, 0);

                // 复制自定义分类值到新数组的后续位置
                Instance.customCategoryDict.Values.CopyTo(array, __result.Length);

                __result = array;
            }
        }

        /// <summary>
        /// 注：处理Piece.PieceCategory枚举的GetNames方法，添加自定义分类名称到结果中
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="__result">枚举名称数组的引用，用于输出包含自定义分类名称的结果</param>
        private void EnumGetPieceCategoryNames(Type enumType, ref string[] __result)
        {
            if ((enumType == typeof(Piece.PieceCategory)) && Instance.customCategoryDict.Count != 0)
            {
                // 将自定义分类名称添加到原有名称数组中
                __result = __result.AddRangeToArray(Instance.customCategoryDict.Keys.ToArray());
            }
        }

        #endregion


        #region 第二部分：转换最大Category值

        /// <summary>
        /// 注：转换与最大分类值相关的IL指令，替换原有的MaxCategory调用或常量加载
        /// </summary>
        /// <param name="instructions">待处理的IL指令集合</param>
        /// <param name="maxOffset">最大分类值的偏移量，用于调整计算结果</param>
        /// <returns>处理后的IL指令集合</returns>
        private IEnumerable<CodeInstruction> TranspileMaxCategory(IEnumerable<CodeInstruction> instructions, int maxOffset)
        {
            // 计算基础最大分类值（从GetPieceCategory获取"Max"对应的枚举值）加上偏移量
            int number = (int)Instance.GetPieceCategory("Max") + maxOffset;

            // 遍历所有IL指令进行处理
            foreach (CodeInstruction instruction in instructions)
            {
                // 若指令是调用包含"MaxCategory"的方法，则替换为自定义的MaxCategory方法调用
                if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo && methodInfo.Name.Contains("MaxCategory"))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PieceManager), nameof(MaxCategory)));
                }
                // 若指令是加载上述计算的number常量，则替换为调用自定义MaxCategory方法，并根据偏移量处理
                else if (instruction.LoadsConstant(number))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PieceManager), nameof(MaxCategory)));

                    // 若偏移量不为0，添加偏移量并执行加法运算
                    if (maxOffset != 0)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4, maxOffset);
                        yield return new CodeInstruction(OpCodes.Add);
                    }
                }
                // 其他指令不做修改，直接返回
                else
                {
                    yield return instruction;
                }
            }
        }
        #endregion


        #region 第三部分：扩展PieceTable 调整PieceTable数组大小 重新排序所有Category

        /// <summary>
        /// 注：扩展部件表的可用部件列表，确保列表数量与分类数量匹配
        /// </summary>
        /// <param name="__instance">当前PieceTable实例</param>
        private void ExpandAvailablePieces(PieceTable __instance)
        {
            if (__instance.m_availablePieces.Count > 0)
            {
                // 计算分类总数与现有可用部件列表的差值，确定需要补充的列表数量
                int indx = (Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1) - __instance.m_availablePieces.Count;

                // 补充足够的空列表，使可用部件列表数量匹配分类数量
                for (int i = 0; i < indx; i++) __instance.m_availablePieces.Add(new List<Piece>());
            }
        }

        /// <summary>
        /// 注：调整部件表中相关数组的大小，使其与可用部件列表数量保持一致
        /// </summary>
        /// <param name="__instance">当前PieceTable实例</param>
        private void AdjustPieceTableArray(PieceTable __instance)
        {
            // 调整选中部件数组大小
            Array.Resize(ref __instance.m_selectedPiece, __instance.m_availablePieces.Count);
            // 调整上次选中部件数组大小
            Array.Resize(ref __instance.m_lastSelectedPiece, __instance.m_availablePieces.Count);
        }

        /// <summary>
        /// 注：重新排序所有分类的部件，将属于"All"分类的部件置于每个列表的开头
        /// </summary>
        /// <param name="__instance">当前PieceTable实例</param>
        private void ReorderAllCategoryPieces(PieceTable __instance)
        {
            // 获取所有部件的Piece组件列表
            List<Piece> pieces = __instance.m_pieces.Select(i => i.GetComponent<Piece>()).ToList();

            // 筛选出属于"All"分类的部件
            List<Piece> piecesWithAllCategory = pieces.FindAll(i => i && i.m_category == GetPieceCategory("All"));

            // 遍历每个可用部件列表，调整"All"分类部件的位置
            foreach (List<Piece> availablePieces in __instance.m_availablePieces)
            {
                int listPosition = 0;

                foreach (var piece in piecesWithAllCategory)
                {
                    // 移除已有实例（避免重复），并插入到列表开头（模拟原生行为）
                    availablePieces.Remove(piece);
                    availablePieces.Insert(Mathf.Min(listPosition, __instance.m_availablePieces.Count), piece);
                    listPosition++;
                }
            }
        }

        #endregion


        #region 第四部分：刷新分类标签


        /// <summary>
        /// 注：检查是否需要刷新分类，若需要则执行刷新并重置检查标志
        /// </summary>
        private void RefreshCategoriesIfNeeded()
        {
            if (RefreshCategoriesCheck)
            {
                RefreshCategoriesCheck = false;
                RefreshCategories();
            }
        }

        /// <summary>
        /// 注：刷新分类标签UI，调整布局，更新可见分类及部件表分类配置
        /// </summary>
        private void RefreshCategories()
        {
            // 确保所有分类标签都已正确创建
            CreateCategoryTabs();

            // 本地玩家不存在时直接返回
            if (!Player.m_localPlayer) return;

            // 获取当前建筑部件表
            PieceTable pieceTable = Player.m_localPlayer.m_buildPieces;
            if (!pieceTable) return;


            // 获取UI元素引用（分类标签、根节点、选择窗口）
            RectTransform firstTab = (RectTransform)Hud.instance.m_pieceCategoryTabs[0].transform;
            RectTransform categoryRoot = (RectTransform)Hud.instance.m_pieceCategoryRoot.transform;
            RectTransform selectionWindow = (RectTransform)Hud.instance.m_pieceSelectionWindow.transform;

            // 禁用水平布局组，使用自定义布局逻辑
            if (firstTab.parent.TryGetComponent<HorizontalLayoutGroup>(out var layoutGroup))
            {
                layoutGroup.enabled = false;
            }

            const int verticalSpacing = 1; // 标签垂直间距
            Vector2 tabSize = firstTab.rect.size; // 标签尺寸

            // 获取可见分类并更新部件表分类配置
            var visibleCategories = CategoriesInPieceTable(pieceTable);
            UpdatePieceTableCategories(pieceTable, visibleCategories);

            // 计算最大水平标签数量（根据根节点宽度）和可见标签总数
            int maxHorizontalTabs = Mathf.Max((int)(categoryRoot.rect.width / tabSize.x), 1);
            int visibleTabs = pieceTable.m_categories.Count;

            // 配置网格布局的约束数量（每行最大标签数）
            if (firstTab.parent.TryGetComponent<GridLayoutGroup>(out var gridLayoutGroup))
            {
                gridLayoutGroup.constraintCount = maxHorizontalTabs;
            }

            // 计算标签锚点基础位置
            float tabAnchorX = (-tabSize.x * maxHorizontalTabs) / 2f + tabSize.x / 2f;
            float tabAnchorY = (tabSize.y + verticalSpacing) * Mathf.Floor((float)(visibleTabs - 1) / maxHorizontalTabs) + 5f;
            Vector2 tabAnchor = new Vector2(tabAnchorX, tabAnchorY);

            int tabIndex = 0;

            // 定位每个分类标签的位置
            for (int i = 0; i < pieceTable.m_categories.Count; ++i)
            {
                GameObject tab = Hud.instance.m_pieceCategoryTabs[i];
                RectTransform rect = tab.GetComponent<RectTransform>();
                // 计算标签在网格中的x、y坐标
                float x = tabSize.x * (tabIndex % maxHorizontalTabs);
                float y = -(tabSize.y + verticalSpacing) * (Mathf.Floor((float)tabIndex / maxHorizontalTabs) + 0.5f);
                rect.anchoredPosition = tabAnchor + new Vector2(x, y);
                rect.anchorMin = new Vector2(0.5f, 1); // 锚点设置为顶部居中
                rect.anchorMax = new Vector2(0.5f, 1);
                tabIndex++;
            }

            // 调整分类选择窗口背景大小以适应标签布局
            RectTransform background = (RectTransform)selectionWindow.Find("Bkg2")?.transform;
            if (background)
            {
                float height = (tabSize.y + verticalSpacing) * Mathf.Max(0, Mathf.FloorToInt((float)(tabIndex - 1) / maxHorizontalTabs));
                background.offsetMax = new Vector2(background.offsetMax.x, height);
            }
            else
            {
                Debug.LogWarning("Category Refresh: 未找到背景图片，跳过尺寸调整");
            }

            // 刷新本地化文本
            Hud.instance.GetComponentInParent<Localize>().RefreshLocalization();
        }

        /// <summary>
        /// 注：创建缺失的分类标签，确保标签数量与分类总数匹配
        /// </summary>
        private void CreateCategoryTabs()
        {
            if (!Hud.instance) return;

            // 获取分类枚举的最大索引值
            int maxCategory = Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1;

            // 为每个尚未创建的分类添加标签
            for (int i = Hud.instance.m_pieceCategoryTabs.Length; i < maxCategory; i++)
            {
                GameObject tab = CreateCategoryTab(); // 创建新标签
                Hud.instance.m_pieceCategoryTabs = Hud.instance.m_pieceCategoryTabs.AddItem(tab).ToArray(); // 添加到标签数组
            }

            // 若本地玩家存在且有建筑部件表，更新可用部件列表
            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces)
            {
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }
        }

        /// <summary>
        /// 注：创建单个分类标签（以第一个标签为模板）
        /// </summary>
        /// <returns>新创建的分类标签GameObject</returns>
        private GameObject CreateCategoryTab()
        {
            // 以第一个标签为模板实例化新标签
            GameObject firstTab = Hud.instance.m_pieceCategoryTabs[0];
            GameObject newTab = UnityEngine.Object.Instantiate(firstTab, firstTab.transform.parent);
            newTab.SetActive(false); // 初始隐藏

            // 添加输入处理组件并绑定点击事件 - 这是正确的调用方式
            UIInputHandler handler = newTab.GetOrAddComponent<UIInputHandler>();
            handler.m_onLeftDown += Hud.instance.OnLeftClickCategory;

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

        /// <summary>
        /// 注：获取部件表中所有部件所属的分类（去重）
        /// </summary>
        /// <param name="pieceTable">目标部件表</param>
        /// <returns>包含所有可见分类的哈希集</returns>
        private HashSet<Piece.PieceCategory> CategoriesInPieceTable(PieceTable pieceTable)
        {
            HashSet<Piece.PieceCategory> categories = new HashSet<Piece.PieceCategory>();

            // 遍历所有部件，收集其所属分类
            foreach (GameObject piece in pieceTable.m_pieces)
            {
                categories.Add(piece.GetComponent<Piece>().m_category);
            }

            return categories;
        }

        /// <summary>
        /// 注：更新部件表的分类列表，添加可见分类并移除不可见分类（含香草分类和自定义分类）
        /// </summary>
        /// <param name="pieceTable">目标部件表</param>
        /// <param name="visibleCategories">可见分类集合</param>
        private void UpdatePieceTableCategories(PieceTable pieceTable, HashSet<Piece.PieceCategory> visibleCategories)
        {
            // 处理香草分类（原生游戏分类）
            for (int i = 0; i < (int)GetPieceCategory("Max"); i++)
            {
                Piece.PieceCategory category = (Piece.PieceCategory)i;

                // 添加可见的香草分类
                if (visibleCategories.Contains(category) && !pieceTable.m_categories.Contains(category))
                {
                    pieceTable.m_categories.Add(category);
                    pieceTable.m_categoryLabels.Add(GetVanillaLabel(category));
                }

                // 移除不可见的香草分类
                if (!visibleCategories.Contains(category) && pieceTable.m_categories.Contains(category))
                {
                    int index = pieceTable.m_categories.IndexOf(category);
                    pieceTable.m_categories.RemoveAt(index);
                    pieceTable.m_categoryLabels.RemoveAt(index);
                }
            }

            // 处理自定义分类
            foreach (var entry in Instance.customCategoryDict)
            {
                string name = entry.Key;
                Piece.PieceCategory category = entry.Value;

                // 添加可见的自定义分类
                if (visibleCategories.Contains(category) && !pieceTable.m_categories.Contains(category))
                {
                    pieceTable.m_categories.Add(category);
                    pieceTable.m_categoryLabels.Add(name);
                }

                // 移除不可见的自定义分类
                if (!visibleCategories.Contains(category) && pieceTable.m_categories.Contains(category))
                {
                    int index = pieceTable.m_categories.IndexOf(category);
                    pieceTable.m_categories.RemoveAt(index);
                    pieceTable.m_categoryLabels.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// 注：获取香草分类（原生游戏分类）的标签文本（从缓存中获取）
        /// </summary>
        /// <param name="category">目标分类</param>
        /// <returns>分类的标签文本，未找到则返回空字符串</returns>
        private string GetVanillaLabel(Piece.PieceCategory category)
        {
            if (!vanillaLabels.ContainsKey(category))
            {
                SearchVanillaLabels(); // 缓存中没有则搜索并缓存
            }

            return vanillaLabels.TryGetValue(category, out string label) ? label : string.Empty;
        }

        /// <summary>
        /// 注：搜索并缓存所有香草分类（原生游戏分类）的标签文本
        /// </summary>
        private void SearchVanillaLabels()
        {
            // 遍历所有部件表，收集分类标签
            foreach (var pieceTable in Resources.FindObjectsOfTypeAll<PieceTable>())
            {
                for (var i = 0; i < pieceTable.m_categories.Count; i++)
                {
                    var category = pieceTable.m_categories[i];

                    // 缓存未缓存的有效分类标签
                    if (i < pieceTable.m_categoryLabels.Count && !vanillaLabels.ContainsKey(category) && !string.IsNullOrEmpty(pieceTable.m_categoryLabels[i]))
                    {
                        vanillaLabels[category] = pieceTable.m_categoryLabels[i];
                    }
                }
            }
        }

        #endregion


        #region 辅助工具方法
        /// <summary>
        /// 注：工具方法---调试检测 PieceTable 缓存（CatModData.m_PieceTableCache）的状态，打印关键属性长度用于校验
        /// 1. 打印缓存中 PieceTable 的总数量
        /// 2. 逐个打印每个 PieceTable 的关键属性长度：m_categoryLabels（分类标签列表）、m_categories（分类列表）、m_availablePieces（可用部件列表）
        /// 用于排查分类相关列表长度不匹配的问题
        /// </summary>
        private void PieceTableCheck()
        {
            Debug.LogError($"[CatModData.m_PieceTableCache]的长度是：{CatModData.m_PieceTableCache.Count}");
            foreach (var PieceTable in CatModData.m_PieceTableCache)
            {
                Debug.LogError($"工具：{PieceTable.Key}，m_categoryLabels长度：{PieceTable.Value.m_categoryLabels.Count}，m_categories长度：{PieceTable.Value.m_categories.Count}，m_availablePieces长度：{PieceTable.Value.m_availablePieces.Count}");
            }
        }


        /// <summary>
        /// 注：工具方法---通过反射遍历 Piece.PieceCategory 枚举，获取与指定名称匹配的枚举值
        /// </summary>
        /// <param name="categoryName">要查找的分类名称（需与枚举名完全一致）</param>
        /// <returns>匹配的 Piece.PieceCategory 枚举值；未找到时打印错误日志并返回 Piece.PieceCategory.All</returns>
        private Piece.PieceCategory GetPieceCategory(string categoryName)
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
            Debug.LogError($"执行GetCategory时，未找到对应名 枚举名：[{categoryName}]，将返回：[Piece.PieceCategory.All]");
            return Piece.PieceCategory.All;
        }


        /// <summary>
        /// 注：工具方法---获取 Piece.PieceCategory 枚举的最大索引值，并处理偏移（避免与 All 分类冲突）
        /// 逻辑：All 分类索引为100，若枚举总长度-1（原生最大索引）≥ All 索引，则额外+1偏移，防止自定义分类索引与 All 重叠
        /// </summary>
        /// <returns>处理偏移后的最大分类索引值</returns>
        private static int MaxCategory()
        {
            return Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1;
        }



        #endregion
    }

}