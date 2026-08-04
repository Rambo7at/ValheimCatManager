using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Data;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using static ZRpc;

namespace ValheimCatManager.Managers;

public class RpcManager
{
    private static RpcManager _instance;
    public static RpcManager Instance => _instance ?? (_instance = new RpcManager());
    private RpcManager() { }

    List<string> VersionList = [];

    Dictionary<string, ZPackage> DataSyncDict = [];

    bool Check = true;

    public bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

    private const string RpcName = "com.rambo7at.CatManager_RPC_CheckClientModList";
    private const string Rpcdatysync = "com.rambo7at.CatManager_RPC_CheckClientModList";

    /// <summary>注：存放已经完成模组版本校验客户端ZRpc </summary>
    private readonly List<ZRpc> _validatedPeers = new List<ZRpc>();

    #region 版本同步 封装
    /// <summary>注：登记模组，用于联机版本校验</summary>
    public void RegisterModVersion()
    {
        Assembly mod = Assembly.GetCallingAssembly();
        string modFullName = Assembly.GetCallingAssembly().FullName;

        if (!VersionList.Contains(modFullName)) VersionList.Add(modFullName);

        if (Check)
        {
            InitVersionCheckRpc();
            Check = false;
        }
    }

    /// <summary>注：构建版本同步容器，注册整套版本校验RPC</summary>
    private void InitVersionCheckRpc()
    {
        PatchManager.OnNewConnectionRegister += (peer) =>
        {
            peer.m_rpc.Register(RpcName, new System.Action<ZRpc, List<string>>(RPC_CheckClientModList));
        };

        PatchManager.OnNewConnectionInvoke += (peer) =>
        {
            if (!ZNet.instance.IsServer())
            {
                peer.m_rpc.Invoke(RpcName, VersionList);
            }
        };

        PatchManager.OnPeerInfoInvoke += (rpc, zNet) =>
        {
            if (!zNet.IsServer())
            {
                return true;
            }
            if (!_validatedPeers.Contains(rpc))
            {
                rpc.Invoke("Error", 3);
                return false;
            }
            return true;
        };

        PatchManager.OnPeerDisconnect += (peer) =>
        {
            if (ZNet.instance.IsServer())
            {
                _validatedPeers.Remove(peer.m_rpc);
            }
        };
    }

    /// <summary>注：服务端接收客户端上报的模组版本列表</summary>
    private void RPC_CheckClientModList(ZRpc zRpc, List<string> modList)
    {
        if (!ZNet.instance.IsServer()) return;

        if (modList?.Count != VersionList.Count)
        {
            zRpc.Invoke("Error", 3);
            return;
        }
        foreach (var item in VersionList)
        {
            if (!modList.Contains(item))
            {
                zRpc.Invoke("Error", 3);
                return;
            }
        }
        if (!_validatedPeers.Contains(zRpc))
        {
            _validatedPeers.Add(zRpc);
        }
    }
    #endregion

    #region 数据同步 封装

    public void RegisterDataSync(ZPackage zPackage)
    {
        Assembly mod = Assembly.GetCallingAssembly();
        string modFullName = Assembly.GetCallingAssembly().FullName;

        if (DataSyncDict.TryGetValue(modFullName, out var _))
        {
            Debug.LogWarning("检测到......");
            return;
        }



    }

    public void InitDataSync()
    {
        PatchManager.OnNewConnectionRegister += (peer) =>
        {
            peer.m_rpc.Register(RpcName, new System.Action<ZRpc, List<string>>(RPC_CheckClientModList));
        };


    }




    #endregion

    #region 序列化工具
    /// <summary>注：将业务对象序列化并封装为全新ZPackage</summary>
    /// <typeparam name="T">业务数据实体类型</typeparam>
    /// <param name="data">待序列化的业务对象</param>
    /// <returns>填充二进制数据的ZPackage</returns>
    public ZPackage PackToZPackage<T>(T data)
    {
        byte[] bytes = SerializeToBytes(data);
        ZPackage pkg = new ZPackage();
        pkg.Write(bytes);
        return pkg;
    }

