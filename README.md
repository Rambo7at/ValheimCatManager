<div align="center">

# 🐱 ValheimCatManager

### 让 Valheim 模组开轻松开发

</div>

---

## 目录

- [项目简介](#项目简介)
- [核心特性](#核心特性)
- [环境要求](#环境要求)
- [快速上手](#快速上手)
- [API 一览](#api-一览)
- [配置类说明](#配置类说明)
- [自定义群系完整示例](#自定义群系完整示例)
- [Mock 系统](#mock-系统)
- [配置文件](#配置文件)
- [项目结构](#项目结构)
- [致谢](#致谢)

---

## 项目简介

**ValheimCatManager** 是一个面向 Valheim 中文模组开发者的资源管理框架（BepInEx 插件）。

它把"往游戏里加东西"这件事封装成了一套**全中文 API**——加物品、加建筑、加怪物、加植被、加配方、加地下城、加自定义群系……都只需要一两行代码。配置类的字段也是中文，鼠标悬停有注释，新手也能看懂。

- **中文 API**：类名、方法名、配置字段全中文，浅学 C# 就能上手
- **AssetBundle 资源加载**：从嵌入资源自动加载 AB 包，按调用方模组自动隔离
- **Mock 系统**：用 `JVLmock_` 前缀的占位预制件/着色器/材质，运行时自动替换为游戏真实资源，无需在 Unity 里硬引用游戏 dll
- **覆盖面广**：物品、装备、配方、建筑、怪物、生成、植被、杂物、烹饪站、炼制站、地点、地下城房间、自定义群系、天气环境、状态效果、动画、攻击速度、地图图标

---

## 核心特性

### 游戏内容

| 类别 | 说明 |
|------|------|
| 🏗️ 物品 / 装备 | 注册自定义物品，或一次性注册装备+制作配方 |
| 📜 配方 | 支持普通材料与升级专用材料（upgraderRes） |
| 🏠 建筑（Piece） | 完整的物件配置，含制作工具、目录、材料需求 |
| 🍱 食物 | 女巫版本可摆放食物，自动绑定 Feaster 制作工具 |
| 👹 怪物 | 自定义怪物预制件与食谱（掉落）配置 |
| 🌱 植被 | 智能植被生成，兼容 Epic Loot 等自定义群系 |
| 🌿 杂物（Clutter） | 草、碎石、地面雾气等地表装饰 |
| 👻 生成（Spawn） | 自定义生物生成规则，类 Spawn That 配置 |
| 🍳 烹饪站 | 自定义烹饪站配方 |
| 🔥 炼制站 | 熔炉、高炉、提炼器的输入输出转换 |
| 📍 地点（Location） | 自定义野外地点/建筑生成 |
| 🗺️ 地图图标 | 自定义地点的小地图图标 |
| 🏰 地下城 | 自定义地下城主题、房间、地牢入口 |
| 🌍 自定义群系 | 自定义 Biome 主题、天气环境、音乐 |
| ✨ 状态效果 | 自定义 Buff / Debuff |
| 🎬 动画 | 注册多段动画片段并分组 |
| ⚔️ 攻击速度 | 按物品修改主/次攻击速度（累加增量） |

### 开发工具

- **📦 AssetBundle 管理**：嵌入资源自动加载，按模组隔离缓存
- **🎭 Mock 系统**：占位预制件 / 着色器 / 材质运行时自动替换
- **📤 游戏数据导出**：进世界时导出原版怪物生成、植被、地点、天气、群系环境等配置文本，供制作参考
- **🔄 旧世界升级**：一键清理旧存档的植被/地点 ZDO，让新生成规则在旧世界生效（⚠️ 不可逆，务必备份）

---

## 环境要求

- **Valheim** 游戏本体
- **BepInEx 5.4.21+**（x64）
- **.NET Framework 4.8.1** 开发环境
- **Visual Studio 2022**（推荐）或 Rider
- 游戏程序集引用放在解决方案上级的 `Libs` 目录（见 csproj 的 HintPath）

> 本项目以 **类库（dll）** 形式编译，产物放入 `BepInEx/plugins` 即可作为其他模组的前置依赖。

---

## 快速上手

### 1. 引用 ValheimCatManager

在你的模组项目中引用编译好的 `ValheimCatManager.dll`，并确保它和你的模组一起放在 `BepInEx/plugins` 下。

### 2. 准备 AssetBundle

1. 在 Unity 中导出 AssetBundle 文件（例如 `myassets`）
2. 在你的 C# 项目中创建 `Assets` 文件夹，把 AB 文件放进去
3. 文件属性 → 生成操作设为 **"嵌入的资源"**（Embedded Resource）

### 3. 写代码

```csharp
using BepInEx;
using ValheimCatManager.CatUtils;
using ValheimCatManager.Config;

[BepInPlugin("com.yourname.yourmod", "你的模组名", "1.0.0")]
public class YourModPlugin : BaseUnityPlugin
{
    public void Awake()
    {
        // 1. 加载你的 AssetBundle（参数是嵌入资源名的结尾匹配）
        ResManager.Instance.LoadAssetBundle("myassets");

        // 2. 添加一个物品（第二个参数 true = 启用 Mock 替换）
        ResManager.Instance.AddItem("我的武器", true);

        // 3. 添加植被
        ResManager.Instance.AddVegetation(new VegetationConfig("覆盆子")
        {
            生态区域 = "Meadows",
            最小_数量 = 5,
            最大_数量 = 10
        }, true);

        // 4. 添加建筑
        ResManager.Instance.AddPiece(new PieceConfig("我的椅子"), true);
    }
}
```

---

## API 一览

所有 API 都通过 `ResManager.Instance` 调用，位于命名空间 `ValheimCatManager.CatUtils`。

### 资源加载

```csharp
// 加载调用方模组嵌入的 AssetBundle（按结尾名匹配）
ResManager.Instance.LoadAssetBundle("资源包名");
```

### 物品 / 装备 / 配方

```csharp
// 添加物品（注册到 ObjectDB + ZNetScene）
ResManager.Instance.AddItem("预制件名", mock: true);

// 添加装备并自动创建配方
// 参数：预制件名, mock, 制作站名, 最低制作台等级, 产出数量, 材料列表
ResManager.Instance.AddEquipment("我的剑", true, "piece_workbench", 1, 1,("木材", 10, 0, false), ("青铜", 5, 2, true));   // upgraderRes = true 表示升级专用材料

// 单独添加配方（两种重载）
ResManager.Instance.AddRecipe("我的剑", "piece_workbench", 1, 1, ("木材", 10, 0));

ResManager.Instance.AddRecipe("我的剑", "piece_workbench", 1, 1, ("木材", 10, 0, false), ("青铜", 5, 2, true));
```

### 建筑 / 食物

```csharp
// 添加建筑（Piece）
ResManager.Instance.AddPiece(new PieceConfig("我的墙"), mock: true);

// 添加可摆放食物（女巫版本，自动绑定 Feaster）
ResManager.Instance.AddFood("我的料理", mock: true);
```

### 怪物 / 生成

```csharp
// 添加怪物（含食谱/掉落配置）
ResManager.Instance.AddMonster(new MonsterConfig("我的怪"), mock: true);

// 添加生物生成规则
ResManager.Instance.AddSpawn(new SpawnConfig("我的怪"));
```

### 植被 / 杂物

```csharp
// 添加植被
ResManager.Instance.AddVegetation(new VegetationConfig("我的树"), mock: true);
```

### 烹饪站 / 炼制站

```csharp
// 添加烹饪站配方
ResManager.Instance.AddCookingStation(new CookingStationConfig());

// 添加炼制站转换（熔炉/高炉/提炼器）
ResManager.Instance.AddSmelters(new SmeltersConfig("熔炉", "铁矿石", "铁锭"));
```

### 地点 / 地下城 / 地图图标

```csharp
// 添加野外地点
ResManager.Instance.AddLocation("我的遗迹", new LocationConfig());

// 添加地下城房间
ResManager.Instance.AddRoom("我的房间", "我的主题");

// 添加地下城（主题 + 地点）
ResManager.Instance.AddDungeon("我的地牢入口", "我的主题", new LocationConfig());

// 添加地图地点图标（图片需为 64x64）
ResManager.Instance.AddLocationIcon("我的图标.png", "MyLocationIcon");
```

### 状态效果 / 动画

```csharp
// 添加状态效果（Buff/Debuff）
ResManager.Instance.AddStatusEffect("我的Buff");

// 添加动画（1~3 段，自动分组）
ResManager.Instance.AddAnimation("attack1", "attack2", "attack3");
```

### 武器攻击速度

按物品名修改攻击速度，**累加增量**模式：正数加速，负数减速，不设为 0 即不修改。

```csharp
// 参数：物品名, 主攻击速度增量, 次攻击速度增量
ResManager.Instance.ModifyAttackSpeed("我的剑", 0.2f, 0.1f);
```

> 主攻击速度对应武器左键，次攻击速度对应右键（如矛的刺击、斧的重击）。增量是在原版基础上叠加，例如原版攻速 1.0，传 0.2 后变为 1.2。

### 通用预制件

```csharp
// 添加非物品预制件（SFX、VFX 等，仅注册到 ZNetScene）
ResManager.Instance.AddPrefab("我的特效", mock: true);
```

---

## 配置类说明

所有配置类都在 `ValheimCatManager.Config` 命名空间下，字段为中文且带默认值，**不需要全部设置**。构造函数参数为必填项（通常是预制件名）。

### 以植被为例

```csharp
// 方式一：先 new 再赋值
var veg = new VegetationConfig("覆盆子")
{
   区域范围 = Heightmap.BiomeArea.Median,
   生态区域 = "Meadows",  // 兼容自定义群系名
   启用 = true,
   最小_数量 = 5,
   最大_数量 = 10
};

ResManager.Instance.AddVegetation(veg, true);

// 方式二：对象初始化器
ResManager.Instance.AddVegetation(new VegetationConfig("橡树")
{
    区域范围 = Heightmap.BiomeArea.Everything,
    生态区域 = "BlackForest",
    最小_数量 = 2,
    最大_数量 = 5
}, true);
```

### 可用配置类

| 配置类 | 用途 |
|--------|------|
| `VegetationConfig` | 植被生成 |
| `ClutterConfig` | 地表杂物 |
| `SpawnConfig` | 生物生成 |
| `MonsterConfig` | 怪物与食谱 |
| `PieceConfig` | 建筑物件 |
| `RecipeConfig` | 制作配方 |
| `RequirementConfig` | 材料需求 |
| `CookingStationConfig` | 烹饪站 |
| `SmeltersConfig` | 炼制站 |
| `LocationConfig` | 野外地点 |
| `RoomConfig` | 地下城房间 |
| `EnvConfig` | 天气环境 |
| `StatusEffectConfig` | 状态效果 |
| `AttackSpeedConfig` | 攻击速度 |

> 鼠标悬停在中文字段上即可看到注释说明。

---

## 自定义群系完整示例

以"死寂沼泽"为例，展示从零注册一个自定义群系的完整流程。一个自定义群系通常包含：群系注册 → 天气 → 植被 → 杂物 → 地点 → 地下城。

> 自定义群系通过 `WorldManager` 分配独立的 Flags 身份位（从 0x400 起），地形高度和材质借用一个官方群系作为"皮囊"，植被/天气/地点等内容则完全自定义。

### 1. 注册群系

```csharp
using ValheimCatManager.Managers;
using ValheimCatManager.CatUtils;

// 参数：群系id(英文小写), 地形皮囊(借用哪个官方群系的地形/材质), 大地图颜色, 显示名
Heightmap.Biome 死寂沼泽 = WorldManager.Instance.RegisterBiome(
    "newzhaoze",
    Heightmap.Biome.Swamp,          // 借用沼泽的地形高度和材质
    new Color(0.30f, 0.40f, 0.30f), // 大地图显示色
    "死寂沼泽"                        // UI 显示名
);
```

注册后可以用 `CatTool.GetBiome("newzhaoze")` 随时获取这个群系的枚举值。

### 2. 注册天气

先注册自定义天气（基于官方天气模板修改颜色、雾、风等），再把多个天气按权重绑定到群系。

```csharp
// 晴朗天气（基于官方 Clear，标记为默认）
BiomeEnvManager.Instance.RegisterEnv(new EnvConfig
{
    Name = "zhaoze_clear",
    FromOfficial = "Clear",   // 继承官方晴天的粒子/音效等
    IsDefault = true,
    IsColdAtNight = true,
    AmbColorDay = new Color(0.46f, 0.57f, 0.70f, 1f),
    FogColorDay = new Color(0.30f, 0.58f, 0.74f, 1f),
    FogDensityDay = 0.03f,
    LightIntensityDay = 1.7f,
    // ... 还有早/晚/夜的颜色、雾密度、太阳颜色等几十个字段
});

// 雨天（基于官方 Rain）
BiomeEnvManager.Instance.RegisterEnv(new EnvConfig
{
    Name = "zhaoze_rain",
    FromOfficial = "Rain",
    IsWet = true,
    AlwaysDark = true,
    WindMin = 0.5f,
    WindMax = 1.2f,
    RainCloudAlpha = 2.2f,
    // ...
});

// 把天气绑定到群系，权重越高出现概率越大
BiomeEnvManager.Instance.RegisterBiomeEnv(new BiomeEnvManager.BiomeEnvData
{
    Name = "死寂沼泽",
    Biome = CatTool.GetBiome("newzhaoze"),
    Environments =
    {
        ["zhaoze_clear"] = 5f,  // 晴朗最常见
        ["zhaoze_rain"]  = 3f,  // 雨次之
    },
    MusicDay = "swamp",
    MusicNight = "swamp"
});
```

### 3. 注册植被

直接调用 `VegetationManager.RegisterVegetationSpawn`，`生态区域` 填自定义群系 id 即可。

```csharp
VegetationManager.Instance.RegisterVegetationSpawn(new VegetationConfig("枯树")
{
    生态区域 = "newzhaoze",
    最小_数量 = 20,
    最大_数量 = 30,
    最小_缩放 = 1.5f,
    最大_缩放 = 3f,
    最低_需求高度 = -2f,
    最高_需求高度 = 0.5f,
    最大倾斜 = 12f,
});

VegetationManager.Instance.RegisterVegetationSpawn(new VegetationConfig("毒蘑菇")
{
    生态区域 = "newzhaoze",
    最小_数量 = 5,
    最大_数量 = 15,
    最小_缩放 = 0.3f,
    最大_缩放 = 0.8f,
    最低_需求高度 = 0.5f,
    森林最小阈值 = 1.1f,  // 仅在森林区域生成
    森林最大阈值 = 1.2f,
});
```

### 4. 注册杂物（Clutter）

草、碎石、水面雾气等地表装饰用 `ClutterManager.RegisterClutter`。

```csharp
ClutterManager.Instance.RegisterClutter(new ClutterConfig("instanced_swamp_grass")
{
    Name = "zhaoze_grass",
    BiomeName = "newzhaoze",
    Instanced = true,       // GPU 实例化渲染（大量小草必备）
    Amount = 150,
    ScaleMin = 0.8f,
    ScaleMax = 2.5f,
    MaxTilt = 25f,
    MinAlt = 0f,
    MaxAlt = 1000f,
    TerrainTilt = true,     // 随地形倾斜
});

// 水生植物（吸附水面）
ClutterManager.Instance.RegisterClutter(new ClutterConfig("instanced_vass")
{
    Name = "zhaoze_reed",
    BiomeName = "newzhaoze",
    Instanced = true,
    Amount = 15,
    SnapToWater = true,      // 吸附水面
    MinAlt = -1f,
    MaxAlt = 30f,
    FractalScale = 5f,       // 分形噪声做成斑块分布
});
```

### 5. 注册地点和地下城

```csharp
// 野外兴趣点（废墟、资源点等）
ResManager.Instance.AddLocation("死寂废墟", new LocationConfig
{
    生态区域 = "newzhaoze",
    最大数量 = 50,
    最低高度 = -1f,
    森林内生成 = true,
    随机旋转 = true,
    最大地形偏差 = 2f,
    清理区域 = true,         // 生成时清理周围植被
});

// 地下城房间（归入同一个主题）
ResManager.Instance.AddRoom("死寂_入口", "死寂监狱");
ResManager.Instance.AddRoom("死寂_牢房1", "死寂监狱");
ResManager.Instance.AddRoom("死寂_大厅", "死寂监狱");

// 地下城入口（主题 + 地点配置）
ResManager.Instance.AddDungeon("死寂_地牢入口", "死寂监狱", new LocationConfig
{
    生态区域 = "newzhaoze",
    最大数量 = 200,
    同类最小距离 = 256,
    最低高度 = 1f,
    森林内生成 = true,
    最大地形偏差 = 35f,
});
```

### 6. 注册资源预制件

地下城和地点里用到的自定义建筑部件、生成器、宝箱等，先通过 `AddPrefab` 注册。

```csharp
ResManager.Instance.AddPrefab("破损铁栅", true);
ResManager.Instance.AddPrefab("地牢铁门", true);
ResManager.Instance.AddPrefab("生成_亡灵士兵", true);
ResManager.Instance.AddPrefab("死寂_宝箱", true);
```

### 7. 修改群系分布规则（可选）

默认情况下自定义群系不会自动出现在世界中，需要通过 `ModifyRule` 设定它的生成条件（距离、海拔、噪声等）。

```csharp
// 在 layer 550 插入一条规则：死寂沼泽在距中心 2000~6000 米、低海拔区域生成
WorldManager.Instance.ModifyRule(550,
    biome: CatTool.GetBiome("newzhaoze"),
    minDistance: 2000f,
    maxDistance: 6000f,
    minHeight: 0.05f,
    maxHeight: 0.25f,
    perlinOffset: 0,
    perlinMin: 0.6f);
```

> layer 是判定优先级，数字越小越先判。官方默认规则在 500~950 之间，插入中间值即可覆盖或穿插。

### 完整注册类模板

把上面所有步骤封装成一个注册类，在插件 `Awake` 里 `new` 一下即可：

```csharp
internal class 死寂沼泽注册
{
    public 死寂沼泽注册()
    {
        WorldManager.Instance.RegisterBiome("newzhaoze", Heightmap.Biome.Swamp, new Color(0.3f, 0.4f, 0.3f), "死寂沼泽");
        注册天气();
        注册植被();
        注册杂物();
        注册地点();
        注册地下城();
        注册资源();
    }
    // ... 各方法内容见上方示例
}
```

```csharp
// 在插件 Awake 中
public void Awake()
{
    new 死寂沼泽注册();
}
```

---

## Mock 系统

### 什么是 Mock？

在 Unity 里做模组资源时，你没法直接引用游戏里的预制件、着色器和材质（因为它们在游戏运行时才存在）。Mock 系统的做法是：

1. 在 Unity 里创建一个**空占位预制件**，命名为 `JVLmock_真实预制件名`
2. 把这个占位预制件挂到你自定义组件的字段上
3. 游戏运行时，Mock 系统自动扫描所有字段，把 `JVLmock_` 开头的引用替换成游戏里真实的资源

### 支持替换的三种资源

- **预制件（GameObject）**：组件字段、数组、列表、DropTable 掉落表
- **着色器（Shader）**：Renderer 材质上的 shader
- **材质（Material）**：Renderer 的 sharedMaterials 数组元素

### 使用方法

**Unity 端**：新建空预制件 → 改名加 `JVLmock_` 前缀 → 挂到对应组件字段。

**代码端**：调用 `AddItem` / `AddPiece` / `AddVegetation` 等方法时，把最后一个 `mock` 参数设为 `true`：

```csharp
ResManager.Instance.AddItem("我的物品", mock: true);
```

> 前缀沿用 `JVLmock_`（历史原因，源自 Jotunn 前置的命名习惯），后续版本可能支持自定义前缀。

---

## 配置文件

插件首次运行后会在 `BepInEx/config/com.rambo7at.CatManager.cfg` 生成配置文件。

### 数据导出（开发参考用，默认关闭）

开启后，玩家进入世界时会把游戏原版配置导出到 `config/CatManager/GameData/` 目录，方便你制作自定义内容时参考原版数值：

| 选项 | 导出内容 |
|------|----------|
| 启用数据导出 | 总开关 |
| 导出怪物生成 | `monster_spawn.txt` |
| 导出群系环境 | `biome_environments.txt`（各群系天气与音乐） |
| 导出天气详情 | `environments.txt`（颜色、雾、风、粒子、音效） |
| 导出地点 | `locations.txt` |
| 导出植被 | `vegetation.txt` |
| 导出地面杂物 | `clutter.txt` |

### 旧世界自动升级（⚠️ 危险）

```
启用旧世界自动升级 = false
```

开启后，检测到旧存档已生成过区块时，会**自动删除所有植被和地点数据并重新生成**，让新规则在旧世界生效。

> ⚠️ **此操作不可逆，执行前务必备份世界存档！** 仅用于从旧版本升级到新版本，完成后建议关闭。

---

## 项目结构

```
ValheimCatManager/
├── CatManagerPlugin.cs      # 插件入口（BepInPlugin）
├── CatConfig.cs             # 配置文件绑定
├── CatUtils/
│   ├── ResManager.cs        # ★ 核心 API，所有 AddXxx 方法
│   ├── CatTool.cs           # 工具方法（获取预制件/着色器/材质）
│   ├── GameDataExporter.cs  # 游戏数据导出
│   ├── LegacyWorldUpgrade.cs# 旧世界植被/地点清理
│   └── DiskBundleMaterialLoader.cs
├── Config/                  # 所有中文配置类
├── Managers/                # 各子系统管理器（注册到游戏的实际逻辑）
├── Mock/                    # Mock 替换系统
├── Data/
│   ├── CatModData.cs        # 模组数据
│   └── CustomTheme.cs       # 自定义群系主题
└── Interface/
    └── IZPackageSync.cs     # 配置同步接口
```

---

## 致谢

- **BepInEx 团队** — 模组加载框架
- **IronGate / Valheim 开发团队** — 这款精彩的游戏
- **所有 Valheim 模组社区开发者**

---

<div align="center">

如果这个项目对你有帮助，欢迎点个 Star ⭐

**让模组开发变得简单而强大** 🐱

</div>
