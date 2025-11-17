using HarmonyLib;
using SoftReferenceableAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.ValheimCatManager.Config;
using ValheimCatManager.ValheimCatManager.Tool;
using static DungeonDB;

namespace ValheimCatManager.ValheimCatManager.Deprecated
{
    [Obsolete("这是EW的房间建立方式", false)]
    public class DungeonThemeEW
    {
        private static DungeonThemeEW _instance;
        public static DungeonThemeEW Instance => _instance ?? (_instance = new DungeonThemeEW());

        private DungeonThemeEW() => new Harmony("DungeonManager").PatchAll(typeof(DungeonPatch));

        /// <summary>
        /// 注：待注册的房间列表
        /// </summary>
        public readonly List<RoomConfig> roomList = new();
        /// <summary>
        /// 注：自定义主题列表
        /// </summary>
        private readonly List<string> customThemeList = new();
        /// <summary>
        /// 注：房间对应的主题字典
        /// </summary>
        private readonly Dictionary<string, List<RoomData>> themeRoomsDict = new();
        /// <summary>
        /// 注：地牢房间的软引用
        /// </summary>
        private Dictionary<RoomConfig, SoftReference<GameObject>> roomSoftReferences;

        // 新增：枚举扩展相关字段
        private readonly Dictionary<string, Room.Theme> CustomThemeToEnum = new();
        private readonly Dictionary<Room.Theme, string> EnumToCustomTheme = new();



        // Harmony补丁
        // Harmony补丁
        // Harmony补丁
        private class DungeonPatch
        {
            [HarmonyPatch(typeof(DungeonDB), nameof(DungeonDB.Start)), HarmonyPostfix, HarmonyPriority(1000)]
            static void RegisterDungeonRooms(DungeonDB __instance) => Instance.RegisterDungeonRooms(__instance, Instance.roomList);

            // ========== 枚举扩展补丁 ==========
            [HarmonyPatch(typeof(Enum), nameof(Enum.GetValues)), HarmonyPostfix]
            static void GetValuesPatch(Type enumType, ref Array __result)
            {
                if (enumType == typeof(Room.Theme))
                {
                    // 获取当前所有枚举值（包括EW的）
                    var currentValues = (Room.Theme[])__result;

                    // 获取我们的自定义值
                    var ourValues = Instance.EnumToCustomTheme.Keys.ToArray();

                    // 智能合并：优先保留我们的值，避免重复
                    var valueDict = new Dictionary<int, Room.Theme>();

                    // 先添加所有当前值
                    foreach (var value in currentValues)
                    {
                        valueDict[(int)value] = value;
                    }

                    // 用我们的值覆盖冲突的值
                    foreach (var value in ourValues)
                    {
                        valueDict[(int)value] = value;
                    }

                    __result = valueDict.Values.ToArray();

                    Debug.Log($"[DungeonManager] 智能合并枚举值: {string.Join(", ", valueDict.Values.Select(v => $"{v}({(int)v})"))}");
                }
            }

            [HarmonyPatch(typeof(Enum), nameof(Enum.GetNames)), HarmonyPostfix]
            static void GetNamesPatch(Type enumType, ref string[] __result)
            {
                if (enumType == typeof(Room.Theme))
                {
                    // 获取当前所有名称（包括EW的）
                    var currentNames = __result;

                    // 构建名称映射字典
                    var nameDict = new Dictionary<int, string>();

                    // 先添加所有当前名称
                    for (int i = 0; i < currentNames.Length; i++)
                    {
                        var name = currentNames[i];
                        var value = (int)Enum.Parse(enumType, name);
                        nameDict[value] = name;
                    }

                    // 用我们的名称覆盖冲突的值
                    foreach (var kvp in Instance.EnumToCustomTheme)
                    {
                        nameDict[(int)kvp.Key] = kvp.Value;
                    }

                    __result = nameDict.Values.ToArray();

                    Debug.Log($"[DungeonManager] 智能合并枚举名称: {string.Join(", ", nameDict.Values)}");
                }
            }

