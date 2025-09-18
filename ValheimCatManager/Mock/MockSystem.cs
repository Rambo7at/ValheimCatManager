using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;
using static ValheimCatManager.Data.CatModData;
using Component = UnityEngine.Component;

namespace ValheimCatManager.Mock
{
    /// <summary>
    /// 模拟系统（用于替换占位预制件和占位着色器为真实资源）
    /// </summary>
    public class MockSystem
    {
        /// <summary>
        /// 注：默认预制件字典
        /// </summary>
        public readonly Dictionary<int, string> mockPrefabDict = new Dictionary<int, string>();

        /// <summary>
        /// 注：存储需要替换的预制件信息列表
        /// </summary>
        private readonly List<MockObjectInfo> MockObjectInfoList = new List<MockObjectInfo>();

        private static MockSystem _instance;

        public static MockSystem Instance => _instance ?? (_instance = new MockSystem());

        private MockSystem() => new Harmony("MockSystem").PatchAll(typeof(MockSystemPatch));


        private class MockSystemPatch()
        {
            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPostfix, HarmonyPriority(Priority.First)]
            static void MockStart(ObjectDB __instance)
            {
                // 仅在主场景（"main"）执行
                if (SceneManager.GetActiveScene().name == "main")
                {
                    Instance.StartMockPrefab(); 
                    CatModData.m_PrefabCache.Clear(); 
                }
            }

        }

        /// <summary>
        /// 需求组件的列表信息（用于收集需处理的组件）
        /// </summary>
        private  readonly List<Component> ComponentsList = new List<Component>();

        /// <summary>
        /// 启用 MockSystem（执行预制件和着色器的替换流程）<br></br>
        /// 流程：清理原有数据 → 收集预制件/着色器信息 → 替换预制件/着色器 → 清理缓存
        /// </summary>
        private void StartMockPrefab()
        {
            var startTime1 = DateTime.Now;
            MockObjectInfoList.Clear();
            CollectMockPrefab();       // 收集信息
            ReplacePlaceholders();     // 原有：替换预制件
            var elapsed1 = DateTime.Now - startTime1; // 计算耗时
            Debug.Log($"[{CatMonstAltar.PluginName}] Mock完成-处理数量:[{MockObjectInfoList.Count}]-耗时: {elapsed1.TotalMilliseconds / 1000}秒 ");
            Cleanup();
        }

        /// <summary>
        /// [1-收集阶段]<br></br>
        /// 注：收集需要mock的预制件信息
        /// </summary>
        private void CollectMockPrefab()
        {
            foreach (var item in Instance.mockPrefabDict)
            {
                GameObject prefab = CatToolManager.GetGameObject(item.Key);

                if (!prefab){ Debug.LogError($"执行CollectMockPrefab方法时，获取预制件:[{item.Value}]是空");continue;}

                if (prefab.name == item.Value) CollectComponents(prefab);
            }
        }



        /// <summary>
        /// [2-收集阶段]<br></br>
        /// 注：收集预制件及其子对象的相关组件
        /// </summary>
        private void CollectComponents(GameObject prefab)
        {
            //获取所有组件（包括禁用状态的和所有子对象）
            Component[] allComponents = prefab.GetComponentsInChildren<Component>(true);

            foreach (Component component in allComponents)
            {
                if (!component) continue;
                CheckComponentFields(component); // 检查组件字段（预制件替换）
            }
        }


        /// <summary>
        /// [3-收集阶段]<br></br>
        /// 注：收集组件内的字段信息
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

