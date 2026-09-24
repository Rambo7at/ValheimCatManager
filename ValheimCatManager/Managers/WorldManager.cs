using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager.CatUtils;

namespace ValheimCatManager.Managers;

public class WorldManager
{
    /// <summary>注：一个运行时注册的自定义地区，身份位借用官方地形皮囊在世界中成形</summary>
    public class CustomBiome
    {
        /// <summary>注：注册时自动分配的 Flags 身份位，也是规则判定的返回值</summary>
        public Heightmap.Biome Biome;
        /// <summary>注：地区注册 id（小写英文），Enum.GetName/BiomeToString/配置 key 用它</summary>
        public string Name = "";
        /// <summary>注：地区显示名（如"死寂沼泽"），写入 Localization 供 UI 显示</summary>
        public string DisplayName = "";
        /// <summary>注：地形皮囊，该地区在地形高度/BiomeIndex/材质层表现为这个官方群系</summary>
        public Heightmap.Biome Terrain;
        /// <summary>注：大地图临时显示色，正式配色以后交给 biome 管理器</summary>
        public Color MapColor;
    }

    /// <summary>注：单群系分布规则（复现官方 GetBiome 的判定条件）</summary>
    public class BiomeRule
    {
        public Heightmap.Biome biome;       // 目标群系
        public float minDistance;            // 距离下限（米，和官方一致）
        public float maxDistance;            // 距离上限（米）
        public float? minHeight;             // 高度下限（null=不限制）
        public float? maxHeight;             // 高度上限（null=不限制）
        public int perlinOffset = -1;       // 用哪个噪声图：0=m_offset0, 1=m_offset1, 2=m_offset2, 4=m_offset4；-1=不用噪声
        public float? perlinMin;            // 噪声下限（noise 需 >= 它才命中），null=不限下限；单边门槛只填它
        public float? perlinMax;            // 噪声上限（noise 需 <= 它才命中），null=不限上限
        public bool useAngleOffset;          // 距离下限是否加官方角度锯齿波动（num > min+num2）
        public float? angleCenter;          // 方位扇区中心（罗盘度：北0 东90 南180 西270），null=不限方位=整环
        public float? angleWidth;           // 方位扇区总张角（度），越小越窄；与 angleCenter 成对使用
    }

    /// <summary>注：单例，构造时注册本管理器全部 Harmony 补丁</summary>
    private static WorldManager _instance;
    public static WorldManager Instance => _instance ?? (_instance = new WorldManager());

    /// <summary>注：群系规则字典，key=layer（间隔100方便中间插值），小先判</summary>
    public readonly SortedDictionary<int, BiomeRule> biomeRules = new();

    /// <summary>注：当前世界是否是"我们介入生成的新世界"。旧世界=false，新世界第一次生成时=true</summary>
    private bool useCustomRules = false;

    /// <summary>注：所有运行时注册的自定义地区，按身份位索引；各补丁统一查这里</summary>
    public readonly Dictionary<Heightmap.Biome, CustomBiome> customBiomes = [];

    /// <summary>注：下一个可分配身份位的游标，从官方最高位 Mistlands(0x200) 起向上翻倍</summary>
    private Heightmap.Biome nextBiomeBit = Heightmap.Biome.Mistlands;

    private WorldManager()
    {
        new Harmony("CatWorldManager").PatchAll(typeof(WorldManagerPatch));
        InitDefaultRules();
    }

    /// <summary>注：注册自定义地区</summary>
    public Heightmap.Biome RegisterBiome(string name, Heightmap.Biome terrain, Color mapColor, string displayName = null)
    {
        nextBiomeBit = NextBiomeBit(nextBiomeBit);
        CustomBiome info = new CustomBiome
        {
            Biome = nextBiomeBit,
            Name = name,
            DisplayName = displayName,
            Terrain = terrain,
            MapColor = mapColor
        };
        customBiomes[nextBiomeBit] = info;
        Debug.Log($"[WorldManager.RegisterBiome] 新增地区 {name}({info.DisplayName}) = {nextBiomeBit}（地形皮囊 {terrain}）");
        return nextBiomeBit;
    }

