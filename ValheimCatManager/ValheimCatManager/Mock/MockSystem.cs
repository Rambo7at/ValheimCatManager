using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager;
using ValheimCatManager.ValheimCatManager.Data;
using ValheimCatManager.ValheimCatManager.Mock;
using ValheimCatManager.ValheimCatManager.Tool;
using static ValheimCatManager.ValheimCatManager.Data.CatModData;

namespace ValheimCatManager.ValheimCatManager.Mock
{
    /// <summary>
    /// 模拟系统（用于替换占位预制件、占位着色器、占位材质为真实资源）
    /// </summary>
    public class MockSystem
    {
        /// <summary>
        /// 默认预制件字典（key：预制件ID，value：预制件名称）
        /// </summary>
        public readonly Dictionary<int, string> mockPrefabDict = new Dictionary<int, string>();

        private int mockNum = 0;
        private string mockDebugName;

        /// <summary>
        /// 存储需要替换的预制件/着色器/材质信息列表
        /// </summary>
        private readonly List<MockObjectInfo> MockObjectInfoList = new List<MockObjectInfo>();

        private static MockSystem _instance;
        public static MockSystem Instance => _instance ?? (_instance = new MockSystem());

        private MockSystem() => new Harmony("MockSystem").PatchAll(typeof(MockSystemPatch));

        /// <summary>
        /// Harmony补丁类（修复参数不匹配问题，确保补丁生效）
        /// </summary>
        private class MockSystemPatch
        {
            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPostfix, HarmonyPriority(Priority.First)]
            static void MockStart(ZoneSystem __instance) // 修复：参数改为ZoneSystem（匹配Start无参签名）
            {
                // 仅在主场景（"main"）执行
                if (SceneManager.GetActiveScene().name == "main")
                {
                    Instance.StartMockPrefab();
                    m_PrefabCache.Clear();
                }
            }
        }

        /// <summary>
        /// 设置调试名称（用于日志区分）
        /// </summary>
        public void LoadmockDebugName(string name) => mockDebugName = name;

        /// <summary>
        /// 启用 MockSystem（执行预制件、着色器、材质的替换流程）<br></br>
        /// 流程：清理原有数据 → 收集信息 → 替换资源 → 清理缓存
        /// </summary>
        private void StartMockPrefab()
        {
            var startTime1 = DateTime.Now;
            MockObjectInfoList.Clear();
            CollectMockPrefab();       // 收集占位资源信息
            ReplacePlaceholders();     // 替换预制件/着色器/材质
            var elapsed1 = DateTime.Now - startTime1;

            int totalCount = MockObjectInfoList.Count + mockNum;
            Debug.Log($"[{mockDebugName}] Mock完成-处理数量:[{totalCount}]-耗时: {elapsed1.TotalMilliseconds / 1000}秒 ");
            Cleanup();
            mockNum = 0;
        }

        /// <summary>
        /// [1-收集阶段] 收集需要mock的预制件信息
        /// </summary>
        private void CollectMockPrefab()
        {
            foreach (var item in Instance.mockPrefabDict)
            {
                GameObject prefab = CatToolManagerOld.GetGameObject(item.Key);
                if (!prefab)
                {
                    Debug.LogError($"执行CollectMockPrefab方法时，获取预制件:[{item.Value}]是空");
                    continue;
                }

                if (prefab.name == item.Value)
                    CollectComponents(prefab);
            }
        }

        /// <summary>
        /// [2-收集阶段] 收集预制件及其子对象的组件、着色器、材质信息
        /// </summary>
        private void CollectComponents(GameObject prefab)
        {
            // 收集所有Renderer的着色器和材质信息
            Renderer[] allRenderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in allRenderers)
            {
                SaveMockShaderInfo(renderer);   // 收集占位着色器
                SaveMockMaterialInfo(renderer); // 收集占位材质【新增】
            }

