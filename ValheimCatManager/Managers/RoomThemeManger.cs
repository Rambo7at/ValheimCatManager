using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace ValheimCatManager.Managers
{
    public class RoomThemeManger
    {
        private static RoomThemeManger _instance;
        public static RoomThemeManger Instance => _instance ?? (_instance = new RoomThemeManger());

        private RoomThemeManger() => new Harmony("RoomThemeManger").PatchAll(typeof(RoomThemeMangerPatch));

        private Dictionary<string, Room.Theme> roomThemeDict = new();
        public List<string> roomThemeList = new();

        private class RoomThemeMangerPatch
        {
            [HarmonyPatch(typeof(Enum), nameof(Enum.GetValues)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
            static void EnumGetEnumValues(Type enumType, ref Array __result) => Instance.EnumGetRoomThemeValues(enumType, ref __result);

            [HarmonyPatch(typeof(Enum), nameof(Enum.GetNames)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
            static void EnumGetEnumNames(Type enumType, ref string[] __result) => Instance.EnumGetRoomThemeNames(enumType, ref __result);
        }

        public void RegisterRoomTheme()
        {
            // 简单方法：从 2^20 开始（足够大，避免冲突）
            int startPower = 20; // 2^20 = 1,048,576
            int nextValue = (int)Math.Pow(2, startPower);

            foreach (string theme in roomThemeList)
            {
                if (!roomThemeDict.ContainsKey(theme))
                {
                    Room.Theme newThemeValue = (Room.Theme)nextValue;
                    roomThemeDict.Add(theme, newThemeValue);

                    Debug.Log($"成功注册新主题: {theme} = {newThemeValue} (0x{nextValue:X})");

                    nextValue <<= 1; // 乘以2，保持2的幂次方
                }
                else
                {
                    Debug.LogWarning($"主题 '{theme}' 已存在，跳过注册");
                }
            }
        }

        private void EnumGetRoomThemeValues(Type enumType, ref Array __result)
        {
            if ((enumType == typeof(Room.Theme)) && Instance.roomThemeDict.Count != 0)
            {
                Room.Theme[] array = new Room.Theme[__result.Length + Instance.roomThemeDict.Count];
                __result.CopyTo(array, 0);
                Instance.roomThemeDict.Values.CopyTo(array, __result.Length);
                __result = array;
            }
        }

        private void EnumGetRoomThemeNames(Type enumType, ref string[] __result)
        {
            if ((enumType == typeof(Room.Theme)) && Instance.roomThemeDict.Count != 0)
            {
                __result = __result.AddRangeToArray(Instance.roomThemeDict.Keys.ToArray());
            }
        }
    }
}
