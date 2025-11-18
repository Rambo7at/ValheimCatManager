using HarmonyLib;
using MonoMod.RuntimeDetour;
using SoftReferenceableAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.ValheimCatManager.Config;
using ValheimCatManager.ValheimCatManager.Data;
using ValheimCatManager.ValheimCatManager.Managers;
using ValheimCatManager.ValheimCatManager.Tool;
using static DungeonDB;

namespace ValheimCatManager.ValheimCatManager.Managers
{
    public class DungeonManager
    {
        private static DungeonManager _instance;
        public static DungeonManager Instance => _instance ?? (_instance = new DungeonManager());

        private DungeonManager() => new Harmony("DungeonManager").PatchAll(typeof(DungeonPatch));

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

        private readonly Dictionary<int, string> hashToName = new();

        private readonly Dictionary<string, RoomData> stringToRoomData = new();

        /// <summary>
        /// 注：地牢房间的软引用
        /// </summary>
        private Dictionary<RoomConfig, SoftReference<GameObject>> roomSoftReferences;

        // Harmony补丁
        private class DungeonPatch
        {
            [HarmonyPatch(typeof(DungeonDB), nameof(DungeonDB.Start)), HarmonyPostfix, HarmonyPriority(1000)]
            static void RegisterDungeonRooms(DungeonDB __instance) => Instance.RegisterDungeonRooms(__instance, Instance.roomList);

            [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.SetupAvailableRooms)), HarmonyPostfix]
            static void ApplyCustomTheme(DungeonGenerator __instance) => Instance.ApplyCustomTheme(__instance);

            [HarmonyPatch(typeof(DungeonDB), nameof(DungeonDB.GetRoom)), HarmonyPrefix]
            private static bool OnDungeonDBGetRoom(int hash, ref DungeonDB.RoomData __result)
            {
                DungeonDB.RoomData result = Instance.OnDungeonDBGetRoom(hash);

                if (result != null)
                {
                    __result = result;
                    return false;
                }

                return true;
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
                string roomPrefabName = roomConfig.预制件.name;
                int roomPrefabHash = roomPrefabName.GetStableHashCode();


                // 验证主题
                if (!CheckRoomTheme(roomConfig.主题))
                {
                    Debug.LogError($"执行[RegisterDungeonRooms]方法出错原因：预制件：[{roomConfig.预制件.name}]对应主题：[{roomConfig.主题}]");
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


                if (!themeRoomsDict.TryGetValue(roomConfig.主题, out var roomList))
                {
                    roomList = new List<RoomData>();
                    themeRoomsDict[roomConfig.主题] = roomList;
                }
                roomList.Add(roomData);


                // 添加到DungeonDB
                instance.m_rooms.Add(roomData);



                if (!hashToName.ContainsKey(roomPrefabHash))
                {
                    Instance.hashToName.Add(roomPrefabHash, roomPrefabName);
                }
                else
                {
                    Debug.LogError($"执行[RegisterDungeonRooms]发现重复哈希值！房间名：[{roomPrefabName}]");
                }

                if (!stringToRoomData.ContainsKey(roomPrefabName))
                {
                    stringToRoomData.Add(roomPrefabName, roomData);
                }
                else
                {
                    Debug.LogError($"执行[RegisterDungeonRooms]发现[stringToRoomData]重复！房间名：[{roomPrefabName}]");

                }

            }
            instance.GenerateHashList();


        }

        /// <summary>
        /// 注册自定义主题
        /// </summary>
        public void RegisterDungeonTheme(GameObject prefab, string customTheme)
        {
            DungeonGenerator dungeonGenerator = prefab.GetComponentInChildren<DungeonGenerator>();
            if (dungeonGenerator == null) return;

            // 添加自定义主题组件
            dungeonGenerator.gameObject.AddComponent<CustomTheme>().customTheme = customTheme;

            // 添加到主题列表
            if (!customThemeList.Contains(customTheme)) customThemeList.Add(customTheme);
        }

        /// <summary>
        /// 应用自定义主题到地下城生成器
        /// </summary>
        private void ApplyCustomTheme(DungeonGenerator dungeonGenerator)
        {
            // 关键修改：在父级对象中查找CustomTheme组件
            CustomTheme customTheme = dungeonGenerator.gameObject.GetComponentInParent<CustomTheme>();

            if (customTheme == null)
            {
                Debug.Log($"[DungeonManager] 生成器及其父级没有CustomTheme组件: {dungeonGenerator.name}");

                // 调试：打印生成器的层级结构
                Transform current = dungeonGenerator.transform;
                string path = current.name;
                while (current.parent != null)
                {
                    current = current.parent;
                    path = current.name + "/" + path;
                }
                Debug.Log($"[DungeonManager] 生成器完整路径: {path}");

                return;
            }

            if (DungeonGenerator.m_availableRooms == null)
            {
                Debug.LogWarning($"[DungeonManager] m_availableRooms为null");
                return;
            }

            Debug.Log($"[DungeonManager] 为生成器应用主题: {dungeonGenerator.name} -> {customTheme.customTheme}");

            // 获取该主题对应的房间
            if (themeRoomsDict.TryGetValue(customTheme.customTheme, out var rooms))
            {
                int addedCount = 0;
                foreach (var roomData in rooms)
                {
                    // 检查房间是否启用
                    if (roomData.m_enabled)
                    {
                        DungeonGenerator.m_availableRooms.Add(roomData);
                        addedCount++;
                        Debug.Log($"[DungeonManager] 添加房间: {roomData.m_prefab.Name}");
                    }
                }
                Debug.Log($"[DungeonManager] 为生成器添加了 {addedCount} 个房间");
            }
            else
            {
                Debug.LogWarning($"[DungeonManager] 未找到主题对应的房间: {customTheme.customTheme}");
                Debug.Log($"[DungeonManager] 可用主题: {string.Join(", ", themeRoomsDict.Keys)}");
            }
        }

        /// <summary>
        /// 工具方法：检测主题是否是已有原版或自定主题对应
        /// </summary>
        private bool CheckRoomTheme(string themeName)
        {
            if (Enum.TryParse<Room.Theme>(themeName, out _)) return true;
            return customThemeList.Contains(themeName);
        }


        private DungeonDB.RoomData OnDungeonDBGetRoom(int hash)
        {
            if (hashToName.TryGetValue(hash, out var roomName) && stringToRoomData.TryGetValue(roomName, out var room))
            {
                return room;
            }

            return null;
        }

    }
}
