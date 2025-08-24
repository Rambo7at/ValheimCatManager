using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;

namespace ValheimCatManager.Mock
{
    /// <summary>
    /// 模拟预制件信息
    /// </summary>
    public struct MockPrefabInfo
    {
        public Component RootComponent;  // 根组件（原始Component）
        public FieldInfo TargetField;    // 实际存储引用的字段（必须属于ParentObject类型）
        public GameObject mockPrefab;    // 占位预制件
        public string prefabName; // 目标真实预制件名称
        public object ParentObject;      // 字段所属的对象（可能是嵌套对象）
        public int ArrayIndex;           // 数组/列表索引（-1表示非数组元素）
    }

    /// <summary>
    /// 新增：模拟着色器信息（JVLmock_前缀着色器）
    /// </summary>
    public struct MockShaderInfo
    {
        public Component RootComponent;  // 关联的根组件（渲染组件）
        public Material TargetMaterial;  // 包含占位着色器的材质
        public Shader MockShader;        // 占位着色器（JVLmock_xxx）
        public string ShaderName;        // 目标真实着色器名称
        public int MaterialIndex;        // 材质在渲染组件中的索引（-1表示单材质）
    }

    public class MockSystem
    {
        private static readonly List<MockPrefabInfo> m_MockPrefabInfoList = new List<MockPrefabInfo>();
        private static readonly List<MockShaderInfo> m_MockShaderInfoList = new List<MockShaderInfo>(); // 新增：着色器替换列表

        /// <summary>
        /// 需求组件的列表信息
        /// </summary>
        private static readonly List<Component> ComponentsList = new List<Component>();

        /// <summary>
        /// 启用 MockSystem（新增着色器替换流程）
        /// </summary>
        public static void StartMockReplacement()
        {
            //if (!ZNetScene.instance || !ObjectDB.instance || CatModData.模拟物品_字典.Count == 0)
            //{
            //    return;
            //}

            Debug.LogError($"[CatMockSystem] 开始执行 mock ");
            // 清理原有列表和新增的着色器列表
            m_MockPrefabInfoList.Clear();
            m_MockShaderInfoList.Clear();

            CollectMockPrefab();       // 原有：收集预制件信息
            CollectMockShaders();      // 新增：收集着色器信息
            ReplacePlaceholders();     // 原有：替换预制件
            ReplaceMockShaders();      // 新增：替换着色器

            Debug.LogError($"[CatMockSystem] 替换流程完成，处理预制件 {m_MockPrefabInfoList.Count} 个，处理着色器 {m_MockShaderInfoList.Count} 个");
            Cleanup();
        }

        /// <summary>
        /// 【阶段一】<br></br>
        /// 注：收集 需要mock的预制件信息（原有逻辑不变）
        /// </summary>
        private static void CollectMockPrefab()
        {
            foreach (var item in CatModData.模拟物品_字典)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(item.Key);
                if (!prefab) continue;
                if (prefab.name == item.Value)
                {
                    CollectComponents(prefab);
                }
            }
        }

        /// <summary>
        ///【阶段一】<br></br>
        /// 注：收集所有的 预制件组件（扩展渲染组件用于着色器检测）
        /// </summary>
        private static void CollectComponents(GameObject prefab)
        {
            List<Component> componentList = new List<Component>();
            // 原有组件收集
            componentList.AddRange(prefab.GetComponentsInChildren<ItemDrop>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<Piece>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<WearNTear>(true));

            componentList.AddRange(prefab.GetComponentsInChildren<Plant>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<Destructible>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<Pickable>(true));

            componentList.AddRange(prefab.GetComponentsInChildren<Humanoid>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<CharacterDrop>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<MonsterAI>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<AnimalAI>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<FootStep>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<Tameable>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<Procreation>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<Growup>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<Ragdoll>(true));

            // 新增：收集渲染组件（用于着色器检测）
            componentList.AddRange(prefab.GetComponentsInChildren<Renderer>(true));

            Debug.Log($"[CatMockSystem] 处理预制件 {prefab.name} 及其子对象的组件（共 {componentList.Count} 个，含渲染组件）");

            foreach (Component component in componentList)
            {
                if (!component) continue;
                CheckComponentFields(component);

                // 新增：检查渲染组件中的着色器
                CheckRendererShaders(component);
            }
        }

        /// <summary>
        /// 新增：检查渲染组件中的材质和着色器，收集JVLmock_着色器信息
        /// </summary>
        private static void CheckRendererShaders(Component component)
        {
            // 处理普通渲染器（MeshRenderer/SkinnedMeshRenderer等）
            if (component is Renderer renderer)
            {
                CheckRendererMaterials(renderer);
            }

        }