                    CheckFieldOrElement(component, field, fieldValue, component, -1);

                }
                catch (Exception ex)
                {
                    Debug.Log($"[{CatMonstAltar.PluginName}] 处理字段 {field.Name} 时出错：{ex.Message}");
                }
            }
        }
        /// [3-收集阶段]<br></br>
        /// 注：递归检查自定义对象的内部字段，收集嵌套结构中的占位预制件信息
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

                    // 修正：parentObject 应传入当前的 obj（字段所属的实际对象），而不是 rootComponent
                    CheckFieldOrElement(rootComponent, field, fieldValue, obj, -1);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[{CatMonstAltar.PluginName}] 处理对象 {objType.Name} 的字段 {field.Name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// [辅助方法]<br></br>
        /// 注：判断字段类型，并进行对应处理
        /// </summary>
        private void CheckFieldOrElement(Component rootComponent, FieldInfo field, object value, object parentObject, int index)
        {
            // 处理直接的GameObject引用
            if (value is GameObject prefab) SaveMockPrefabInfo(rootComponent, field, prefab, parentObject, index);
            // 处理Renderer（着色器）
            else if (value is Renderer renderer) SaveMockShaderInfo(renderer);
            // 处理Component引用（取其GameObject）
            else if (value is Component componentValue) SaveMockPrefabInfo(rootComponent, field, componentValue.gameObject, parentObject, index);
            // 处理集合类型（遍历元素）
            else if (value is IEnumerable enumerable) HandleEnumerable(rootComponent, field, enumerable, parentObject);
            // 处理自定义对象（递归检查内部字段）
            else if (value.GetType().IsClass) CheckObjectFields(rootComponent, field, value, parentObject);

        }

        /// <summary>
        /// [1-存储阶段]<br></br>
        /// 注：存储带有占位信息的预制件
        /// </summary>
        private void SaveMockPrefabInfo(Component rootComponent, FieldInfo targetField, GameObject prefab, object parentObject, int arrayIndex)
        {
            // 仅处理占位预制件（名称以JVLmock_为前缀）
            if (!prefab || !prefab.name.StartsWith("JVLmock_")) return;

            Type parentType = parentObject.GetType();
            // 验证字段所属关系，避免类型不匹配
            if (!targetField.DeclaringType.IsAssignableFrom(parentType))
            {
                Debug.LogWarning($"[{CatMonstAltar.PluginName}]：字段：【{targetField.Name}】不属于对象 类型：【{parentType.Name}】 跳过");
                return;
            }

            // 去除JVLmock_前缀，获取真实预制件名称
            string oPrefabName = prefab.name.Substring("JVLmock_".Length);
            MockObjectInfoList.Add(new MockObjectInfo
            {
                RootComponent = rootComponent,
                TargetField = targetField,
                mockPrefab = prefab,
                prefabName = oPrefabName,
                ParentObject = parentObject,
                ArrayIndex = arrayIndex
            });
        }
        /// <summary>
        /// [1-存储阶段]<br></br>
        /// 注：存储带有占位信息的预制件
        /// </summary>
        private void SaveMockShaderInfo(Renderer renderer)
        {
            if (!renderer || renderer.sharedMaterials == null) return;

            // 遍历渲染器的所有材质
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material mat = renderer.sharedMaterials[i];
                if (!mat || !mat.shader) continue;

                // 检测名称以JVLmock_为前缀的占位着色器
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
        /// [1-存储阶段]<br></br>
        /// 注：处理数据集合类型
        /// </summary>
        private void HandleEnumerable(Component rootComponent, FieldInfo rootField, IEnumerable enumerable, object parentObject)
        {
            int index = 0;
            foreach (var item in enumerable)
            {
                try
                {
                    CheckFieldOrElement(rootComponent, rootField, item, parentObject, index);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[{CatMonstAltar.PluginName}] 处理列表元素（索引：{index}）时出错：{ex.Message}");
                }
                index++;
            }
        }

        /// <summary>
        /// [1-处理阶段]<br></br>
        /// 注：处理阶段入口
        /// </summary>
        private void ReplacePlaceholders()
        {
            foreach (var info in MockObjectInfoList)
            {
                if (!string.IsNullOrEmpty(info.prefabName))
                {
                    try
                    {
                        ReplaceFieldValue(info, CatToolManager.GetGameObject(info.prefabName));
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[{CatMonstAltar.PluginName}] 替换 {info.mockPrefab.name} 时出错：{ex.Message}");
                    }
                }
                else if (!string.IsNullOrEmpty(info.ShaderName))
                {
                    var shader = CatToolManager.GetShader(info.ShaderName);
                    if (shader == null)
                    {
                        Debug.Log($"[{CatMonstAltar.PluginName}] 执行ReplaceMockShaders时 找不到着色器：{info.ShaderName}");
                    }

                    try
                    {
                        // 替换材质中的着色器为真实着色器
                        info.TargetMaterial.shader = shader;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[{CatMonstAltar.PluginName}] 替换着色器 {info.MockShader.name} 失败：{ex.Message}");
                    }
                }
            }
        }
        /// <summary>
        /// [2-处理阶段]<br></br>
        /// 注：根据字段类型和索引，更新占位预制件引用为真实预制件
        /// </summary>
        private void ReplaceFieldValue(MockObjectInfo info, GameObject realPrefab)
        {
            if (info.ParentObject == null || info.TargetField == null)
            {
                Debug.Log($"[{CatMonstAltar.PluginName}] 替换失败：ParentObject或TargetField为null");
                return;
            }

            Type targetType = info.ParentObject.GetType();
            // 验证字段所属关系，避免类型不匹配
            if (!info.TargetField.DeclaringType.IsAssignableFrom(targetType))
            {
                Debug.Log($"[{CatMonstAltar.PluginName}] 字段 {info.TargetField.Name} 不属于对象类型 {targetType.Name}");
                return;
            }

            object fieldValue = info.TargetField.GetValue(info.ParentObject);
            if (fieldValue == null) return;

            // 根据目标字段类型获取正确的赋值对象
            object valueToSet = GetValueForFieldType(info.TargetField.FieldType, realPrefab);
            if (valueToSet == null)
            {
                Debug.LogError($"[{CatMonstAltar.PluginName}] 预制件 {realPrefab.name} 不包含字段所需的组件类型 {info.TargetField.FieldType.Name}");
                return;
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
        /// [辅助方法]<br></br>
        /// 根据字段类型获取正确的赋值对象<br></br>
        /// - 如果是GameObject类型，直接返回realPrefab<br></br>
        /// - 如果是Component类型，返回realPrefab上的对应组件
        /// </summary>
        private object GetValueForFieldType(Type fieldType, GameObject realPrefab)
        {
            // 字段类型是GameObject，直接返回
            if (fieldType == typeof(GameObject))
            {
                return realPrefab;
            }
            // 字段类型是Component或其派生类，获取组件
            else if (typeof(Component).IsAssignableFrom(fieldType))
            {
                return realPrefab.GetComponent(fieldType);
            }
            // 不支持的类型
            return null;
        }
        /// <summary>
        /// 注：清空预制件和着色器的替换列表，释放资源）
        /// </summary>
        private  void Cleanup() => MockObjectInfoList.Clear();
    }
}