    /// <summary>注：读取ZPackage内二进制数据，反序列化为业务对象</summary>
    /// <typeparam name="T">目标业务实体类型</typeparam>
    /// <param name="pkg">网络接收的ZPackage数据包</param>
    /// <returns>解析还原后的业务对象</returns>
    public T UnpackFromZPackage<T>(ZPackage pkg)
    {
        byte[] raw = pkg.ReadByteArray();
        return DeserializeFromBytes<T>(raw);
    }

    /// <summary>注：字节数组反序列化为指定类型对象</summary>
    /// <typeparam name="T">目标实体类型</typeparam>
    /// <param name="serializedData">序列化二进制数组</param>
    /// <returns>反序列化得到的对象</returns>
    private T DeserializeFromBytes<T>(byte[] serializedData)
    {
        using MemoryStream stream = new MemoryStream(serializedData);
        BinaryFormatter formatter = new BinaryFormatter();
        return (T)formatter.Deserialize(stream);
    }

    /// <summary>注：对象序列化为二进制字节数组</summary>
    /// <typeparam name="T">待序列化对象类型</typeparam>
    /// <param name="objectToSerialize">需要序列化的对象</param>
    /// <returns>序列化后的字节数组</returns>
    private byte[] SerializeToBytes<T>(T objectToSerialize)
    {
        using MemoryStream stream = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, objectToSerialize);
        return stream.ToArray();
    }

    #endregion


    /// <summary>注：注册 数据同步RPC 接收回调</summary>
    public void RpcRegister(string rpcName, System.Action<long> action) => PatchManager.OnGameStartRegister += () => ZRoutedRpc.instance.Register(rpcName, action);
    public void RpcRegister<T>(string rpcName, System.Action<long, T> action) => PatchManager.OnGameStartRegister += () => ZRoutedRpc.instance.Register(rpcName, action);
    /// <summary>客户端调用，自动发给服务端（使用ZRoutedRpc默认重载）</summary>
    public void ClientCallRpc(string rpcName) => ZRoutedRpc.instance.InvokeRoutedRPC(rpcName);

    /// <summary>客户端调用，自动发给服务端（带参数）</summary>
    public void ClientCallRpc<T>(string rpcName, T data)=> ZRoutedRpc.instance.InvokeRoutedRPC(rpcName, data);

    /// <summary>定向指定PeerId发送RPC</summary>
    public void TargetCallRpc(string rpcName, long targetPeerId)=> ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, rpcName);

    /// <summary>定向指定PeerId发送RPC（带参数）</summary>
    public void TargetCallRpc<T>(string rpcName, long targetPeerId, T data) => ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, rpcName, data);

    /// <summary>PeerInfo握手完成后执行：客户端向服务端调用RPC</summary>
    public void ClientCallRpc_PeerInfo(string rpcName) => PatchManager.OnZNetPeerInfo += () => ClientCallRpc(rpcName);

    /// <summary>PeerInfo握手完成后执行：客户端向服务端调用RPC（带参数）</summary>
    public void ClientCallRpc_PeerInfo<T>(string rpcName, T data)=> PatchManager.OnZNetPeerInfo += () => ClientCallRpc(rpcName, data);

    /// <summary>PeerInfo握手完成后执行：定向给指定Peer发送RPC</summary>
    public void TargetCallRpc_PeerInfo(string rpcName, long targetPeerId) => PatchManager.OnZNetPeerInfo += () => TargetCallRpc(rpcName, targetPeerId);

    /// <summary>PeerInfo握手完成后执行：定向给指定Peer发送RPC（带参数）</summary>
    public void TargetCallRpc_PeerInfo<T>(string rpcName, long targetPeerId, T data) => PatchManager.OnZNetPeerInfo += () => TargetCallRpc(rpcName, targetPeerId, data);

    /// <summary>广播给全部Peer（Everybody）</summary>
    public void BroadcastCallRpc(string rpcName) => ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, rpcName);

    /// <summary>广播给全部Peer（Everybody，带参数）</summary>
    public void BroadcastCallRpc<T>(string rpcName, T data) => ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, rpcName, data);



}

