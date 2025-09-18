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
            Debug.Log($"[{CatManagerPlugin.PluginName}] Mock完成-处理数量:[{MockObjectInfoList.Count}]-耗时: {elapsed1.TotalMilliseconds / 1000}秒 ");
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
                    Debug.Log($"[{CatManagerPlugin.PluginName}] 处理字段 {field.Name} 时出错：{ex.Message}");
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
                    Debug.Log($"[{CatManagerPlugin.PluginName}] 处理对象 {objType.Name} 的字段 {field.Name} 时出错：{ex.Message}");
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
                Debug.LogWarning($"[{CatManagerPlugin.PluginName}]：字段：【{targetField.Name}】不属于对象 类型：【{parentType.Name}】 跳过");
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
                    // 新增：跳过 null 元素，避免空引用
                    if (item == null)
                    {
                        index++;
                        continue;
                    }
                    CheckFieldOrElement(rootComponent, rootField, item, parentObject, index);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[{CatManagerPlugin.PluginName}] 处理列表元素（索引：{index}）时出错：{ex.Message}");
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
                        GameObject realPrefab = CatToolManager.GetGameObject(info.prefabName);
                        // 新增：检查真实预制件是否存在
                        if (realPrefab == null)
                        {
                            Debug.LogError($"[{CatManagerPlugin.PluginName}] 替换失败：未找到真实预制件 {info.prefabName}（对应占位预制件 {info.mockPrefab.name}）");
                            continue;
                        }
                        ReplaceFieldValue(info, realPrefab);
                    }
                    catch (Exception ex)
                    {
                        // 新增：输出详细错误信息（包含字段类型和预制件名称）
                        Debug.LogError($"[{CatManagerPlugin.PluginName}] 替换 {info.mockPrefab.name} 时出错：字段类型 {info.TargetField.FieldType.Name}，错误：{ex.Message}");
                    }
                }
                else if (!string.IsNullOrEmpty(info.ShaderName))
                {
                    var shader = CatToolManager.GetShader(info.ShaderName);
                    if (shader == null)
                    {
                        Debug.Log($"[{CatManagerPlugin.PluginName}] 执行ReplaceMockShaders时 找不到着色器：{info.ShaderName}");
                    }

                    try
                    {
                        // 替换材质中的着色器为真实着色器
                        info.TargetMaterial.shader = shader;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[{CatManagerPlugin.PluginName}] 替换着色器 {info.MockShader.name} 失败：{ex.Message}");
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
                Debug.Log($"[{CatManagerPlugin.PluginName}] 替换失败：ParentObject或TargetField为null");
                return;
            }

            Type targetType = info.ParentObject.GetType();
            Type fieldType = info.TargetField.FieldType;

            object fieldValue = info.TargetField.GetValue(info.ParentObject);
            if (fieldValue == null)
            {
                Debug.Log($"[{CatManagerPlugin.PluginName}] 字段值为null：{info.TargetField.Name}");
                return;
            }

            object valueToSet = GetValueForFieldType(fieldType, realPrefab);
            if (valueToSet == null)
            {
                Debug.LogError($"[{CatManagerPlugin.PluginName}] 无法获取替换值，字段类型：{fieldType.Name}，预制件：{realPrefab?.name}");
                return;
            }

            // 检查替换值与字段类型是否匹配（关键验证）
            if (!fieldType.IsInstanceOfType(valueToSet))
            {
                Debug.LogError($"[{CatManagerPlugin.PluginName}] 类型不匹配：字段类型 {fieldType.Name}，替换值类型 {valueToSet.GetType().Name}");
                return;
            }

            try
            {
                // 处理数组/列表元素
                if (info.ArrayIndex != -1)
                {
                    // 数组处理（针对单元素替换，而非整个数组替换）
                    if (fieldValue is GameObject[] gameObjectArray)
                    {
                        if (info.ArrayIndex >= 0 && info.ArrayIndex < gameObjectArray.Length)
                        {
                            // 若字段是GameObject[]数组，直接替换指定索引的元素为真实预制件
                            gameObjectArray[info.ArrayIndex] = realPrefab;
                        }
                        else
                        {
                            Debug.LogError($"[{CatManagerPlugin.PluginName}] 数组索引越界：{info.ArrayIndex}（数组长度：{gameObjectArray.Length}）");
                        }
                    }
                    else if (fieldValue is IList list)
                    {
                        list[info.ArrayIndex] = valueToSet;
                    }
                }
                // 处理整个数组替换（直接赋值新数组）
                else
                {
                    info.TargetField.SetValue(info.ParentObject, valueToSet);
                }
            }
            catch (Exception ex)
            {
                // 输出完整异常信息（包括堆栈跟踪）
                Debug.LogError($"[{CatManagerPlugin.PluginName}] 赋值时出错：字段 {info.TargetField.Name}，类型 {fieldType.Name}，错误：{ex.Message}\n堆栈：{ex.StackTrace}");
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
            // 处理 GameObject 直接引用
            if (fieldType == typeof(GameObject))
            {
                return realPrefab;
            }
            // 处理 GameObject[] 数组
            else if (fieldType == typeof(GameObject[]))
            {
                if (realPrefab == null)
                {
                    Debug.LogError($"[{CatManagerPlugin.PluginName}] 真实预制件为null，无法创建GameObject[]");
                    return null;
                }
                // 返回包含真实预制件的单元素数组（根据实际需求调整长度）
                var result = new GameObject[] { realPrefab };
                return result;
            }
            // 处理 Component 引用（原有逻辑）
            else if (typeof(Component).IsAssignableFrom(fieldType))
            {
                Component comp = realPrefab.GetComponent(fieldType);
                if (comp == null)
                {
                    Debug.LogWarning($"[{CatManagerPlugin.PluginName}] 预制件 {realPrefab.name} 缺少组件 {fieldType.Name}");
                }
                return comp;
            }
            // 处理其他数组类型（如 Component[]）
            else if (fieldType.IsArray)
            {
                Type elementType = fieldType.GetElementType();
                Debug.LogWarning($"[{CatManagerPlugin.PluginName}] 暂不支持数组类型 {fieldType.Name}（元素类型：{elementType.Name}）");
                return null;
            }
            // 其他不支持的类型
            else
            {
                Debug.LogWarning($"[{CatManagerPlugin.PluginName}] 不支持的字段类型：{fieldType.FullName}");
                return null;
            }
        }
        /// <summary>
        /// 注：清空预制件和着色器的替换列表，释放资源）
        /// </summary>
        private  void Cleanup() => MockObjectInfoList.Clear();
    }
}