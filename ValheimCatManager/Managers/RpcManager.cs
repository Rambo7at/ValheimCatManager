using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Data;
using ValheimCatManager.Interface;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using static ZRpc;

namespace ValheimCatManager.Managers;

/// <summary>注：RPC通信核心管理器，提供版本校验、数据同步、序列化及RPC注册调用等通用功能</summary>
public class RpcManager
{
    private static RpcManager _instance;
    public static RpcManager Instance => _instance ?? (_instance = new RpcManager());
    private RpcManager() { }

    /// <summary>注：已注册的模组完整名称列表（用于版本校验）</summary>
    private List<string> _registeredModList = [];

    /// <summary>注：数据同步提供者字典（Key=模组全名，Value=数据同步接口实例）</summary>
    private Dictionary<string, IZPackageSync> _syncProviders = [];

    /// <summary>注：是否已初始化版本校验RPC（true=尚未初始化，false=已初始化）</summary>
    private bool _isVersionCheckInitialized = true;
    /// <summary>注：是否已初始化数据同步RPC</summary>
    private bool _isDataSyncInitialized = false;

    /// <summary>注：当前是否为服务端</summary>
    public bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

    /// <summary>注：版本校验RPC名称</summary>
    private const string RPC_VersionCheck = "com.rambo7at.CatManager_RPC_CheckClientModList";
    /// <summary>注：数据同步请求RPC名称（客户端→服务端）</summary>
    private const string RPC_DataSync_Request = "com.rambo7at.CatManager.Rpc_ClientRequestSyncData";
    /// <summary>注：数据同步响应RPC名称（服务端→客户端）</summary>
    private const string RPC_DataSync_Response = "com.rambo7at.CatManager.Rpc_ServerSendSyncData";

    /// <summary>注：存放已完成模组版本校验的客户端ZRpc列表</summary>
    private readonly List<ZRpc> _validatedPeers = new List<ZRpc>();

    #region 版本同步 封装

    /// <summary>注：登记当前模组到版本校验列表（由各下游Mod在加载时调用）</summary>
    public void RegisterModVersion()
    {
        Assembly mod = Assembly.GetCallingAssembly();
        string modFullName = Assembly.GetCallingAssembly().FullName;

        if (!_registeredModList.Contains(modFullName)) _registeredModList.Add(modFullName);

        if (_isVersionCheckInitialized)
        {
            InitVersionCheckRpc();
            _isVersionCheckInitialized = false;
        }
    }

