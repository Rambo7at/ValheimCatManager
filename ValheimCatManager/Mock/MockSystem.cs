using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;
using 准备开饭小子;

namespace ValheimCatManager.Mock
{
    /// <summary>
    /// 模拟预制件信息（用于存储占位预制件及其替换目标的关联数据）
    /// </summary>
    public struct MockPrefabInfo
    {
        public Component RootComponent;  // 根组件（关联的原始Component）
        public FieldInfo TargetField;    // 实际存储引用的字段（所属类型需与ParentObject一致）
        public GameObject mockPrefab;    // 占位预制件（名称以JVLmock_为前缀）
        public string prefabName; // 目标真实预制件的名称（去除JVLmock_前缀后的值）
        public object ParentObject;      // 字段所属的对象（可能是嵌套对象）
        public int ArrayIndex;           // 数组/列表中的索引（-1表示非数组元素）
    }

    /// <summary>
    /// 新增：模拟着色器信息（用于存储JVLmock_前缀占位着色器及其替换目标的关联数据）
    /// </summary>
    public struct MockShaderInfo
    {
        public Component RootComponent;  // 关联的根组件（通常是渲染组件）
        public Material TargetMaterial;  // 包含占位着色器的材质
        public Shader MockShader;        // 占位着色器（名称以JVLmock_为前缀）
        public string ShaderName;        // 目标真实着色器的名称（去除JVLmock_前缀后的值）
        public int MaterialIndex;        // 材质在渲染组件中的索引（-1表示单材质）
    }

    /// <summary>
    /// 模拟系统（用于替换占位预制件和占位着色器为真实资源）
    /// </summary>
    public class MockSystem
    {
        /// <summary>
        /// 注：默认预制件字典
        /// </summary>
        public readonly Dictionary<int, string> mockPrefabDict = new Dictionary<int, string>();

        // 存储需要替换的预制件信息列表
        private readonly List<MockPrefabInfo> m_MockPrefabInfoList = new List<MockPrefabInfo>();
        // 新增：存储需要替换的着色器信息列表
        private readonly List<MockShaderInfo> m_MockShaderInfoList = new List<MockShaderInfo>();

        private static MockSystem _instance;

        public static MockSystem Instance
        {
            get 
            { 
               if (_instance == null) { _instance = new MockSystem(); }
               return _instance;
            }
        }

        private MockSystem()
        {
            new Harmony("MockSystem").PatchAll(typeof(MockSystemPatch));
        }


        private class MockSystemPatch()
        {
            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPostfix, HarmonyPriority(Priority.First)]
            static void mockStart(ObjectDB __instance)
            {
                // 仅在主场景（"main"）执行
                if (SceneManager.GetActiveScene().name == "main")
                {
                    var startTime1 = DateTime.Now; // 记录开始时间（用于统计耗时）

                    Instance.StartMockReplacement(); // 执行占位预制件/着色器替换
                    CatModData.m_PrefabCache.Clear(); // 清理预制件缓存，释放资源

                    var elapsed1 = DateTime.Now - startTime1; // 计算耗时
                    Debug.LogError($"mock 完成耗时: {elapsed1.TotalMilliseconds / 1000}秒"); // 打印耗时日志
                }
            }

        }



        /// <summary>
        /// 需求组件的列表信息（用于收集需处理的组件）
        /// </summary>
        private  readonly List<Component> ComponentsList = new List<Component>();

        /// <summary>
        /// 启用 MockSystem（执行预制件和着色器的替换流程）
        /// 流程：清理原有数据 → 收集预制件信息 → 收集着色器信息 → 替换预制件 → 替换着色器 → 最终清理
        /// </summary>
        private void StartMockReplacement()
        {
            Debug.LogError($"[{CatManagerPlugin.PluginName}] 开始执行 mock ");
            // 清理原有列表（包括新增的着色器列表）
            m_MockPrefabInfoList.Clear();
            m_MockShaderInfoList.Clear();

            CollectMockPrefab();       // 原有：收集预制件信息
            CollectMockShaders();      // 新增：收集着色器信息
            ReplacePlaceholders();     // 原有：替换预制件
            ReplaceMockShaders();      // 新增：替换着色器

            Debug.LogError($"[{CatManagerPlugin.PluginName}] 替换流程完成，处理预制件 {m_MockPrefabInfoList.Count} 个，处理着色器 {m_MockShaderInfoList.Count} 个");
            Cleanup();
        }

