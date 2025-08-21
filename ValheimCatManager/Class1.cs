using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

public static class ZNetSceneReflection
{
    public static Dictionary<int, GameObject> GetNamedPrefabs(ZNetScene zNetScene)
    {
        if (zNetScene == null)
        {
            Debug.LogError("ZNetScene实例为null，无法获取m_namedPrefabs");
            return null;
        }

        try
        {
            // 1. 获取ZNetScene类的Type对象（这是反射的起点）
            Type zNetSceneType = typeof(ZNetScene);

            // 2. 通过Type.GetField()获取私有字段m_namedPrefabs
            // 必须指定BindingFlags：NonPublic（私有/内部字段） + Instance（实例字段，非静态）
            FieldInfo namedPrefabsField = zNetSceneType.GetField(
                "m_namedPrefabs",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            // 3. 检查字段是否存在（防止字段名变更导致的错误）
            if (namedPrefabsField == null)
            {
                Debug.LogError("未找到ZNetScene的m_namedPrefabs字段，可能游戏版本已更新");
                return null;
            }

            // 4. 获取字段的值，并转换为实际类型（Dictionary<int, GameObject>）
            object fieldValue = namedPrefabsField.GetValue(zNetScene);
            Dictionary<int, GameObject> namedPrefabs = fieldValue as Dictionary<int, GameObject>;

            // 5. 检查类型转换是否成功
            if (namedPrefabs == null)
            {
                Debug.LogError("m_namedPrefabs的类型不是预期的Dictionary<int, GameObject>");
            }

            return namedPrefabs;
        }
        catch (Exception ex)
        {
            Debug.LogError($"获取m_namedPrefabs失败：{ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    // 使用示例
    public static void ExampleUsage()
    {
        if (ZNetScene.instance != null)
        {
            // 调用方法获取m_namedPrefabs
            var namedPrefabs = GetNamedPrefabs(ZNetScene.instance);

            if (namedPrefabs != null)
            {
                Debug.Log($"成功获取m_namedPrefabs，包含{namedPrefabs.Count}个预制体");
                // 可以在这里操作字典（添加/删除条目等）
            }
        }
    }
}