            [HarmonyPatch(typeof(Enum), nameof(Enum.GetName)), HarmonyPostfix]
            static void GetNamePatch(Type enumType, object value, ref string __result)
            {
                if (enumType == typeof(Room.Theme))
                {
                    // 如果原版方法返回null或空，尝试我们的自定义主题
                    if (string.IsNullOrEmpty(__result))
                    {
                        if (Instance.EnumToCustomTheme.TryGetValue((Room.Theme)value, out var customName))
                        {
                            __result = customName;
                            Debug.Log($"[DungeonManager] GetName补丁设置自定义名称: {value} -> {customName}");
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(Enum), nameof(Enum.Parse), typeof(Type), typeof(string)), HarmonyPostfix]
            static void EnumParsePatch(Type enumType, string value, ref object __result)
            {
                if (enumType == typeof(Room.Theme) && (__result == null || (Room.Theme)__result == Room.Theme.None))
                {
                    // 如果原版解析失败，尝试我们的自定义主题
                    if (Instance.CustomThemeToEnum.TryGetValue(value.ToLowerInvariant(), out var theme))
                    {
                        __result = theme;
                        Debug.Log($"[DungeonManager] EnumParse补丁设置自定义主题: {value} -> {theme}");
                    }
                }
            }

            [HarmonyPatch(typeof(Enum), nameof(Enum.Parse), typeof(Type), typeof(string), typeof(bool)), HarmonyPostfix]
            static void ParseIgnoreCasePatch(Type enumType, string value, ref object __result)
            {
                if (enumType == typeof(Room.Theme) && (__result == null || (Room.Theme)__result == Room.Theme.None))
                {
                    // 如果原版解析失败，尝试我们的自定义主题
                    if (Instance.CustomThemeToEnum.TryGetValue(value.ToLowerInvariant(), out var theme))
                    {
                        __result = theme;
                        Debug.Log($"[DungeonManager] ParseIgnoreCase补丁设置自定义主题: {value} -> {theme}");
                    }
                }
            }
        }

        /// <summary>
        /// 注：注册地下城房间到DungeonDB
        /// </summary>
        private void RegisterDungeonRooms(DungeonDB instance, List<RoomConfig> roomConfigs)
        {
            if (roomConfigs.Count == 0) return;
            roomSoftReferences ??= new Dictionary<RoomConfig, SoftReference<GameObject>>();


            Debug.Log($"[DungeonManager] 开始注册 {roomConfigs.Count} 个房间");

            foreach (var roomConfig in roomConfigs)
            {
                if (roomConfig == null) continue;

                // 获取RoomData
                RoomData roomData = roomConfig.GetRoomData();
                if (roomData == null)
                {
                    Debug.LogError($"执行[RegisterDungeonRooms]方法出错原因：GetRoomData返回null: {roomConfig.预制件?.name}");
                    continue;
                }

                // 验证Room组件
                Room roomComponent = roomConfig.预制件.GetComponent<Room>();
                if (roomComponent == null)
                {
                    Debug.LogError($"执行[RegisterDungeonRooms]方法出错原因：预制件[{roomConfig.预制件.name}]缺少Room组件");
                    continue;
                }


                roomData.m_loadedRoom = roomComponent;

                // 处理软引用
                if (!roomSoftReferences.TryGetValue(roomConfig, out var softRef))
                {
                    softRef = CatToolManagerOld.AddLoadedSoftReferenceAsset(roomConfig.预制件);
                    roomSoftReferences[roomConfig] = softRef;
                }

                // 设置RoomData
                roomData.m_prefab = softRef;

                // 添加到DungeonDB
                instance.m_rooms.Add(roomData);
            }

            // 重建哈希表
            RebuildRoomHash(instance);
            Debug.Log($"[DungeonManager] 房间注册完成，总房间数: {instance.m_rooms.Count}");
        }

        /// <summary>
        /// 注册自定义主题
        /// </summary>
        public void RegisterDungeonTheme(GameObject prefab, string customTheme)
        {
            DungeonGenerator dungeonGenerator = prefab.GetComponentInChildren<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogError($"[DungeonManager] 预制件缺少DungeonGenerator组件: {prefab.name}");
                return;
            }

            // 为这个自定义主题分配枚举值
            if (!CustomThemeToEnum.ContainsKey(customTheme))
            {
                Room.Theme themeEnum = GenerateNewThemeValue();
                CustomThemeToEnum[customTheme] = themeEnum;
                EnumToCustomTheme[themeEnum] = customTheme;

                Debug.Log($"[DungeonManager] 映射自定义主题到枚举: {customTheme} -> {themeEnum}");
            }

            dungeonGenerator.m_themes = CatToolManagerOld.GetTheme(customTheme);


            Debug.Log($"[DungeonManager] 成功注册主题: {customTheme} -> 生成器: {prefab.name}");
        }



        /// <summary>
        /// 生成新的主题枚举值 - 高范围版本
        /// </summary>
        private Room.Theme GenerateNewThemeValue()
        {
            // 使用更高的起始值（2^20 = 1048576），避免与EW冲突
            int baseValue = 1048576;

            // 获取当前所有枚举值
            var allValues = Enum.GetValues(typeof(Room.Theme)).Cast<Room.Theme>().Select(v => (int)v).ToArray();

            int newValue = baseValue;

            // 确保新值不与现有值冲突
            while (allValues.Contains(newValue) || Instance.EnumToCustomTheme.ContainsKey((Room.Theme)newValue))
            {
                newValue *= 2;
                if (newValue > int.MaxValue / 2) // 避免溢出
                {
                    Debug.LogError("[DungeonManager] 无法找到可用的枚举值！");
                    return Room.Theme.None;
                }
            }

            Debug.Log($"[DungeonManager] 生成高范围枚举值: {newValue}");
            return (Room.Theme)newValue;
        }












        /// <summary>
        /// 工具方法：检测主题是否是已有原版或自定主题对应
        /// </summary>
        private bool CheckRoomTheme(string themeName)
        {
            if (Enum.TryParse<Room.Theme>(themeName, out _)) return true;
            return customThemeList.Contains(themeName);
        }








        /// <summary>
        /// 重建房间哈希表
        /// </summary>
        private void RebuildRoomHash(DungeonDB instance)
        {
            instance.m_roomByHash.Clear();
            foreach (var roomData in instance.m_rooms)
            {
                if (roomData != null)
                {
                    instance.m_roomByHash[roomData.Hash] = roomData;
                }
            }
        }
    }
}
