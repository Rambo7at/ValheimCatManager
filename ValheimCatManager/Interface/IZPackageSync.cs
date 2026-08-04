using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimCatManager.Interface;
/// <summary>注：数据同步接口，下游 Mod 实现此接口即可接入通用数据同步框架</summary>
public interface IZPackageSync
{
    /// <summary>注：服务端调用，将当前配置数据打包为 ZPackage</summary>
    ZPackage GetSyncZPackage();

    /// <summary>注：客户端调用，从 ZPackage 还原配置并应用到本地</summary>
    void SetSyncZPackage(ZPackage zPackage);
}