    /// <summary>注：构建版本同步容器，注册整套版本校验RPC（仅执行一次）</summary>
    private void InitVersionCheckRpc()
    {
        PatchManager.OnNewConnectionRegister += (peer) =>
        {
            peer.m_rpc.Register(RPC_VersionCheck, new System.Action<ZRpc, List<string>>(RPC_CheckClientModList));
        };

        PatchManager.OnNewConnectionInvoke += (peer) =>
        {
            if (!ZNet.instance.IsServer())
            {
                peer.m_rpc.Invoke(RPC_VersionCheck, _registeredModList);
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

    /// <summary>注：服务端接收客户端上报的模组版本列表，校验通过后加入已验证列表</summary>
    private void RPC_CheckClientModList(ZRpc zRpc, List<string> modList)
    {
        if (!ZNet.instance.IsServer()) return;

        if (modList?.Count != _registeredModList.Count)
        {
            zRpc.Invoke("Error", 3);
            return;
        }
        foreach (var item in _registeredModList)
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

    /// <summary>注：注册数据同步接口（下游Mod在加载时调用，注册后自动触发首次同步）</summary>
    public void RegisterDataSync(IZPackageSync Intf)
    {
        Assembly mod = Assembly.GetCallingAssembly();
        string modFullName = Assembly.GetCallingAssembly().FullName;

        if (_syncProviders.TryGetValue(modFullName, out var _))
        {
            Debug.LogWarning($"[RpcManager.RegisterDataSync] 模组 {modFullName} 已注册，跳过重复注册");
            return;
        }

        _syncProviders[modFullName] = Intf;

        InitDataSync();
    }

    /// <summary>注：服务端主动推送最新数据给所有客户端（配置热更新时调用）</summary>
    public void UpdateServerDataSync(IZPackageSync Intf)
    {
        if (IsServer == false) return;

        Assembly mod = Assembly.GetCallingAssembly();
        string modFullName = Assembly.GetCallingAssembly().FullName;

        if (!_syncProviders.TryGetValue(modFullName, out var _))
        {
            Debug.LogWarning($"[RpcManager.UpdateServerDataSync] 模组 {modFullName} 未注册，无法推送");
            return;
        }
        if (Intf.GetSyncZPackage() is not ZPackage zPackage) return;

        _syncProviders[modFullName] = Intf;

        BroadcastCallRpc<string, ZPackage>(RPC_DataSync_Response, modFullName, zPackage);
    }

    /// <summary>注：初始化数据同步路由（注册RPC并触发客户端首次请求）</summary>
    private void InitDataSync()
    {
        if (_isDataSyncInitialized) return;

        RpcRegister(RPC_DataSync_Request, Rpc_ClientRequestSyncData);
        RpcRegister<string, ZPackage>(RPC_DataSync_Response, Rpc_ServerSendSyncData);
        ClientCallRpc_PeerInfo(RPC_DataSync_Request);

        _isDataSyncInitialized = true;
    }

    /// <summary>注：服务端接收客户端数据请求，遍历所有已注册模块并逐个下发数据</summary>
    private void Rpc_ClientRequestSyncData(long senderPeerId)
    {
        if (IsServer == false) return;

        foreach (var data in _syncProviders)
        {
            if (string.IsNullOrEmpty(data.Key) || data.Value == null) continue;

            if (data.Value.GetSyncZPackage() is not ZPackage zPackage) continue;

            TargetCallRpc<string, ZPackage>(RPC_DataSync_Response, senderPeerId, data.Key, zPackage);
        }
    }

    /// <summary>注：客户端接收服务端下发的单个模块数据，通过接口注入到对应Mod</summary>
    private void Rpc_ServerSendSyncData(long senderPeerId, string modFullName, ZPackage data)
    {
        if (IsServer) return;

        if (!_syncProviders.TryGetValue(modFullName, out var Intf))
        {
            Debug.LogWarning($"[RpcManager.Rpc_ServerSendSyncData] 未找到模块 {modFullName} 的数据接收器");
            return;
        }

        Intf.SetSyncZPackage(data);
    }

    #endregion



    #region 序列化工具

    /// <summary>注：将业务对象序列化并封装为全新ZPackage</summary>
    public ZPackage PackToZPackage<T>(T data)
    {
        byte[] bytes = SerializeToBytes(data);
        ZPackage pkg = new ZPackage();
        pkg.Write(bytes);
        return pkg;
    }

    /// <summary>注：读取ZPackage内二进制数据，反序列化为业务对象</summary>
    public T UnpackFromZPackage<T>(ZPackage pkg)
    {
        byte[] raw = pkg.ReadByteArray();
        return DeserializeFromBytes<T>(raw);
    }

    /// <summary>注：字节数组反序列化为指定类型对象</summary>
    private T DeserializeFromBytes<T>(byte[] serializedData)
    {
        using MemoryStream stream = new MemoryStream(serializedData);
        BinaryFormatter formatter = new BinaryFormatter();
        return (T)formatter.Deserialize(stream);
    }

    /// <summary>注：对象序列化为二进制字节数组</summary>
    private byte[] SerializeToBytes<T>(T objectToSerialize)
    {
        using MemoryStream stream = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, objectToSerialize);
        return stream.ToArray();
    }

    #endregion


    #region Rpc 注册与调用封装

    /// <summary>注：注册无参数RPC接收回调（在GameStart时注册）</summary>
    public void RpcRegister(string rpcName, System.Action<long> action) => PatchManager.OnGameStartRegister += () => ZRoutedRpc.instance.Register(rpcName, action);
    /// <summary>注：注册单参数RPC接收回调（在GameStart时注册）</summary>
    public void RpcRegister<T>(string rpcName, System.Action<long, T> action) => PatchManager.OnGameStartRegister += () => ZRoutedRpc.instance.Register(rpcName, action);
    /// <summary>注：注册双参数RPC接收回调（在GameStart时注册）</summary>
    public void RpcRegister<T1, T2>(string rpcName, System.Action<long, T1, T2> action) => PatchManager.OnGameStartRegister += () => ZRoutedRpc.instance.Register(rpcName, action);

    /// <summary>注：客户端调用无参数RPC（发给服务端）</summary>
    public void ClientCallRpc(string rpcName) => ZRoutedRpc.instance.InvokeRoutedRPC(rpcName);
    /// <summary>注：客户端调用单参数RPC（发给服务端）</summary>
    public void ClientCallRpc<T>(string rpcName, T data) => ZRoutedRpc.instance.InvokeRoutedRPC(rpcName, data);
    /// <summary>注：客户端调用双参数RPC（发给服务端）</summary>
    public void ClientCallRpc<T1, T2>(string rpcName, T1 data1, T2 data2) => ZRoutedRpc.instance.InvokeRoutedRPC(rpcName, data1, data2);

    /// <summary>注：定向发送无参数RPC给指定Peer</summary>
    public void TargetCallRpc(string rpcName, long targetPeerId) => ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, rpcName);
    /// <summary>注：定向发送单参数RPC给指定Peer</summary>
    public void TargetCallRpc<T>(string rpcName, long targetPeerId, T data) => ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, rpcName, data);
    /// <summary>注：定向发送双参数RPC给指定Peer</summary>
    public void TargetCallRpc<T1, T2>(string rpcName, long targetPeerId, T1 data1, T2 data2) => ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, rpcName, data1, data2);

