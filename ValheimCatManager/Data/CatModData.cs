using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.Config;

namespace ValheimCatManager.Data
{
    class CatModData
    {

        public static readonly Dictionary<string, GameObject> m_PrefabCache = new Dictionary<string, GameObject>();

        public static readonly Dictionary<string, Shader> m_shaderCache = new Dictionary<string, Shader>();

        public static readonly Dictionary<string, PieceTable> m_PieceTableCache = new Dictionary<string, PieceTable>();


        /// <summary>
        /// 模拟预制件信息（用于存储占位预制件及其替换目标的关联数据）
        /// </summary>
        public class MockObjectInfo
        {
            public Component RootComponent;  // 根组件（关联的原始Component）
            public FieldInfo TargetField;    // 实际存储引用的字段（所属类型需与ParentObject一致）
            public GameObject mockPrefab;    // 占位预制件（名称以JVLmock_为前缀）
            public string prefabName; // 目标真实预制件的名称（去除JVLmock_前缀后的值）
            public object ParentObject;      // 字段所属的对象（可能是嵌套对象）
            public int ArrayIndex;           // 数组/列表中的索引（-1表示非数组元素）

            public Material TargetMaterial;  // 包含占位着色器的材质
            public Shader MockShader;        // 占位着色器（名称以JVLmock_为前缀）
            public string ShaderName;        // 目标真实着色器的名称（去除JVLmock_前缀后的值）
            public int MaterialIndex;        // 材质在渲染组件中的索引（-1表示单材质）
        }

    }
}