    /// <summary>注：修改现有 layer 的群系规则。layer 不存在则新增；不传的参数保持原值。噪声用双边区间，方位用罗盘扇区，null 侧不限</summary>
    public void ModifyRule(int layer, Heightmap.Biome? biome = null, float? minDistance = null, float? maxDistance = null, float? minHeight = null, float? maxHeight = null, int? perlinOffset = null, float? perlinMin = null, float? perlinMax = null, bool? useAngleOffset = null, float? angleCenter = null, float? angleWidth = null)
    {
        if (!biomeRules.TryGetValue(layer, out var rule))
        {
            rule = new BiomeRule();
            biomeRules[layer] = rule;
        }
        if (biome.HasValue) rule.biome = biome.Value;
        if (minDistance.HasValue) rule.minDistance = minDistance.Value;
        if (maxDistance.HasValue) rule.maxDistance = maxDistance.Value;
        if (minHeight.HasValue) rule.minHeight = minHeight.Value;
        if (maxHeight.HasValue) rule.maxHeight = maxHeight.Value;
        if (perlinOffset.HasValue) rule.perlinOffset = perlinOffset.Value;
        if (perlinMin.HasValue) rule.perlinMin = perlinMin.Value;
        if (perlinMax.HasValue) rule.perlinMax = perlinMax.Value;
        if (useAngleOffset.HasValue) rule.useAngleOffset = useAngleOffset.Value;
        if (angleCenter.HasValue) rule.angleCenter = angleCenter.Value;
        if (angleWidth.HasValue) rule.angleWidth = angleWidth.Value;
        Debug.Log($"[WorldManager.ModifyRule] layer={layer} biome={rule.biome} dist={rule.minDistance}~{rule.maxDistance} height={rule.minHeight}~{rule.maxHeight} perlin=offset{rule.perlinOffset}[{rule.perlinMin}~{rule.perlinMax}] sector={rule.angleCenter}±{(rule.angleWidth.HasValue ? rule.angleWidth.Value * 0.5f : 0f)} angle={rule.useAngleOffset}");
    }

    /// <summary>注：按官方 GetBiome 判定顺序初始化规则（layer 间隔100），基线与官方完全一致</summary>
    private void InitDefaultRules()
    {
        biomeRules[500] = new() { biome = Heightmap.Biome.Swamp, minDistance = 2000f, maxDistance = 6000f, minHeight = 0.05f, maxHeight = 0.25f, perlinOffset = 0, perlinMin = 0.6f };
        biomeRules[600] = new() { biome = Heightmap.Biome.Mountain, minDistance = 0f, maxDistance = 99999f, minHeight = 0.4f };
        biomeRules[700] = new() { biome = Heightmap.Biome.Mistlands, minDistance = 6000f, maxDistance = 10000f, perlinOffset = 4, perlinMin = 0.4f, useAngleOffset = true };
        biomeRules[800] = new() { biome = Heightmap.Biome.Plains, minDistance = 3000f, maxDistance = 8000f, perlinOffset = 1, perlinMin = 0.4f, useAngleOffset = true };
        biomeRules[900] = new() { biome = Heightmap.Biome.BlackForest, minDistance = 600f, maxDistance = 6000f, perlinOffset = 2, perlinMin = 0.4f, useAngleOffset = true };
        biomeRules[950] = new() { biome = Heightmap.Biome.BlackForest, minDistance = 5000f, maxDistance = 99999f, useAngleOffset = true };
    }

    /// <summary>注：取当前身份位的下一个 Flags 位；从 0x200 上移不经过官方保留空位 0x80，到最高位报错</summary>
    private static Heightmap.Biome NextBiomeBit(Heightmap.Biome current)
    {
        uint next = (uint)current << 1;
        if (next == 0u || next >= 0x80000000u)
        {
            throw new Exception("[WorldManager.NextBiomeBit] 自定义地区数量已达上限");
        }
        return (Heightmap.Biome)next;
    }

