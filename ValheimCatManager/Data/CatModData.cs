using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace ValheimCatManager.Data
{
    public static class CatModData
    {
        /// <summary>
        /// 预制件缓存（key：预制件名称）
        /// </summary>
        public static readonly Dictionary<string, GameObject> m_PrefabCache = new Dictionary<string, GameObject>();

        /// <summary>
        /// 着色器缓存（key：着色器名称）
        /// </summary>
        public static readonly Dictionary<string, Shader> m_shaderCache = new Dictionary<string, Shader>();

        /// <summary>
        /// 制作表缓存（key：制作表名称）
        /// </summary>
        public static readonly Dictionary<string, PieceTable> m_PieceTableCache = new Dictionary<string, PieceTable>();

        /// <summary>
        /// 材质缓存（key：材质名称）【新增】
        /// </summary>
        public static readonly Dictionary<string, Material> m_materialCache = new Dictionary<string, Material>();

        /// <summary>
        /// Mock对象信息类（存储替换所需的所有关键信息）
        /// </summary>
        public class MockObjectInfo
        {
            public Component RootComponent;  // 根组件（关联的原始Component）
            public FieldInfo TargetField;    // 实际存储引用的字段（所属类型需与ParentObject一致）
            public GameObject mockPrefab;    // 占位预制件（名称以JVLmock_为前缀）
            public string prefabName;        // 目标真实预制件的名称（去除JVLmock_前缀后的值）
            public object ParentObject;      // 字段所属的对象（可能是嵌套对象）
            public int ArrayIndex;           // 数组/列表中的索引（-1表示非数组元素）

            public Material TargetMaterial;  // 包含占位着色器/材质的目标材质
            public Shader MockShader;        // 占位着色器（名称以JVLmock_为前缀）
            public string ShaderName;        // 目标真实着色器的名称（去除JVLmock_前缀后的值）
            public int MaterialIndex;        // 材质在渲染组件中的索引（-1表示单材质）
            public string MaterialName;      // 目标真实材质的名称（去除JVLmock_前缀后的值）【新增】
        }
    }
}