using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValheimCatManager;
using ValheimCatManager.Managers;

namespace ValheimCatManager.Managers;

public static class HarmonyPatchManager
{
    // ---- 内部注册事件 ----
    internal static event Action<ObjectDB> OnObjectDBAwakeRegister;
    internal static event Action<ZNetScene> OnZNetSceneAwakeRegister;
    internal static event Action<ObjectDB, ObjectDB> OnObjectDBCopyRegister;

    // ---- 内部修改事件（在 Register 之后、Patch 之前触发） ----
    internal static event Action<ObjectDB> OnObjectDBAwakeModify;

    // ---- 对外开放的前置补丁事件 ----
    public static event Action<ObjectDB> OnObjectDBAwakePatch;
    public static event Action<ZNetScene> OnZNetSceneAwakePatch;
    public static event Action<ObjectDB, ObjectDB> OnObjectDBCopyPatch;

    // ---- 对外开放的后置补丁事件 ----

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(0)]
    static void ObjectDB_Awake_Postfix(ObjectDB __instance)
    {
        OnObjectDBAwakeRegister?.Invoke(__instance);
        OnObjectDBAwakeModify?.Invoke(__instance);
        OnObjectDBAwakePatch?.Invoke(__instance);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyPriority(0)]
    static void ZNetScene_Awake_Postfix(ZNetScene __instance)
    {
        OnZNetSceneAwakeRegister?.Invoke(__instance);
        OnZNetSceneAwakePatch?.Invoke(__instance);
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), HarmonyPrefix, HarmonyPriority(-101)]
    static void ObjectDB_CopyOtherDB_Prefix(ObjectDB __instance, ObjectDB other)
    {
        OnObjectDBCopyRegister?.Invoke(__instance, other);
        OnObjectDBCopyPatch?.Invoke(__instance, other);
    }

}