    /// <summary>注：自定义身份位降级成它的地形皮囊，官方群系原样返回</summary>
    private Heightmap.Biome GetTerrain(Heightmap.Biome biome)
    {
        return customBiomes.TryGetValue(biome, out CustomBiome info) ? info.Terrain : biome;
    }

    /// <summary>注：取自定义地区的大地图显示色；非自定义地区返回 false 走官方</summary>
    private bool TryGetMapColor(Heightmap.Biome biome, out Color color)
    {
        if (customBiomes.TryGetValue(biome, out CustomBiome info))
        {
            color = info.MapColor;
            return true;
        }
        color = default;
        return false;
    }

    /// <summary>注：罗盘方位角（度）：北0 东90 南180 西270，顺时针。用官方 atan2(x,z) 原始角，不用振荡的 WorldAngle</summary>
    private static float GetBearing(float wx, float wy)
    {
        float deg = Mathf.Atan2(wx, wy) * Mathf.Rad2Deg;
        return (deg + 360f) % 360f;
    }

    /// <summary>注：按 layer 字典判定群系。传入 WorldGenerator 实例以访问 m_offset 和 GetBaseHeight</summary>
    public Heightmap.Biome? GetCustomBiome(WorldGenerator wg, float wx, float wy)
    {
        float dist = DUtils.Length(wx, wy);
        float waveAngle = (float)((double)WorldGenerator.WorldAngle(wx, wy) * 100.0);
        float baseHeight = wg.GetBaseHeight(wx, wy, menuTerrain: false);
        foreach (var kv in biomeRules)
        {
            var rule = kv.Value;
            // 距离下限（可选加官方角度锯齿波动，和官方 num > (min + num2) 一致）
            float effectiveMin = rule.useAngleOffset ? rule.minDistance + waveAngle : rule.minDistance;
            if (dist < effectiveMin || dist > rule.maxDistance) continue;
            // 方位扇区（null=整环）；用最短角距，天然兼容跨正北0度的扇区
            if (rule.angleCenter.HasValue)
            {
                float half = (rule.angleWidth ?? 360f) * 0.5f;
                float delta = Mathf.Abs(Mathf.DeltaAngle(GetBearing(wx, wy), rule.angleCenter.Value));
                if (delta > half) continue;
            }
            // 高度范围
            if (rule.minHeight.HasValue && baseHeight < rule.minHeight.Value) continue;
            if (rule.maxHeight.HasValue && baseHeight > rule.maxHeight.Value) continue;
            // Perlin 双边区间（perlinOffset<0 不看噪声；区间越宽覆盖越强）
            if (rule.perlinOffset >= 0)
            {
                float offset = GetOffset(wg, rule.perlinOffset);
                float noise = DUtils.PerlinNoise((offset + wx) * 0.001f, (offset + wy) * 0.001f);
                if (rule.perlinMin.HasValue && noise < rule.perlinMin.Value) continue;
                if (rule.perlinMax.HasValue && noise > rule.perlinMax.Value) continue;
            }
            return rule.biome;
        }
        return null; // 未命中 → 走官方兜底 Meadows
    }

    /// <summary>注：按索引取 m_offset（0/1/2/4 对应官方四个噪声偏移）</summary>
    private float GetOffset(WorldGenerator wg, int index)
    {
        switch (index)
        {
            case 0: return wg.m_offset0;
            case 1: return wg.m_offset1;
            case 2: return wg.m_offset2;
            case 4: return wg.m_offset4;
            default: return 0f;
        }
    }

    /// <summary>注：嵌套补丁类，集中挂所有补丁</summary>
    private class WorldManagerPatch
    {
        /// <summary>后置：自定义地区身份位返回注册 id；官方已识别的值和非自定义组合值走原版</summary>
        [HarmonyPatch(typeof(Enum), nameof(Enum.GetName)), HarmonyPostfix, HarmonyPriority(0)]
        static void Enum_GetName_Postfix(Type enumType, object value, ref string __result)
        {
            if (__result != null) return;
            if (enumType != typeof(Heightmap.Biome)) return;
            if (value is Heightmap.Biome b && Instance.customBiomes.TryGetValue(b, out CustomBiome info))
                __result = info.Name;
        }

