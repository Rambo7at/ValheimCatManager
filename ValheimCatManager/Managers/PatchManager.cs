using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.Managers;

namespace ValheimCatManager.Managers;

public static class PatchManager
{
    // ---- 内部注册事件 ----
    internal static event Action OnGameStartRegister;
    internal static event Action<ObjectDB> OnObjectDBAwakeRegister;
    internal static event Action<ObjectDB> OnObjectDBAwakeModify;
    internal static event Action<ZNetScene> OnZNetSceneAwakeRegister;
    internal static event Action<ObjectDB, ObjectDB> OnObjectDBCopyRegister;

    // NewConnection 阶段
    internal static event Action<ZNetPeer> OnNewConnectionRegister;
    internal static event Action<ZNetPeer> OnNewConnectionInvoke;

    // PeerInfo 阶段
    internal static event Func<ZRpc, ZNet, bool> OnPeerInfoInvoke;

    // PeerDisconnect 阶段
    internal static event Action<ZNetPeer> OnPeerDisconnect;


    // ---- 对外开放的前置补丁事件 ----
    public static event Action<ObjectDB> OnObjectDBAwake;
    public static event Action<ZNetScene> OnZNetSceneAwake;
    public static event Action<ObjectDB, ObjectDB> OnObjectDBCopy;
    public static event Action OnGameStart;
    public static event Action OnZNetPeerInfo;
    public static event Action OnSpawnPlayer;
    public static event Action OnFixedUpdate;

    // ---- 对外开放的后置补丁事件 ----

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(0)]
    static void ObjectDB_Awake_Postfix(ObjectDB __instance)
    {
        OnObjectDBAwakeRegister?.Invoke(__instance);
        OnObjectDBAwakeModify?.Invoke(__instance);
        OnObjectDBAwake?.Invoke(__instance);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyPriority(0)]
    static void ZNetScene_Awake_Postfix(ZNetScene __instance)
    {
        OnZNetSceneAwakeRegister?.Invoke(__instance);
        OnZNetSceneAwake?.Invoke(__instance);
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), HarmonyPrefix, HarmonyPriority(-101)]
    static void ObjectDB_CopyOtherDB_Prefix(ObjectDB __instance, ObjectDB other)
    {
        OnObjectDBCopyRegister?.Invoke(__instance, other);
        OnObjectDBCopy?.Invoke(__instance, other);
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection)), HarmonyPrefix, HarmonyPriority(0)]
    static void ZNet_OnNewConnection_Prefix(ZNetPeer peer)
    {
        OnNewConnectionRegister?.Invoke(peer);
        OnNewConnectionInvoke?.Invoke(peer);
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPrefix, HarmonyPriority(0)]
    static bool ZNet_RPC_PeerInfo_Prefix(ZRpc rpc, ZPackage pkg, ref ZNet __instance)
    {
        if (OnPeerInfoInvoke?.Invoke(rpc, __instance) is not bool b)
        {
            return true;
        }

        return b;
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPostfix, HarmonyPriority(0)]
    static void ZNet_RPC_PeerInfo_Postfix()
    {
        OnZNetPeerInfo?.Invoke();
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect)), HarmonyPrefix, HarmonyPriority(0)]
    static void ZNet_Disconnect_Prefix(ZNetPeer peer)
    {
        OnPeerDisconnect?.Invoke(peer);
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Start)), HarmonyPrefix, HarmonyPriority(0)]
    static void Game_Start_Prefix()
    {
        OnGameStartRegister?.Invoke();
        OnGameStart?.Invoke();
    }

    [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer)), HarmonyPrefix, HarmonyPriority(0)]
    static void Game_SpawnPlayer_Prefix()
    {
        OnSpawnPlayer?.Invoke();
    }
}
