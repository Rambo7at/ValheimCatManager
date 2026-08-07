using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager;
using ValheimCatManager.Managers;

namespace ValheimCatManager.Managers;

using HarmonyLib;
using UnityEngine.SceneManagement;
using System;

/// <summary>注：集中式 Harmony 补丁事件中心，统一管理游戏生命周期各阶段的补丁挂载与事件分发</summary>
public static class PatchManager
{
    /// <summary>注：内部事件，Game.Start 阶段触发，用于资源注册</summary>
    internal static event Action OnGameStartRegister;

    /// <summary>注：内部事件，ObjectDB.Awake 阶段触发，用于资源注册</summary>
    internal static event Action<ObjectDB> OnObjectDBAwakeRegister;

    /// <summary>注：内部事件，ObjectDB.Awake 阶段触发，用于资源修改</summary>
    internal static event Action<ObjectDB> OnObjectDBAwakeModify;

    /// <summary>注：内部事件，ZNetScene.Awake 阶段触发，用于资源注册</summary>
    internal static event Action<ZNetScene> OnZNetSceneAwakeRegister;

    /// <summary>注：内部事件，ObjectDB.CopyOtherDB 阶段触发，用于资源注册</summary>
    internal static event Action<ObjectDB, ObjectDB> OnObjectDBCopyRegister;

    /// <summary>注：内部事件，ZNet.OnNewConnection 阶段触发，用于资源注册</summary>
    internal static event Action<ZNetPeer> OnNewConnectionRegister;

    /// <summary>注：内部事件，ZNet.OnNewConnection 阶段触发，用于执行连接后的初始化操作</summary>
    internal static event Action<ZNetPeer> OnNewConnectionInvoke;

    /// <summary>注：内部事件，ZNet.RPC_PeerInfo 阶段触发，用于联机RPC通讯的PeerInfo拦截与处理</summary>
    internal static event Func<ZRpc, ZNet, bool> OnPeerInfoInvoke;

    /// <summary>注：内部事件，ZNet.Disconnect 阶段触发，用于联机断开连接时的清理与通讯处理</summary>
    internal static event Action<ZNetPeer> OnPeerDisconnect;

    // ---- 对外开放的前置补丁事件 ----
    /// <summary>注：ObjectDB.Awake 阶段触发</summary>
    public static event Action<ObjectDB> OnObjectDBAwake;

    /// <summary>注：ObjectDB.Awake 阶段遍历配方时触发，用于统一修改配方</summary>
    public static event Action<Recipe> OnObjectDBRecipe;

    /// <summary>注：ZNetScene.Awake 阶段触发</summary>
    public static event Action<ZNetScene> OnZNetSceneAwake;

    /// <summary>注：ObjectDB.CopyOtherDB 阶段触发</summary>
    public static event Action<ObjectDB, ObjectDB> OnObjectDBCopy;

    /// <summary>注：点击主菜单开始游戏后第一时间触发</summary>
    public static event Action OnGameStart;

    /// <summary>注：该时机仅联机状态会触发，ZNet.RPC_PeerInfo 后置阶段</summary>
    public static event Action OnZNetPeerInfo;

    /// <summary>注：ZNet.RPC_PeerInfo 阶段遍历配方时触发，用于统一修改配方</summary>
    public static event Action<Recipe> OnZNetPeerInfoRecipe;

    /// <summary>注：ObjectDB.Awake 补丁，在对象数据库初始化时触发</summary>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(0)]
    static void ObjectDB_Awake_Postfix(ObjectDB __instance)
    {
        if (SceneManager.GetActiveScene().name != "main" || __instance == null) return;

        OnObjectDBAwakeRegister?.Invoke(__instance);
        OnObjectDBAwakeModify?.Invoke(__instance);
        OnObjectDBAwake?.Invoke(__instance);

        if (OnObjectDBRecipe != null && __instance?.m_recipes.Count > 0)
        {
            foreach (var item in __instance.m_recipes)
            {
                OnObjectDBRecipe?.Invoke(item);
            }
        }
    }

    /// <summary>注：ZNetScene.Awake 补丁，在网络场景初始化时触发</summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyPriority(0)]
    static void ZNetScene_Awake_Postfix(ZNetScene __instance)
    {
        OnZNetSceneAwakeRegister?.Invoke(__instance);
        OnZNetSceneAwake?.Invoke(__instance);
    }

    /// <summary>注：ObjectDB.CopyOtherDB 补丁，在数据库复制时触发（Prefix）</summary>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), HarmonyPrefix, HarmonyPriority(-101)]
    static void ObjectDB_CopyOtherDB_Prefix(ObjectDB __instance, ObjectDB other)
    {
        OnObjectDBCopyRegister?.Invoke(__instance, other);
        OnObjectDBCopy?.Invoke(__instance, other);
    }

    /// <summary>注：ZNet.OnNewConnection 补丁，在新连接建立时触发</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection)), HarmonyPrefix, HarmonyPriority(0)]
    static void ZNet_OnNewConnection_Prefix(ZNetPeer peer)
    {
        OnNewConnectionRegister?.Invoke(peer);
        OnNewConnectionInvoke?.Invoke(peer);
    }

    /// <summary>注：ZNet.RPC_PeerInfo 补丁（Prefix），用于联机RPC通讯拦截，可阻断原方法执行</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPrefix, HarmonyPriority(0)]
    static bool ZNet_RPC_PeerInfo_Prefix(ZRpc rpc, ZPackage pkg, ref ZNet __instance)
    {
        if (OnPeerInfoInvoke?.Invoke(rpc, __instance) is not bool b)
        {
            return true;
        }
        return b;
    }

    /// <summary>注：ZNet.RPC_PeerInfo 补丁（Postfix），用于联机RPC通讯后置处理</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPostfix, HarmonyPriority(0)]
    static void ZNet_RPC_PeerInfo_Postfix()
    {
        OnZNetPeerInfo?.Invoke();

        if (OnZNetPeerInfoRecipe != null && ObjectDB.instance?.m_recipes.Count > 0)
        {
            foreach (var res in ObjectDB.instance.m_recipes)
            {
                OnZNetPeerInfoRecipe?.Invoke(res);
            }
        }
       
    }

    /// <summary>注：ZNet.Disconnect 补丁，在断开连接时触发，用于联机通讯清理</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect)), HarmonyPrefix, HarmonyPriority(0)]
    static void ZNet_Disconnect_Prefix(ZNetPeer peer)
    {
        OnPeerDisconnect?.Invoke(peer);
    }

    /// <summary>注：Game.Start 补丁，在游戏启动时触发</summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start)), HarmonyPrefix, HarmonyPriority(0)]
    static void Game_Start_Prefix()
    {
        OnGameStartRegister?.Invoke();
        OnGameStart?.Invoke();
    }
}
