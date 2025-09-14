<div align="center">

# 🐱 ValheimCatManager 🐱


### 🚀 | **轻松上手** | 🚀

</div>

--- 
 
## 📋 目录

- [🌟 项目简介](#-项目简介)
- [✨ 核心特性](#-核心特性)
- [🛠️ 快速开始](#️-快速开始)
- [📚 详细使用指南](#-详细使用指南)
- [🎯 Mock系统](#-mock系统)

---

## 🌟 项目简介

**ValheimCatManager** 是一个专为 Valheim 中文模组开发者设计的资源管理框架。它提供了一套完整、直观的 API，能帮助您轻松管理或扩展游戏中的各类资源与配置，为中文 mod 的开发提供针对性支持。

### 🎯 ValheimCatManager

- **🎨 学习成本** - 中文API设计，浅学一下c#就可以尝试制作。
- **⚡ 资源加载** - 基于AssetBundle的资源加载。
- **🔧 高度可配置** - 支持植被、物品、建筑、怪物等全方位的游戏内容定制
- **🛡️ 减轻资源** - Mock系统，重复利用原版资源，无需单独建立

---

## ✨ 核心特性

### 🎮 游戏内容管理
- **🏗️ 物品管理** - 轻松添加自定义物品、武器、工具
- **🌱 植被管理** - 智能植被生成，支持复杂的地形和生态配置
- **🏠 建筑管理** - 完整的Piece物件管理，支持制作配方和材料需求
- **👹 怪物管理** - 自定义怪物AI、掉落物、生成规则
- **🍳 工艺管理** - 烹饪站和炼制站的完整配置支持

### 🔧 开发工具
- **📦 资源包管理** - 自动化的AssetBundle加载和管理
- **🎭 Mock系统** - 智能占位符替换，确保模组兼容性
---

## 🛠️ 快速开始

### 📋 要求

- **Valheim** 游戏本体
- **BepInEx 5.4.21++** 
- **Visual Studio 2022** (推荐)
- **.NET 4.8.1** 开发环境

### ⚡ 5分钟快速上手

#### 1️⃣ 创建新项目

```csharp
using BepInEx;
using ValheimCatManager.Tool;

[BepInPlugin("com.yourname.yourmod", "你的模组名", "1.0.0")]
public class YourModPlugin : BaseUnityPlugin
{
    public void Awake()
    {
        // 🎯 加载资源包
        CatResModManager.Instance.LoadAssetBundle("预制名");
        
        // 🎮 添加物品
        CatResModManager.Instance.AddItem("预制名", true);
        
        // 🌱 添加植被
        CatResModManager.Instance.AddVegetation(new VegetationConfig("覆盆子")
        {
            生态区域 = "Meadows",
            最小_数量 = 5,
            最大_数量 = 10
        }, true);
    }
}
```

#### 2️⃣ 配置资源包

1. 在项目中创建 `Asset` 文件夹
2. 将Unity导出的AssetBundle文件放入
3. 设置文件属性为"嵌入式资源"(重要)

---

## 📚 详细使用指南


#### 无需设置的预制件添加

```C#
// 添加物品：这些物品会注册至 ObjectDB 和 ZNetScene
// 参数： 1，预制件名，2，启用mock
CatResModManager.Instance.AddItem("预制件名",true);


// 添加预制件：这些物品会注册至 ZNetScene ，不会注册给 ObjectDB
// 参数：1，预制件名，2，启用mock
// 非物品类：SFX，VFX，(怪物也是包括的)
CatResModManager.Instance.AddPrefab("预制件名", true);


// 添加食物：针对女巫版本食物的方法。
// 参数：1，预制件名，2，食物目录，3，启用mock
CatResModManager.Instance.AddFood("预制件名","蔬菜类" ,true);

```
#### 无需设置的预制件添加

```C#
// 添加植被：针对植被的方法
// 参数： 1，植被配置类(VegetationConfig)，2，启用mock
CatResModManager.Instance.AddVegetation(new VegetationConfig("植被名"),true);

// 添加生成：给预制件增加生成，生成配置会注册给 m_spawnLists
// 参数：1，生成配置类(SpawnConfig)与Spawn That相似
CatResModManager.Instance.AddSpawn(new SpawnConfig("生物名"));

// 添加物品：给游戏添加 物件的类，例：木墙，火堆，椅子。。。。
// 参数： 1，物件配置类，2，启用mock
// 注：物件目录，如果需要空，填：None，场景：耕地耙，官方默认就是空
CatResModManager.Instance.AddPiece(new PieceConfig("物件名"),true);

// 炼制站转换：熔炉，高炉，提炼器
// 参数： 构造函数：炼制站预制名，输入材料，输出材料
CatResModManager.Instance.AddSmelters(new SmeltersConfig("熔炉","铁块","铁锭"));
```

#### 配置类演示
```c#
// 以【添加植被】为例
// 配置：每个选项我都有默认值，并不需要全部设置。每个选项都是中文字段，鼠标悬停会有对应说明
// 注：构造函数的信息是必填的，需要对应植被的预制名。

// 植被添加方式-1
VegetationConfig vegetationConfig1 = new VegetationConfig("覆盆子");
vegetationConfig1.区域范围 = Heightmap.BiomeArea.Median;
vegetationConfig1.生态区域 = "Meadows";  // 兼容EW 的自定义区域
vegetationConfig1.启用 = true;
vegetationConfig1.最小_数量 = 5;
vegetationConfig1.最大_数量 = 10;
CatResModManager.Instance.AddVegetation(vegetationConfig1, true);

// 植被添加方式-2
VegetationConfig vegetationConfig2 = new VegetationConfig("橡树")
{
    区域范围 = Heightmap.BiomeArea.Everything,
    生态区域 = "BlackForest",
    最大_数量 = 5,
    最小_数量 = 2
};
CatResModManager.Instance.AddVegetation(vegetationConfig2, true);

// 植被添加方式-3
CatResModManager.Instance.AddVegetation(new VegetationConfig("洋葱种子")
{
    区域范围 = Heightmap.BiomeArea.Everything,
    生态区域 = "BlackForest",
    最大_数量 = 5,
    最小_数量 = 2
}, true);
```


---

## 🎯 Mock系统

### 🔍 什么是Mock系统？
---
-  **Mock**系统是一个用于游戏资源动态替换的工具，主要功能是在游戏运行时将占位资源（预制件 / 着色器）替换为真实游戏资源。
它通过识别带有特定前缀（JVLmock_）的占位资源，自动关联并替换为对应的真实资源，解决了模组开发中资源引用时机、依赖管理等问题，确保模组资源能正确加载和生效。** 
---

### 🔍 为什么是JVLmock前缀？
---
-  我在制作Mod的时候，以**Jotuun**作为前置，**unity**项目中都是**JVLmock**前缀的空预制件，所以一直沿用。之后更新自定义前缀
---


### 🎭 工作原理
---
-  **Mock**系统系统的核心流程分为资源收集和资源替换两大阶段，整体工作流程： → 收集占位预制件信息 → 收集占位着色器信息 → 替换预制件 → 替换着色器 → 完成替换
---




### 🛠️ 使用方法
---
-  1：unity项目：右键-新建预制件-更改JVLmock前缀的名称-挂载在对应脚本组件中

-  2：代码中操作，在添加对应预制件时，尾部打开 mock ，示例如下
---

#### 2️⃣ 在代码中启用Mock

```csharp
// 添加物品时启用Mock
CatResModManager.Instance.AddItem("苹果", true);  // true = 启用Mock
```





## 🙏 致谢

感谢所有为Valheim模组社区做出贡献的开发者们！

特别感谢：
- **BepInEx团队** - 提供了强大的模组加载框架
- **Valheim开发团队** - 创造了这个精彩的游戏
- **所有贡献者** - 让这个项目变得更好

---

<div align="center">

### ⭐ 如果这个项目对你有帮助，请给我们一个Star！

**让模组开发变得简单而强大** 🚀

[![GitHub stars](https://img.shields.io/github/stars/yourusername/ValheimCatManager?style=social)](https://github.com/yourusername/ValheimCatManager)
[![GitHub forks](https://img.shields.io/github/forks/yourusername/ValheimCatManager?style=social)](https://github.com/yourusername/ValheimCatManager/fork)

---

**Made with ❤️ by ValheimCatManager Team**

</div>
