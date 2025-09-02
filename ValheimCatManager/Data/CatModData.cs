using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    }
}