        /// <summary>后置：自定义地区身份位视为已定义枚举值，兼容其他模组的 IsDefined 校验</summary>
        [HarmonyPatch(typeof(Enum), nameof(Enum.IsDefined)), HarmonyPostfix, HarmonyPriority(0)]
        static void Enum_IsDefined_Postfix(Type enumType, object value, ref bool __result)
        {
            if (__result) return;
            if (enumType != typeof(Heightmap.Biome)) return;
            if (value is Heightmap.Biome b && Instance.customBiomes.ContainsKey(b))
                __result = true;
        }

        /// <summary>前置：Enum.Parse(Heightmap.Biome, name) 时先查自定义地区注册表，命中直接返回；未命中走官方解析</summary>
        [HarmonyPatch(typeof(Enum), nameof(Enum.Parse), new Type[] { typeof(Type), typeof(string), typeof(bool) }), HarmonyPrefix, HarmonyPriority(0)]
        static bool Enum_Parse_Prefix(Type enumType, string value, bool ignoreCase, ref object __result)
        {
            if (enumType != typeof(Heightmap.Biome)) return true;
            foreach (var kv in Instance.customBiomes)
            {
                if (string.Equals(kv.Value.Name, value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    __result = kv.Key;
                    return false;
                }
            }
            return true;
        }

        /// <summary>前置：进世界时判断是不是新世界（无缓存）。旧世界有缓存，完全不介入</summary>
        [HarmonyPatch(typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.VerifyBiomeData)), HarmonyPrefix, HarmonyPriority(0)]
        static void AltBiomeWorldData_VerifyBiomeData_Prefix(World world)
        {
            bool hasCache = File.Exists(AltBiomeWorldData.GetFilePath(world));
            Instance.useCustomRules = !hasCache;
            Debug.Log($"[WorldManager.VerifyBiomeData] path={AltBiomeWorldData.GetFilePath(world)}");
            Debug.Log($"[WorldManager.VerifyBiomeData] 世界 {world.m_name} 缓存存在={hasCache}，启用自定义群系={Instance.useCustomRules}");
        }

