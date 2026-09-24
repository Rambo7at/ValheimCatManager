using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Data;

namespace ValheimCatManager.CatUtils;

/// <summary>注：磁盘AssetBundle材质工具：全量扫描磁盘bundle兜底加载出生点未预加载的材质，并提供已加载bundle内资产归属的诊断搜索</summary>
internal class DiskBundleMaterialLoader
{
    /// <summary>注：SoftRef目录下的清单文件名（非AssetBundle，LoadFromFile会报读头错误，需跳过）</summary>
    private static readonly HashSet<string> m_manifestFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hash", "manifest", "manifest_extended"
        };

    /// <summary>注：扫描StreamingAssets下未加载的小体积bundle建立材质索引并按名加载，兜底官方出生点未预加载的材质</summary>
    public static Material LoadMaterialFromDiskBundles(string name)
    {
        // 首次调用：扫描磁盘bundle建立全量材质索引（只构建一次）
        if (CatModData.m_diskMaterialIndex == null)
        {
            CatModData.m_diskMaterialIndex = new Dictionary<string, (string, string)>();

            // 已加载bundle的文件名集合：这些由"已加载bundle"查找层负责，磁盘层只扫未加载的
            HashSet<string> loadedBundleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AssetBundle loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (loadedBundle == null) continue;
                loadedBundleNames.Add(Path.GetFileName(loadedBundle.name));
            }

            const long maxBundleSize = 1024L * 1024L * 1024L; // 超过1GB的bundle不扫描
            int skippedLargeCount = 0;

            foreach (string filePath in Directory.GetFiles(Application.streamingAssetsPath, "*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(filePath);
                if (Path.GetExtension(filePath) != "") continue;       // 跳过有扩展名的非bundle文件
                if (m_manifestFileNames.Contains(fileName)) continue;  // 跳过SoftRef清单文件
                if (loadedBundleNames.Contains(fileName)) continue;    // 跳过已加载的bundle

                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > maxBundleSize) { skippedLargeCount++; continue; } // 跳过超大bundle

                AssetBundle bundle;
                try { bundle = AssetBundle.LoadFromFile(filePath); }
                catch { continue; }
                if (bundle == null) continue;
                if (bundle.isStreamedSceneAssetBundle) { bundle.Unload(false); continue; } // 场景bundle不含材质，直接释放

                string[] assetNames;
                try { assetNames = bundle.GetAllAssetNames(); }
                catch { bundle.Unload(false); continue; }

                foreach (string assetName in assetNames)
                {
                    if (!assetName.ToLower().EndsWith(".mat")) continue;
                    string materialName = Path.GetFileNameWithoutExtension(assetName).ToLower();
                    if (!CatModData.m_diskMaterialIndex.ContainsKey(materialName))
                        CatModData.m_diskMaterialIndex.Add(materialName, (filePath, assetName));
                }

                // 建完索引立即释放文件占用，避免与官方SoftReference加载系统冲突（未取出任何资产，Unload(false)无损失）
                bundle.Unload(false);
            }
            Debug.Log($"[DiskBundleMaterialLoader] 磁盘材质索引构建完成，共 {CatModData.m_diskMaterialIndex.Count} 个材质，跳过超大bundle {skippedLargeCount} 个");
        }

        // 按材质名定位所在bundle
        if (!CatModData.m_diskMaterialIndex.TryGetValue(name.ToLower(), out var location)) return null;

        // 先查已加载bundle：官方可能已自行加载，重复LoadFromFile同一文件会直接报错
        string targetFileName = Path.GetFileName(location.bundlePath);
        AssetBundle targetBundle = null;
        foreach (AssetBundle loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (loadedBundle == null) continue;
            if (!string.Equals(Path.GetFileName(loadedBundle.name), targetFileName, StringComparison.OrdinalIgnoreCase)) continue;
            targetBundle = loadedBundle;
            break;
        }

        // 官方没加载才自己临时加载
        bool loadedBySelf = false;
        if (targetBundle == null)
        {
            try { targetBundle = AssetBundle.LoadFromFile(location.bundlePath); loadedBySelf = true; }
            catch { targetBundle = null; }
        }
        if (targetBundle == null) return null;

        Material material = null;
        try { material = targetBundle.LoadAsset<Material>(location.assetPath); }
        catch { material = null; }

        // 自己临时加载的：取完立即释放，材质对象靠m_materialCache强引用保留可用
        if (loadedBySelf) targetBundle.Unload(false);

        if (material != null)
        {
            CatModData.m_materialCache[name] = material; // 缓存并持有引用，防止被卸载
            Debug.Log($"[DiskBundleMaterialLoader] 已从磁盘bundle加载材质：{name}（bundle：{targetFileName}）");
            return material;
        }
        return null;
    }

    /// <summary>注：F6诊断：在所有已加载bundle中按关键词搜索资产路径并打印，用于定位某个材质（或任意资产）归属哪个bundle</summary>
    public static void SearchAssetsInLoadedBundles(string keyword)
    {
        int hitCount = 0;
        foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (bundle == null) continue;
            if (bundle.isStreamedSceneAssetBundle) continue; // 场景bundle不含普通资产，跳过

            string[] assetNames;
            try { assetNames = bundle.GetAllAssetNames(); }
            catch { continue; }

            foreach (string assetName in assetNames)
            {
                if (!assetName.ToLower().Contains(keyword.ToLower())) continue;
                hitCount++;
                Debug.Log($"[DiskBundleMaterialLoader][F6资产搜索] bundle={bundle.name}，asset={assetName}");
            }
        }
        Debug.Log($"[DiskBundleMaterialLoader][F6资产搜索] 关键词「{keyword}」搜索完成，命中 {hitCount} 条");
    }
}


