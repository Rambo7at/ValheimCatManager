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
        /// 注：自定义房间的列表
        /// </summary>
        public readonly List<RoomConfig> roomList = new();

        /// <summary>
        /// 注：自定义主题的列表
        /// </summary>
        private readonly List<string> customThemeList = new();

        /// <summary>
        /// 注：针对房间相同的主题分类
        /// </summary>
        private readonly Dictionary<string, List<RoomData>> themeRoomsDict = new();



        /// <summary>
        /// 注：存储房间配置到RoomData的映射
        /// </summary>
        private readonly Dictionary<RoomConfig, RoomData> roomConfigToData = new();


        private class DungeonPatch
        {
            [HarmonyPatch(typeof(DungeonDB), nameof(DungeonDB.Start)), HarmonyPostfix, HarmonyPriority(1000)]
            static void RegisterDungeonRooms(DungeonDB __instance) => Instance.RegisterDungeonRooms(__instance, Instance.roomList);

            [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.SetupAvailableRooms)), HarmonyPostfix]
            static void ApplyCustomTheme(DungeonGenerator __instance) => Instance.ApplyCustomTheme(__instance);
        }

        // 缓存软引用
        private Dictionary<RoomConfig, SoftReference<GameObject>> roomSoftReferences;


        /// <summary>
        /// 注：注册地下城房间
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="roomConfigs"></param>
        private void RegisterDungeonRooms(DungeonDB instance, List<RoomConfig> roomConfigs)
        {
            Instance.roomSoftReferences ??= new Dictionary<RoomConfig, SoftReference<GameObject>>();

            if (!(roomConfigs.Count > 0)) return;   

            foreach (var roomConfig in roomConfigs)
            {

                DungeonDB.RoomData roomData = roomConfig.GetRoomData();
                if (roomData == null) continue;

                Room room = roomConfig.预制件.GetComponent<Room>();
                if (room == null)
                {
                    Debug.LogError($"执行[RegisterDungeonRooms]时出错，预制件：[{roomConfig.预制件.name}] 没有Room组件！");
                    continue;
                }


                if (!Instance.ValidateRoomTheme(roomConfig.主题))
                {
                    Debug.LogError($"执行[RegisterDungeonRooms]时出错，原因：房间主题是原版 或 内容不在自定义主题列表内");
                    continue;
                }


                // 获取或创建软引用
                if (!Instance.roomSoftReferences.TryGetValue(roomConfig, out var softRef))
                {
                    softRef = CatToolManager.AddLoadedSoftReferenceAsset(roomConfig.预制件);
                    Instance.roomSoftReferences[roomConfig] = softRef;
                }
                // 设置软引用
                roomData.m_prefab = softRef;


                if (!Instance.themeRoomsDict.ContainsKey(roomConfig.主题)) Instance.themeRoomsDict[roomConfig.主题] = new List<RoomData>();
                Instance.themeRoomsDict[roomConfig.主题].Add(roomData);
                Instance.roomConfigToData.Add(roomConfig, roomData);





                // 添加到DungeonDB的m_rooms列表
                instance.m_rooms.Add(roomData);

                // 重新生成哈希列表以确保新房间被正确索引
                instance.m_roomByHash.Clear();
                foreach (var roomDataS in instance.m_rooms)
                {
                    instance.m_roomByHash[roomDataS.Hash] = roomDataS;
                }
            }

        }


        public void RegisterDungeonTheme(GameObject prefab, string customTheme)
        {
            DungeonGenerator dungeonGenerator = prefab.GetComponentInChildren<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogError($"执行[RegisterDungeonTheme]时出错，预制件：[{prefab.name}] 没有[DungeonGenerator]组件！");
                return;
            }
            prefab.AddComponent<CustomTheme>().customTheme = customTheme;
            if (!customThemeList.Contains(customTheme)) Instance.customThemeList.Add(customTheme);
        }






        private void ApplyCustomTheme(DungeonGenerator dungeonGenerator)
        {
            CustomTheme customTheme = dungeonGenerator.gameObject.GetComponent<CustomTheme>();
            if (customTheme == null) return;

            if (DungeonGenerator.m_availableRooms != null)
            {
                if (Instance.themeRoomsDict.TryGetValue(customTheme.customTheme, out var rooms))
                {
                    foreach (var roomData in rooms)
                    {
                        // 现在可以正确使用 m_enabled 字段
                        if (roomData.m_enabled == true)
                        {
                            DungeonGenerator.m_availableRooms.Add(roomData);

                            // 获取房间名称的几种方式：
                            string roomName = roomData.m_prefab.Name; // 软引用名称
                                                                      // 或者：string roomName = roomData.RoomInPrefab?.name ?? "Unknown";

                            Debug.Log($"[DungeonManager] 为生成器 '{dungeonGenerator.name}' 添加房间 '{roomName}'");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[DungeonManager] 未找到主题 '{customTheme.customTheme}' 对应的房间");
                }
            }
        }


        // 在注册房间时验证主题是否存在
        private bool ValidateRoomTheme(string themeName)
        {
            // 检查是否是原版主题
            if (Enum.TryParse<Room.Theme>(themeName, out _))
                return false;

            // 检查是否是已注册的自定义主题
            return customThemeList.Contains(themeName);
        }
    }
}
