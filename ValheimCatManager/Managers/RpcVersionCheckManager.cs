using HarmonyLib;
using System;
using System.Collections.Generic;

public class RpcVersionCheckManager
{
    private static RpcVersionCheckManager _instance;
    public static RpcVersionCheckManager Instance => _instance ?? (_instance = new RpcVersionCheckManager());

    private RpcVersionCheckManager() => new Harmony("RpcNetworkManagerPatch").PatchAll(typeof(RpcVersionCheckPatch));

    private string version;
    private string rpcName;

    // 已验证的客户端列表
    private List<ZRpc> validatedPeers = new List<ZRpc>();

    public void EnableVersionCheck(string versionInfo, string rpcName)
    {
        version = versionInfo;
        this.rpcName = rpcName;
    }

    // RPC 网络补丁类 - 包含所有 Harmony 补丁
    private static class RpcVersionCheckPatch
    {
        /// <summary>
        /// 当有新连接时的处理补丁
        /// </summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection)), HarmonyPrefix, HarmonyPriority(0)]
        static void NewConnectionPatch(ZNetPeer peer, ref ZNet __instance)
        {
            Instance.RegisterRpcCheckVersion(peer, ref __instance);
        }

        /// <summary>
        /// 验证客户端是否已完成版本检查
        /// </summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPrefix, HarmonyPriority(0)]
        private static bool VerifyClient(ZRpc rpc, ZPackage pkg, ref ZNet __instance)
        {
            // 如果是服务器且客户端未经验证
            if (__instance.IsServer() && !Instance.validatedPeers.Contains(rpc))
            {
                ZLog.LogWarning("客户端未通过版本验证，断开连接");
                rpc.Invoke("Error", (object)3);
                return false; // 阻止执行原始方法
            }

            return true; // 继续执行原始方法
        }

        /// <summary>
        /// 处理断开连接的补丁
        /// </summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect)), HarmonyPrefix, HarmonyPriority(0)]
        private static void RemoveDisconnectedPeerFromVerified(ZNetPeer peer, ref ZNet __instance)
        {
            // 检查是否是服务器
            if (__instance.IsServer())
            {
                // 从已验证列表中移除断开连接的客户端
                ZLog.Log("对等端断开连接，从已验证列表中移除");
                Instance.validatedPeers.Remove(peer.m_rpc);
            }
        }
    }

    private void RegisterRpcCheckVersion(ZNetPeer peer, ref ZNet __instance)
    {
        ZLog.Log("启动Rpc注册");

        // 注册 RPC 处理方法
        peer.m_rpc.Register<ZPackage>(rpcName, new Action<ZRpc, ZPackage>(CheckVersion));

        // 只有服务器需要主动发起版本检查
        if (__instance.IsServer())
        {
            ZLog.Log("服务器发起版本检查");

            ZPackage zpackage = new ZPackage();
            zpackage.Write(version);

            peer.m_rpc.Invoke(rpcName, (object)zpackage);
        }
    }

    private void CheckVersion(ZRpc rpc, ZPackage pkg)
    {
        try
        {
            // 从数据包中读取接收到的版本号
            var receivedVersion = pkg.ReadString();

            // 获取本地版本号
            var localVersion = version;

            ZLog.Log($"版本检查, 接收到的: {receivedVersion}, 本地的: {localVersion}");

            // 检查版本兼容性
            if (receivedVersion != localVersion)
            {
                if (ZNet.instance.IsServer())
                {
                    ZLog.LogWarning($"客户端版本不符，断开连接。客户端版本: {receivedVersion}, 服务器版本: {localVersion}");
                    rpc.Invoke("Error", (object)3);
                }
                else
                {
                    // 客户端发现服务器版本不兼容
                    ZLog.LogError($"服务器版本不符，请更新模组。服务器版本: {receivedVersion}, 本地版本: {localVersion}");
                    // 客户端可以主动断开连接或显示错误消息
                }
            }
            else
            {
                // 版本兼容
                if (ZNet.instance.IsServer())
                {
                    ZLog.Log("版本兼容，将对等端添加到已验证列表");
                    if (!validatedPeers.Contains(rpc))
                    {
                        validatedPeers.Add(rpc);
                    }
                }
                else
                {
                    ZLog.Log("服务器版本验证通过");
                }
            }
        }
        catch (Exception ex)
        {
            ZLog.LogError($"版本检查过程中发生错误: {ex.Message}");

            // 如果是服务器，断开连接
            if (ZNet.instance.IsServer())
            {
                rpc.Invoke("Error", (object)3);
            }
        }
    }
}