        /// <summary>前置：修改官方的地形生成，灰烬与北方不修改</summary>
        [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), new Type[] { typeof(float), typeof(float), typeof(float), typeof(bool) }), HarmonyPrefix, HarmonyPriority(0)]
        static bool WorldGenerator_GetBiome_Prefix(WorldGenerator __instance, float wx, float wy, float oceanLevel, bool waterAlwaysOcean, ref Heightmap.Biome __result)
        {
            // 旧世界或菜单世界：完全不介入
            if (!Instance.useCustomRules || __instance.m_world.m_menu) return true;
            // 官方顺序 1：waterAlwaysOcean 海洋
            if (waterAlwaysOcean && __instance.GetHeight(wx, wy) <= oceanLevel) return true;
            // 官方顺序 2：AshLands（南极点，固定）
            if (WorldGenerator.IsAshlands(wx, wy)) { __result = Heightmap.Biome.AshLands; return false; }
            // 官方顺序 3：低于海平面海洋
            if (!waterAlwaysOcean && __instance.GetBaseHeight(wx, wy, menuTerrain: false) <= oceanLevel) return true;
            // 官方顺序 4：DeepNorth（北极点，固定）
            if (WorldGenerator.IsDeepnorth(wx, wy)) { __result = Heightmap.Biome.DeepNorth; return false; }
            // 官方顺序 5~11：按 layer 字典判定
            Heightmap.Biome? biome = Instance.GetCustomBiome(__instance, wx, wy);
            if (biome.HasValue) { __result = biome.Value; return false; }
            return true; // 兜底走官方 Meadows
        }

        /// <summary>前置：sector 存盘和四角权重混合前，把自定义身份位折叠成皮囊群系，避免落到固定10槽之外</summary>
        [HarmonyPatch(typeof(BiomeHelpers), nameof(BiomeHelpers.ToBiomeIndex)), HarmonyPrefix, HarmonyPriority(0)]
        static bool BiomeHelpers_ToBiomeIndex_Prefix(ref Heightmap.Biome b)
        {
            b = Instance.GetTerrain(b);
            return true;
        }

        /// <summary>前置：地形高度公式只认官方群系，计算前降级成皮囊，高度形状才正确</summary>
        [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight)), HarmonyPrefix, HarmonyPriority(0)]
        static bool WorldGenerator_GetBiomeHeight_Prefix(ref Heightmap.Biome biome)
        {
            biome = Instance.GetTerrain(biome);
            return true;
        }

        /// <summary>前置：自定义地区替换小地图颜色，官方地图走原有逻辑</summary>
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.GetPixelColor)), HarmonyPrefix, HarmonyPriority(0)]
        static bool Minimap_GetPixelColor_Prefix(Heightmap.Biome biome, ref Color __result)
        {
            if (Instance.TryGetMapColor(biome, out Color color))
            {
                __result = color;
                return false;
            }
            return true;
        }

        /// <summary>前置：自定义地区在区块查询时按实时判定精确返回身份（绕开折叠成皮囊的四角缓存，不做边界混合）；官方群系放回原版四角权重混合，基线不变</summary>
        [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetBiome), new Type[] { typeof(Vector3), typeof(float), typeof(bool) }), HarmonyPrefix, HarmonyPriority(0)]
        static bool Heightmap_GetBiome_Prefix(Heightmap __instance, Vector3 point, float oceanLevel, bool waterAlwaysOcean, ref Heightmap.Biome __result)
        {
            if (!Instance.useCustomRules) return true;
            Heightmap.Biome b = WorldGenerator.instance.GetBiome(point.x, point.z, oceanLevel, waterAlwaysOcean);
            if ((int)b >= 0x400)
            {
                __result = b;
                return false;
            }
            return true;
        }

        /// <summary>后置：让 Enum.GetValues(Heightmap.Biome) 包含所有已注册自定义地区，兼容其他模组遍历群系列表</summary>
        [HarmonyPatch(typeof(Enum), nameof(Enum.GetValues)), HarmonyPostfix, HarmonyPriority(0)]
        static void Enum_GetValues_Postfix(Type enumType, ref Array __result)
        {
            if (enumType != typeof(Heightmap.Biome)) return;
            if (Instance.customBiomes.Count == 0) return;
            Heightmap.Biome[] original = (Heightmap.Biome[])__result;
            Heightmap.Biome[] expanded = new Heightmap.Biome[original.Length + Instance.customBiomes.Count];
            Array.Copy(original, expanded, original.Length);
            int i = original.Length;
            foreach (var kv in Instance.customBiomes)
                expanded[i++] = kv.Key;
            __result = expanded;
        }

        /// <summary>前置：官方 BiomeToString switch 不含自定义地区，这里返回注册 id（用于环境/音乐/配置 key）</summary>
        [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.BiomeToString)), HarmonyPrefix, HarmonyPriority(0)]
        static bool Heightmap_BiomeToString_Prefix(Heightmap.Biome biome, ref string __result)
        {
            if (Instance.customBiomes.TryGetValue(biome, out CustomBiome info))
            {
                __result = info.Name;
                return false;
            }
            return true;
        }

        /// <summary>前置：重写官方 UpdateBiome。去掉 sector(Plains皮囊) 与实时身份(2048)不一致的警告；自定义地区时 m_currentBiome 用实时身份，环境/音乐才生效</summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateBiome)), HarmonyPrefix, HarmonyPriority(0)]
        static bool Player_UpdateBiome_Prefix(Player __instance, float dt)
        {
            if (!WorldManager.Instance.useCustomRules) return true;
            if (__instance.InIntro()) return false;
            // 官方逻辑：timer 为 0 时处理地点发现标签
            if (__instance.m_biomeTimer == 0f)
            {
                Location location = Location.GetLocation(__instance.transform.position, false);
                if (location != null && !string.IsNullOrEmpty(location.m_discoverLabel))
                {
                    __instance.AddKnownLocationName(location.m_discoverLabel);
                }
            }
            __instance.m_biomeTimer += dt;
            if (__instance.m_biomeTimer <= 1f) return false;
            __instance.m_biomeTimer = 0f;
            BiomeSector sector = WorldGenerator.instance.GetBiomeSector(__instance.transform.position, false);
            Heightmap.Biome realtime = WorldGenerator.instance.GetBiome(__instance.transform.position);
            // 去掉了官方的不一致警告（sector 存 Plains、实时 2048 是我们设计的预期行为）
            // m_currentBiome：自定义地区用实时身份 2048，官方群系用 sector 的
            Heightmap.Biome currentBiome = ((int)realtime >= 0x400) ? realtime : sector.BiomeType.Biome;
            if (__instance.m_currentBiomeData == sector) return false;
            __instance.m_currentBiome = currentBiome;
            __instance.m_currentBiomeData = sector;
            __instance.AddKnownBiome(sector);
            return false;
        }

        /// <summary>前置：官方 GetName 用 this.Biome（缓存 Plains 皮囊）生成名称会显示"平原"。这里用 this.Center 实时判定，自定义身份返回显示名，未配置细节显示"???"</summary>
        [HarmonyPatch(typeof(BiomeSector), nameof(BiomeSector.GetName)), HarmonyPrefix, HarmonyPriority(0)]
        static bool BiomeSector_GetName_Prefix(BiomeSector __instance, bool debug, ref string __result)
        {
            if (!WorldManager.Instance.useCustomRules) return true;
            Heightmap.Biome realtime = WorldGenerator.instance.GetBiome(__instance.Center.x, __instance.Center.y);
            if (Instance.customBiomes.TryGetValue(realtime, out var value))
            {
                __result = value.DisplayName;
                return false;
            }
            if ((int)realtime >= 0x400)
            {
                __result = "???";
                return false;
            }
            return true;
        }


        /// <summary>前置：按自定义身份(0x400/0x800)取采样点时先折叠成地形皮囊，避免 Biomes 空列表越界</summary>
        [HarmonyPatch(typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GetRandomPointByBiome)), HarmonyPrefix]
        static void AltBiomeWorldData_GetRandomPointByBiome_Prefix(ref Heightmap.Biome biome)
        {
            biome = Instance.GetTerrain(biome);
        }

        /// <summary>前置：同上，海平面以上取点路径</summary>
        [HarmonyPatch(typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GetRandomPointByBiomeAboveSeaLevel)), HarmonyPrefix]
        static void AltBiomeWorldData_GetRandomPointByBiomeAboveSeaLevel_Prefix(ref Heightmap.Biome biome)
        {
            biome = Instance.GetTerrain(biome);
        }

        /// <summary>前置：按自定义身份取 BiomeSector 时同样折叠成皮囊</summary>
        [HarmonyPatch(typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GetRandomSectorByBiome)), HarmonyPrefix]
        static void AltBiomeWorldData_GetRandomSectorByBiome_Prefix(ref Heightmap.Biome biome)
        {
            biome = Instance.GetTerrain(biome);
        }


        /// <summary>前置：PlaceVegetation用HaveBiome按chunk四角缓存预筛，缓存里是折叠皮囊(Plains/Swamp)，
        /// 对高位自定义身份(≥0x400)放行，交给后面逐点GetBiome精确判定，避免整条植被在该chunk被跳过</summary>
        [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.HaveBiome)), HarmonyPrefix, HarmonyPriority(0)]
        static bool Heightmap_HaveBiome_Prefix(Heightmap.Biome biome, ref bool __result)
        {
            if (!Instance.useCustomRules) return true;
            if ((int)biome >= 0x400)
            {
                __result = true;
                return false;
            }
            return true;
        }



    }
}