            // 收集所有组件的字段信息（预制件替换）
            Component[] allComponents = prefab.GetComponentsInChildren<Component>(true);
            foreach (Component component in allComponents)
            {
                if (!component) continue;
                CheckComponentFields(component);
            }
        }

        /// <summary>
        /// [3-收集阶段] 检查组件内的字段信息（预制件替换相关）
        /// </summary>
        private void CheckComponentFields(Component component)
        {
            Type compType = component.GetType();
            FieldInfo[] fields = compType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                try
                {
                    if (field.IsPrivate) continue;
                    object fieldValue = field.GetValue(component);
                    if (fieldValue == null) continue;

                    if (field.FieldType == typeof(DropTable))
                    {
                        DefaultItemsReplace(component, field, fieldValue, component);
                    }

                    CheckFieldOrElement(component, field, fieldValue, component, -1);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[{mockDebugName}] 处理字段 {field.Name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理DropTable的默认物品替换
        /// </summary>
        private void DefaultItemsReplace(Component rootComponent, FieldInfo field, object dropTable, object parentObject)
        {
            Type parentType = parentObject.GetType();
            if (!field.DeclaringType.IsAssignableFrom(parentType))
            {
                Debug.LogWarning($"[{mockDebugName}]：字段：【{field.Name}】不属于对象 类型：【{parentType.Name}】 跳过");
                return;
            }

            var dropTableObj = (DropTable)dropTable;
            ReplaceGameobject(dropTableObj.m_drops);
        }

        /// <summary>
        /// 替换DropTable中的占位预制件
        /// </summary>
        private void ReplaceGameobject(List<DropTable.DropData> dropDatas)
        {
            for (int i = 0; i < dropDatas.Count; i++)
            {
                if (dropDatas[i].m_item.name.StartsWith("JVLmock_"))
                {
                    var itemName = dropDatas[i].m_item.name.Substring("JVLmock_".Length);
                    var gameObject = CatToolManagerOld.GetGameObject(itemName);

                    DropTable.DropData newDropData = new DropTable.DropData()
                    {
                        m_item = gameObject,
                        m_stackMin = dropDatas[i].m_stackMin,
                        m_stackMax = dropDatas[i].m_stackMax,
                        m_weight = dropDatas[i].m_weight,
                        m_dontScale = dropDatas[i].m_dontScale
                    };
                    dropDatas[i] = newDropData;
                    mockNum++;
                }
            }
        }

        /// <summary>
        /// [3-收集阶段] 递归检查自定义对象的内部字段
        /// </summary>
        private void CheckObjectFields(Component rootComponent, FieldInfo parentField, object obj, object parentObject)
        {
            if (obj == null) return;

            Type objType = obj.GetType();
            if (objType.IsPrimitive || objType == typeof(string) || objType == typeof(object)) return;

            FieldInfo[] fields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                try
                {
                    if (field.IsPrivate) continue;
                    object fieldValue = field.GetValue(obj);
                    if (fieldValue == null) continue;

                    CheckFieldOrElement(rootComponent, field, fieldValue, obj, -1);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[{mockDebugName}] 处理对象 {objType.Name} 的字段 {field.Name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// [辅助方法] 判断字段类型，并进行对应处理
        /// </summary>
        private void CheckFieldOrElement(Component rootComponent, FieldInfo field, object value, object parentObject, int index)
        {
            // 处理直接的GameObject引用
            if (value is GameObject prefab)
                SaveMockPrefabInfo(rootComponent, field, prefab, parentObject, index);
            // 处理Component引用（取其GameObject）
            else if (value is Component componentValue)
                SaveMockPrefabInfo(rootComponent, field, componentValue.gameObject, parentObject, index);
            // 处理集合类型（遍历元素）
            else if (value is IEnumerable enumerable)
                HandleEnumerable(rootComponent, field, enumerable, parentObject);
            // 处理自定义对象（递归检查内部字段）
            else if (value.GetType().IsClass)
                CheckObjectFields(rootComponent, field, value, parentObject);
        }

        /// <summary>
        /// [存储阶段] 存储带有占位信息的预制件
        /// </summary>
        private void SaveMockPrefabInfo(Component rootComponent, FieldInfo targetField, GameObject prefab, object parentObject, int arrayIndex)
        {
            if (!prefab || !prefab.name.StartsWith("JVLmock_")) return;

            Type parentType = parentObject.GetType();
            if (!targetField.DeclaringType.IsAssignableFrom(parentType))
            {
                Debug.LogWarning($"[{mockDebugName}]：字段：【{targetField.Name}】不属于对象 类型：【{parentType.Name}】 跳过");
                return;
            }

            string realPrefabName = prefab.name.Substring("JVLmock_".Length);
            MockObjectInfoList.Add(new MockObjectInfo
            {
                RootComponent = rootComponent,
                TargetField = targetField,
                mockPrefab = prefab,
                prefabName = realPrefabName,
                ParentObject = parentObject,
                ArrayIndex = arrayIndex
            });
        }

        /// <summary>
        /// [存储阶段] 存储带有占位信息的着色器
        /// </summary>
        private void SaveMockShaderInfo(Renderer renderer)
        {
            if (!renderer || renderer.sharedMaterials == null) return;

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material mat = renderer.sharedMaterials[i];
                if (!mat || !mat.shader) continue;

                if (mat.shader.name.StartsWith("JVLmock_"))
                {
                    string realShaderName = mat.shader.name.Substring("JVLmock_".Length);
                    MockObjectInfoList.Add(new MockObjectInfo
                    {
                        RootComponent = renderer,
                        TargetMaterial = mat,
                        MockShader = mat.shader,
                        ShaderName = realShaderName,
                        MaterialIndex = i
                    });
                }
            }
        }

        /// <summary>
        /// [存储阶段] 存储带有占位信息的材质【新增】
        /// </summary>
        private void SaveMockMaterialInfo(Renderer renderer)
        {
            if (!renderer || renderer.sharedMaterials == null) return;

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material placeholderMat = renderer.sharedMaterials[i];
                if (!placeholderMat) continue;

                if (placeholderMat.name.StartsWith("JVLmock_"))
                {
                    string realMaterialName = placeholderMat.name.Substring("JVLmock_".Length);
                    MockObjectInfoList.Add(new MockObjectInfo
                    {
                        RootComponent = renderer,
                        TargetMaterial = placeholderMat,
                        MaterialIndex = i,
                        MaterialName = realMaterialName
                    });
                }
            }
        }

        /// <summary>
        /// [存储阶段] 处理数据集合类型
        /// </summary>
        private void HandleEnumerable(Component rootComponent, FieldInfo rootField, IEnumerable enumerable, object parentObject)
        {
            int index = 0;
            foreach (var item in enumerable)
            {
                try
                {
                    if (item == null)
                    {
                        index++;
                        continue;
                    }
                    CheckFieldOrElement(rootComponent, rootField, item, parentObject, index);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[{mockDebugName}] 处理列表元素（索引：{index}）时出错：{ex.Message}");
                }
                index++;
            }
        }

        /// <summary>
        /// [处理阶段] 替换占位预制件、着色器、材质为真实资源
        /// </summary>
        private void ReplacePlaceholders()
        {
            foreach (var info in MockObjectInfoList)
            {
                // 替换预制件
                if (!string.IsNullOrEmpty(info.prefabName))
                {
                    try
                    {
                        ReplaceFieldValue(info, CatToolManagerOld.GetGameObject(info.prefabName));
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[{mockDebugName}] 替换预制件 {info.mockPrefab.name} 时出错：{ex.Message}");
                    }
                }
                // 替换着色器
                else if (!string.IsNullOrEmpty(info.ShaderName))
                {
                    var shader = CatToolManagerOld.GetShader(info.ShaderName);
                    if (shader == null)
                    {
                        Debug.Log($"[{mockDebugName}] 找不到着色器：{info.ShaderName}");
                        continue;
                    }

                    try
                    {
                        info.TargetMaterial.shader = shader;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[{mockDebugName}] 替换着色器 {info.MockShader.name} 失败：{ex.Message}");
                    }
                }
                // 替换材质【新增】
                else if (!string.IsNullOrEmpty(info.MaterialName))
                {
                    Material realMaterial = CatToolManagerOld.GetMaterial(info.MaterialName);
                    if (realMaterial == null)
                    {
                        Debug.LogWarning($"[{mockDebugName}] 未找到真实材质：{info.MaterialName}（占位材质：{info.TargetMaterial?.name}）");
                        continue;
                    }

                    try
                    {
                        Renderer targetRenderer = info.RootComponent as Renderer;
                        if (targetRenderer == null)
                        {
                            Debug.LogWarning($"[{mockDebugName}] 材质替换失败：关联组件不是Renderer");
                            continue;
                        }

                        // 克隆材质数组（解决只读问题）
                        Material[] newMaterials = targetRenderer.sharedMaterials.Clone() as Material[];
                        newMaterials[info.MaterialIndex] = realMaterial;
                        targetRenderer.sharedMaterials = newMaterials;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[{mockDebugName}] 材质替换失败：{info.TargetMaterial?.name} → {info.MaterialName}，错误：{ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// [处理阶段] 更新占位预制件引用为真实预制件
        /// </summary>
        private void ReplaceFieldValue(MockObjectInfo info, GameObject realPrefab)
        {
            if (info.ParentObject == null || info.TargetField == null || realPrefab == null)
            {
                Debug.Log($"[{mockDebugName}] 预制件替换失败：ParentObject、TargetField或realPrefab为null");
                return;
            }

            Type targetType = info.ParentObject.GetType();
            if (!info.TargetField.DeclaringType.IsAssignableFrom(targetType))
            {
                Debug.Log($"[{mockDebugName}] 字段 {info.TargetField.Name} 不属于对象类型 {targetType.Name}");
                return;
            }

            object fieldValue = info.TargetField.GetValue(info.ParentObject);
            if (fieldValue == null) return;

            // 处理Component类型字段
            object valueToSet = realPrefab;
            if (typeof(Component).IsAssignableFrom(info.TargetField.FieldType))
            {
                Component targetComponent = realPrefab.GetComponent(info.TargetField.FieldType);
                if (targetComponent == null)
                {
                    Debug.LogError($"[{mockDebugName}] 真实预制件 {realPrefab.name} 上找不到 {info.TargetField.FieldType.Name} 组件");
                    return;
                }
                valueToSet = targetComponent;
            }

            // 处理数组/列表元素
            if (info.ArrayIndex != -1)
            {
                if (fieldValue is Array array && info.ArrayIndex >= 0 && info.ArrayIndex < array.Length)
                {
                    array.SetValue(valueToSet, info.ArrayIndex);
                }
                else if (fieldValue is IList list && info.ArrayIndex >= 0 && info.ArrayIndex < list.Count)
                {
                    list[info.ArrayIndex] = valueToSet;
                }
            }
            // 处理直接引用
            else
            {
                info.TargetField.SetValue(info.ParentObject, valueToSet);
            }
        }

        /// <summary>
        /// 清理替换列表，释放资源
        /// </summary>
        private void Cleanup() => MockObjectInfoList.Clear();
    }
}