    /// <summary>注：在PeerInfo握手完成后，客户端调用无参数RPC</summary>
    public void ClientCallRpc_PeerInfo(string rpcName) => PatchManager.OnZNetPeerInfo += () => ClientCallRpc(rpcName);
    /// <summary>注：在PeerInfo握手完成后，客户端调用单参数RPC</summary>
    public void ClientCallRpc_PeerInfo<T>(string rpcName, T data) => PatchManager.OnZNetPeerInfo += () => ClientCallRpc(rpcName, data);
    /// <summary>注：在PeerInfo握手完成后，客户端调用双参数RPC</summary>
    public void ClientCallRpc_PeerInfo<T1, T2>(string rpcName, T1 data1, T2 data2) => PatchManager.OnZNetPeerInfo += () => ClientCallRpc(rpcName, data1, data2);

    /// <summary>注：在PeerInfo握手完成后，定向发送无参数RPC给指定Peer</summary>
    public void TargetCallRpc_PeerInfo(string rpcName, long targetPeerId) => PatchManager.OnZNetPeerInfo += () => TargetCallRpc(rpcName, targetPeerId);
    /// <summary>注：在PeerInfo握手完成后，定向发送单参数RPC给指定Peer</summary>
    public void TargetCallRpc_PeerInfo<T>(string rpcName, long targetPeerId, T data) => PatchManager.OnZNetPeerInfo += () => TargetCallRpc(rpcName, targetPeerId, data);
    /// <summary>注：在PeerInfo握手完成后，定向发送双参数RPC给指定Peer</summary>
    public void TargetCallRpc_PeerInfo<T1, T2>(string rpcName, long targetPeerId, T1 data1, T2 data2) => PatchManager.OnZNetPeerInfo += () => TargetCallRpc(rpcName, targetPeerId, data1, data2);

    /// <summary>注：广播无参数RPC给所有Peer</summary>
    public void BroadcastCallRpc(string rpcName) => ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, rpcName);
    /// <summary>注：广播单参数RPC给所有Peer</summary>
    public void BroadcastCallRpc<T>(string rpcName, T data) => ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, rpcName, data);
    /// <summary>注：广播双参数RPC给所有Peer</summary>
    public void BroadcastCallRpc<T1, T2>(string rpcName, T1 data1, T2 data2) => ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, rpcName, data1, data2);

    #endregion
}

