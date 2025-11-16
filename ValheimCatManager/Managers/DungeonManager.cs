using HarmonyLib;
using SoftReferenceableAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Config;
using ValheimCatManager.Tool;
using static DungeonDB;

namespace ValheimCatManager.Managers
{
    public class DungeonManager
    {

        private static DungeonManager _instance;


        public static DungeonManager Instance => _instance ?? (_instance = new DungeonManager());


        private DungeonManager() => new Harmony("DungeonManager").PatchAll(typeof(DungeonPatch));


        public readonly List<RoomConfig> roomList = new ();





        private class DungeonPatch
        {
            [HarmonyPatch(typeof(DungeonDB), nameof(DungeonDB.Start)), HarmonyPostfix, HarmonyPriority(1000)]
            static void RegisterDungeonRooms(DungeonDB __instance) => Instance.RegisterDungeonRooms(__instance, Instance.roomList);



        }

        // 缓存软引用
        private Dictionary<RoomConfig, SoftReference<GameObject>> roomSoftReferences;
        private void RegisterDungeonRooms(DungeonDB instance, List<RoomConfig> roomConfigs)
        {

            Instance.roomSoftReferences ??= new Dictionary<RoomConfig, SoftReference<GameObject>>();

            foreach (var roomConfig in roomConfigs)
            {

                if (roomConfig == null)
                {
                    Debug.LogError($"执行RegisterDungeonRooms时 [{roomConfig.预制件.name}] 有问题，执行跳过");
                    continue;
                }

                roomConfig.预制件.GetComponent<Room>().m_theme = CatToolManager.GetTheme(roomConfig.主题);





                DungeonDB.RoomData roomData = roomConfig.GetRoomData();




                // 获取或创建软引用
                if (!Instance.roomSoftReferences.TryGetValue(roomConfig, out var softRef))
                {
                    softRef = CatToolManager.AddLoadedSoftReferenceAsset(roomConfig.预制件);
                    Instance.roomSoftReferences[roomConfig] = softRef;
                }

                // 设置软引用
                roomData.m_prefab = softRef;
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

    }
}