        /// <summary>
        /// 【阶段一】<br></br>
        /// 注：收集需要mock的预制件信息（遍历模拟物品字典，处理关联预制件）
        /// </summary>
        private  void CollectMockPrefab()
        {
            foreach (var item in Instance.mockPrefabDict)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(item.Key);
                if (!prefab) continue;
                // 匹配预制件名称时，收集其组件信息
                if (prefab.name == item.Value)
                {
                    CollectComponents(prefab);
                }
            }
        }

        /// <summary>
        ///【阶段一】<br></br>
        /// 注：收集预制件及其子对象的所有相关组件（含原有功能组件和新增渲染组件）
        /// 原有：收集物品、建筑、实体等功能组件
        /// 新增：收集渲染组件（用于检测着色器）
        /// </summary>
        private  void CollectComponents(GameObject prefab)
        {
            List<Component> componentList = new List<Component>();
            // 原有组件收集（功能相关）
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

            componentList.AddRange(prefab.GetComponentsInChildren<SpawnAbility>(true));
            componentList.AddRange(prefab.GetComponentsInChildren<Projectile>(true));

            // 新增：收集渲染组件（用于检测和处理着色器）
            componentList.AddRange(prefab.GetComponentsInChildren<Renderer>(true));

            //Debug.Log($"[{CatManagerPlugin.PluginName}] 处理预制件 {prefab.name} 及其子对象的组件（共 {componentList.Count} 个，含渲染组件）");

            foreach (Component component in componentList)
            {
                if (!component) continue;
                CheckComponentFields(component); // 检查组件字段（预制件替换）

                // 新增：检查渲染组件中的着色器（着色器替换）
                CheckRendererShaders(component);
            }
        }

        /// <summary>
        /// 新增：检查渲染组件中的材质和着色器，收集JVLmock_前缀的占位着色器信息
        /// </summary>
        private  void CheckRendererShaders(Component component)
        {
            // 处理普通渲染器（如MeshRenderer、SkinnedMeshRenderer等）
            if (component is Renderer renderer)
            {
                CheckRendererMaterials(renderer);
            }
        }

        /// <summary>
        /// 新增：检查普通渲染器的材质列表，检测并收集含占位着色器的材质信息（支持多材质）
        /// </summary>
        private  void CheckRendererMaterials(Renderer renderer)
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
                    AddMockShaderInfo(renderer, mat, i);
                }
            }
        }

        /// <summary>
        /// 新增：将JVLmock_占位着色器的信息添加到替换列表（解析真实着色器名称并存储关联数据）
        /// </summary>
        private  void AddMockShaderInfo(Component rootComponent, Material material, int matIndex)
        {
            // 去除JVLmock_前缀，获取真实着色器名称
            string realShaderName = material.shader.name.Substring("JVLmock_".Length);
            m_MockShaderInfoList.Add(new MockShaderInfo
            {
                RootComponent = rootComponent,
                TargetMaterial = material,
                MockShader = material.shader,
                ShaderName = realShaderName,
                MaterialIndex = matIndex
            });
        }

        /// <summary>
        /// 新增：【阶段一】补充收集所有需要替换的着色器信息（遍历模拟物品关联的预制件，二次检查渲染组件）
        /// </summary>
        private  void CollectMockShaders()
        {
            foreach (var item in Instance.mockPrefabDict)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(item.Key);
                if (prefab && prefab.name == item.Value)
                {
                    // 二次检查预制件中的渲染组件，避免遗漏着色器
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
        /// 注：检查组件的字段（根层级），收集需要替换的占位预制件信息（处理直接引用、集合、自定义对象等）
        /// </summary>
        private  void CheckComponentFields(Component component)
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
                    // 处理Component引用（取其GameObject）
                    else if (fieldValue is Component componentValue)
                    {
                        CheckAndAddPlaceholder(component, field, componentValue.gameObject, component, -1);
                    }
                    // 处理集合类型（遍历元素）
                    else if (fieldValue is IEnumerable enumerable)
                    {
                        HandleEnumerable(component, field, enumerable, component);
                    }
                    // 处理自定义对象（递归检查内部字段）
                    else if (fieldValue.GetType().IsClass)
                    {
                        CheckObjectFields(component, field, fieldValue, component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{CatManagerPlugin.PluginName}] 处理字段 {field.Name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 【阶段二】<br></br>
        /// 注：递归检查自定义对象的内部字段，收集嵌套结构中的占位预制件信息
        /// </summary>
        private  void CheckObjectFields(Component rootComponent, FieldInfo parentField, object obj, object parentObject)
        {
            if (obj == null) return;

            Type objType = obj.GetType();
            // 跳过基础类型（避免无意义检查）
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
                    Debug.LogError($"[{CatManagerPlugin.PluginName}] 处理对象 {objType.Name} 的字段 {field.Name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 注：收集占位预制件信息（仅处理名称以JVLmock_为前缀的预制件，解析目标名称并存储关联数据）
        /// </summary>
        private  void CheckAndAddPlaceholder(Component rootComponent, FieldInfo targetField, GameObject prefab, object parentObject, int arrayIndex)
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
            m_MockPrefabInfoList.Add(new MockPrefabInfo
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
        /// 处理集合类型（数组、列表等）中的元素，检查并收集占位预制件信息
        /// </summary>
        private  void HandleEnumerable(Component rootComponent, FieldInfo rootField, IEnumerable enumerable, object parentObject)
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
                    Debug.LogError($"[{CatManagerPlugin.PluginName}] 处理列表元素（索引：{index}）时出错：{ex.Message}");
                }
                index++;
            }
        }

        /// <summary>
        /// 注：辅助方法 检测集合内的预制件字段（处理集合中的GameObject、Component、嵌套集合或自定义对象）
        /// </summary>
        private  void CheckItemInCollection(Component rootComponent, FieldInfo rootField, object item, object parentObject, int index)
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
        /// 注：替换占位预制件为真实预制件（从缓存或工具类获取真实预制件，更新字段引用）
        /// </summary>
        private  void ReplacePlaceholders()
        {
            foreach (var info in m_MockPrefabInfoList)
            {
                // 优先从缓存获取真实预制件，未命中则通过工具类获取
                if (!CatModData.m_PrefabCache.TryGetValue(info.prefabName, out GameObject realPrefab))
                {
                    realPrefab = CatToolManager.GetGameObject(info.prefabName);
                    if (realPrefab == null) continue;
                }

                try
                {
                    ReplaceFieldValue(info, realPrefab);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{CatManagerPlugin.PluginName}] 替换 {info.mockPrefab.name} 时出错：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 替换字段值（根据字段类型和索引，更新占位预制件引用为真实预制件）
        /// </summary>
        private  void ReplaceFieldValue(MockPrefabInfo info, GameObject realPrefab)
        {
            if (info.ParentObject == null || info.TargetField == null)
            {
                Debug.LogError($"[{CatManagerPlugin.PluginName}] 替换失败：ParentObject或TargetField为null");
                return;
            }

            Type targetType = info.ParentObject.GetType();
            // 验证字段所属关系，避免类型不匹配
            if (!info.TargetField.DeclaringType.IsAssignableFrom(targetType))
            {
                Debug.LogError($"[{CatManagerPlugin.PluginName}] 字段 {info.TargetField.Name} 不属于对象类型 {targetType.Name}");
                return;
            }

            object fieldValue = info.TargetField.GetValue(info.ParentObject);
            if (fieldValue == null) return;

            // 处理数组/列表元素
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
            // 处理直接引用
            else
            {
                if (fieldValue is GameObject || fieldValue is Component)
                {
                    info.TargetField.SetValue(info.ParentObject, realPrefab);
                }
            }
        }

        /// <summary>
        /// 新增：【阶段二】替换所有JVLmock_占位着色器为真实着色器（从缓存获取真实着色器，更新材质引用）
        /// </summary>
        private  void ReplaceMockShaders()
        {
            foreach (var info in m_MockShaderInfoList)
            {
                var shader =  CatToolManager.GetShader(info.ShaderName);
                if (shader == null)
                {
                    Debug.LogError($"[{CatManagerPlugin.PluginName}] 执行ReplaceMockShaders时 找不到着色器：{info.ShaderName}");
                }

                try
                {
                    // 替换材质中的着色器为真实着色器
                    info.TargetMaterial.shader = shader;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{CatManagerPlugin.PluginName}] 替换着色器 {info.MockShader.name} 失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清理方法（清空预制件和着色器的替换列表，释放资源）
        /// </summary>
        private  void Cleanup()
        {
            m_MockPrefabInfoList.Clear();
            m_MockShaderInfoList.Clear(); // 新增：清理着色器列表
        }
    }
}