        /// <summary>
        /// 新增：检查普通渲染器的材质（多材质支持）
        /// </summary>
        private static void CheckRendererMaterials(Renderer renderer)
        {
            if (!renderer || renderer.sharedMaterials == null) return;

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material mat = renderer.sharedMaterials[i];
                if (!mat || !mat.shader) continue;

                // 检测JVLmock_前缀的着色器
                if (mat.shader.name.StartsWith("JVLmock_"))
                {
                    AddMockShaderInfo(renderer, mat, i);
                }
            }
        }



        /// <summary>
        /// 新增：将JVLmock_着色器信息添加到替换列表
        /// </summary>
        private static void AddMockShaderInfo(Component rootComponent, Material material, int matIndex)
        {
            string realShaderName = material.shader.name.Substring("JVLmock_".Length);
            m_MockShaderInfoList.Add(new MockShaderInfo
            {
                RootComponent = rootComponent,
                TargetMaterial = material,
                MockShader = material.shader,
                ShaderName = realShaderName,
                MaterialIndex = matIndex
            });
            //Debug.Log($"[CatMockSystem] 发现占位着色器：{material.shader.name} → 目标：{realShaderName}（材质：{material.name}）");
        }

        /// <summary>
        /// 新增：【阶段一】收集所有需要替换的着色器信息
        /// </summary>
        private static void CollectMockShaders()
        {
            // 遍历模拟物品关联的预制件，补充着色器收集（确保全覆盖）
            foreach (var item in CatModData.模拟物品_字典)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(item.Key);
                if (prefab && prefab.name == item.Value)
                {
                    // 二次检查预制件中的渲染组件（避免遗漏）
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers)
                    {
                        CheckRendererShaders(renderer);
                    }
                }
            }
        }

        /// <summary>
        /// 【阶段二】<br></br>
        /// 注：检查组件的字段（根层级）（原有逻辑不变）
        /// </summary>
        private static void CheckComponentFields(Component component)
        {
            Type compType = component.GetType();
            FieldInfo[] fields = compType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                try
                {
                    object fieldValue = field.GetValue(component);
                    if (fieldValue == null) continue;

                    // 处理直接的GameObject引用
                    if (fieldValue is GameObject prefab)
                    {
                        CheckAndAddPlaceholder(component, field, prefab, component, -1);
                    }
                    // 处理Component引用
                    else if (fieldValue is Component componentValue)
                    {
                        CheckAndAddPlaceholder(component, field, componentValue.gameObject, component, -1);
                    }
                    // 处理集合类型
                    else if (fieldValue is IEnumerable enumerable)
                    {
                        HandleEnumerable(component, field, enumerable, component);
                    }
                    // 处理自定义对象（递归检查）
                    else if (fieldValue.GetType().IsClass)
                    {
                        CheckObjectFields(component, field, fieldValue, component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CatMockSystem] 处理字段 {field.Name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 【阶段二】<br></br>
        /// 注：递归检查自定义对象的内部字段（原有逻辑不变）
        /// </summary>
        private static void CheckObjectFields(Component rootComponent, FieldInfo parentField, object obj, object parentObject)
        {
            if (obj == null) return;

            Type objType = obj.GetType();
            if (objType.IsPrimitive || objType == typeof(string) || objType == typeof(object)) return;

            FieldInfo[] fields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                try
                {
                    object fieldValue = field.GetValue(obj);
                    if (fieldValue == null) continue;

                    if (fieldValue is GameObject go)
                    {
                        CheckAndAddPlaceholder(rootComponent, field, go, obj, -1);
                    }
                    else if (fieldValue is Component comp)
                    {
                        CheckAndAddPlaceholder(rootComponent, field, comp.gameObject, obj, -1);
                    }
                    else if (fieldValue is IEnumerable enumerable)
                    {
                        HandleEnumerable(rootComponent, field, enumerable, obj);
                    }
                    else if (fieldValue.GetType().IsClass)
                    {
                        CheckObjectFields(rootComponent, field, fieldValue, obj);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CatMockSystem] 处理对象 {objType.Name} 的字段 {field.Name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 注：收集占位预制件信息（原有逻辑不变）
        /// </summary>
        private static void CheckAndAddPlaceholder(Component rootComponent, FieldInfo targetField, GameObject prefab, object parentObject, int arrayIndex)
        {
            if (!prefab || !prefab.name.StartsWith("JVLmock_")) return;

            Type parentType = parentObject.GetType();
            if (!targetField.DeclaringType.IsAssignableFrom(parentType))
            {
                Debug.LogWarning($"[CatMockSystem]：字段：【{targetField.Name}】不属于对象 类型：【{parentType.Name}】 跳过");
                return;
            }

            string oPrefabName = prefab.name.Substring("JVLmock_".Length);
            m_MockPrefabInfoList.Add(new MockPrefabInfo
            {
                RootComponent = rootComponent,
                TargetField = targetField,
                mockPrefab = prefab,
                prefabName = oPrefabName,
                ParentObject = parentObject,
                ArrayIndex = arrayIndex
            });

            //Debug.Log($"[CatMockSystem] 发现占位预制件：{prefab.name} → 目标：{oPrefabName}（字段：{targetField.Name}）");
        }

        /// <summary>
        /// 处理列表类型中的元素（原有逻辑不变）
        /// </summary>
        private static void HandleEnumerable(Component rootComponent, FieldInfo rootField, IEnumerable enumerable, object parentObject)
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
                    CheckItemInCollection(rootComponent, rootField, item, parentObject, index);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CatMockSystem] 处理列表元素（索引：{index}）时出错：{ex.Message}");
                }
                index++;
            }
        }

        /// <summary>
        /// 注：辅助方法 检测集合内的 预制件字段（原有逻辑不变）
        /// </summary>
        private static void CheckItemInCollection(Component rootComponent, FieldInfo rootField, object item, object parentObject, int index)
        {
            if (item == null) return;

            if (item is GameObject go)
            {
                CheckAndAddPlaceholder(rootComponent, rootField, go, parentObject, index);
            }
            else if (item is Component comp)
            {
                CheckAndAddPlaceholder(rootComponent, rootField, comp.gameObject, parentObject, index);
            }
            else if (item is IEnumerable nestedEnumerable)
            {
                HandleEnumerable(rootComponent, rootField, nestedEnumerable, parentObject);
            }
            else if (item.GetType().IsClass)
            {
                CheckObjectFields(rootComponent, rootField, item, parentObject);
            }
        }

        /// <summary>
        /// 注：替换占位预制件（原有逻辑不变）
        /// </summary>
        private static void ReplacePlaceholders()
        {
            foreach (var info in m_MockPrefabInfoList)
            {
                if (!CatModData.m_PrefabCache.TryGetValue(info.prefabName, out GameObject realPrefab))
                {
                    realPrefab = CatToolManager.GetGameObject(info.prefabName);
                    if (realPrefab == null) continue;
                }

                try
                {
                    ReplaceFieldValue(info, realPrefab);
                    //Debug.Log($"[CatMockSystem] 替换完成：{info.mockPrefab.name} → {realPrefab.name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CatMockSystem] 替换 {info.mockPrefab.name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 替换字段值（原有逻辑不变）
        /// </summary>
        private static void ReplaceFieldValue(MockPrefabInfo info, GameObject realPrefab)
        {
            if (info.ParentObject == null || info.TargetField == null)
            {
                Debug.LogError("[CatMockSystem] 替换失败：ParentObject或TargetField为null");
                return;
            }

            Type targetType = info.ParentObject.GetType();
            if (!info.TargetField.DeclaringType.IsAssignableFrom(targetType))
            {
                Debug.LogError($"[CatMockSystem] 字段 {info.TargetField.Name} 不属于对象类型 {targetType.Name}");
                return;
            }

            object fieldValue = info.TargetField.GetValue(info.ParentObject);
            if (fieldValue == null) return;

            if (info.ArrayIndex != -1)
            {
                if (fieldValue is Array array && info.ArrayIndex >= 0 && info.ArrayIndex < array.Length)
                {
                    array.SetValue(realPrefab, info.ArrayIndex);
                }
                else if (fieldValue is IList list && info.ArrayIndex >= 0 && info.ArrayIndex < list.Count)
                {
                    list[info.ArrayIndex] = realPrefab;
                }
            }
            else
            {
                if (fieldValue is GameObject || fieldValue is Component)
                {
                    info.TargetField.SetValue(info.ParentObject, realPrefab);
                }
            }
        }

        /// <summary>
        /// 新增：【阶段二】替换所有JVLmock_着色器为真实着色器
        /// </summary>
        private static void ReplaceMockShaders()
        {
            foreach (var info in m_MockShaderInfoList)
            {
                // 优先从缓存获取真实着色器，未命中则通过工具类获取
                if (!CatModData.m_haderCache.TryGetValue(info.ShaderName, out Shader realShader) || realShader == null)
                {
                    Debug.LogError($"[CatMockSystem] 未找到真实着色器：{info.ShaderName}（跳过）");
                    continue;
                }

                try
                {
                    // 替换材质中的着色器
                    info.TargetMaterial.shader = realShader;
                    //Debug.Log($"[CatMockSystem] 着色器替换完成：{info.MockShader.name} → {realShader.name}（材质：{info.TargetMaterial.name}）");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CatMockSystem] 替换着色器 {info.MockShader.name} 失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清理方法（扩展着色器列表清理）
        /// </summary>
        private static void Cleanup()
        {
            m_MockPrefabInfoList.Clear();
            m_MockShaderInfoList.Clear(); // 新增：清理着色器列表
        }
    }
}