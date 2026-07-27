using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Data;
using static ZRpc;

namespace ValheimCatManager.Managers
{
    public class RpcManager
    {
        private static RpcManager _instance;

        public static RpcManager Instance => _instance ?? (_instance = new RpcManager());

        private RpcManager() { }


        /// <summary>注：注册 数据同步RPC 接收回调</summary>
        public void RpcRegister(string rpcName, Action<ZRpc, ZPackage> action)
        {
            HarmonyPatchManager.OnZNetRpcRegister += (peer) =>
            {
                peer.m_rpc.Register(rpcName, action);
                Debug.LogError($"[RpcManager.RpcRegister]：已完成 RPC 注册 + {rpcName} ，是否是服务器 :{peer.m_server}");
            };
        }

        /// <summary>注：登记新连接自动推送RPC，执行节点位于 ZNet.OnNewConnection 触发阶段</summary>
        public void RegisterAutoPushRpc(string rpcName, LazySyncPackage syncPackage)
        {
            HarmonyPatchManager.OnCallRpcNewConnection += (peer) =>
            {
                peer.m_rpc.Invoke(rpcName, syncPackage.ZPackage);
                Debug.LogError($"[RpcManager.CallRpc]：新连接自动推送RPC + {rpcName} ，是否是服务器 :{peer.m_server}");
            };
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
