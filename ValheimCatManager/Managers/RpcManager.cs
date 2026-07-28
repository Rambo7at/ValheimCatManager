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

namespace ValheimCatManager.Managers
{
    public class RpcManager
    {
        private static RpcManager _instance;
        public static RpcManager Instance => _instance ?? (_instance = new RpcManager());
        private RpcManager() { }

        List<string> VersionList = [];

        bool Check = true;

        private const string Rpc_ModVersionPush = "com.rambo7at.CatManager_ModVersionPush";
        private const string Rpc_ModVersionReply = "com.rambo7at.CatManager_ModVersionReply";


        /// <summary>注：注册 数据同步RPC 接收回调</summary>
        public void RpcRegister(string rpcName, System.Action<ZRpc, ZPackage> action) => HarmonyPatchManager.OnZNetRpcRegister += (peer) => peer.m_rpc.Register(rpcName, action);

        /// <summary>注：登记新连接自动推送RPC，执行于 ZNet.OnNewConnection 阶段触发</summary>
        public void RegisterAutoPushRpc(string rpcName, LazySyncPackage syncPackage)  =>  HarmonyPatchManager.OnCallRpcNewConnection += (peer) => peer.m_rpc.Invoke(rpcName, syncPackage.ZPackage);


        /// <summary>注：登记模组，用于联机版本校验</summary>
        public void RegisterModVersion()
        {
            Assembly mod = Assembly.GetCallingAssembly();
            string modName = mod.GetName().Name;
            if (!VersionList.Contains(modName)) VersionList.Add(modName);

            if (Check)
            {
                SetupVersionCheckRpc();
                Check = false;
            }

        }

        /// <summary>注：构建版本同步容器，注册整套版本校验RPC</summary>
        private void SetupVersionCheckRpc()
        {
            HarmonyPatchManager.OnZNetRpcRegister += (peer) =>
            {
                peer.m_rpc.Register(Rpc_ModVersionPush, new System.Action<ZRpc, List<string>>(OnReceiveServerModVersion));
                peer.m_rpc.Register(Rpc_ModVersionReply, new System.Action<ZRpc, List<string>>(OnReceiveClientModVersionReply));
            };

            HarmonyPatchManager.OnCallRpcNewConnection += (peer) =>
            {
                if (ZNet.instance.IsServer())
                {
                    peer.m_rpc.Invoke(Rpc_ModVersionPush, VersionList);
                }
            };
        }


        /// <summary>注：客户端接收服务端推送的模组版本列表并校验</summary>
        private void OnReceiveServerModVersion(ZRpc zRpc, List<string> serverModList)
        {
            if (ZNet.instance.IsServer()) return;

            bool mismatch = false;
            if (serverModList.Count != VersionList.Count) mismatch = true;

            else
            {
                foreach (var mod in serverModList)
                {
                    if (!VersionList.Contains(mod))
                    {
                        mismatch = true;
                        break;
                    }
                }
            }

            if (mismatch)
            {
                // 断开逻辑 zRpc.GetSocket().Close();
                return;
            }

            // 校验通过，向服务端回传自身模组清单
            zRpc.Invoke(Rpc_ModVersionReply, VersionList);
        }

        /// <summary>注：服务端接收客户端回传的模组版本列表并二次校验</summary>
        private void OnReceiveClientModVersionReply(ZRpc zRpc, List<string> clientModList)
        {
            if (!ZNet.instance.IsServer()) return;

            bool mismatch = false;
            if (clientModList.Count != VersionList.Count)
                mismatch = true;
            else
            {
                foreach (var mod in clientModList)
                {
                    if (!VersionList.Contains(mod))
                    {
                        mismatch = true;
                        break;
                    }
                }
            }

            if (mismatch)
            {
                // 断开逻辑 zRpc.GetSocket().Close();
            }
        }


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

    }
}
