using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimCatManager.Config;

namespace ValheimCatManager.Managers;

/// <summary>注：攻击速度管理器，在攻击动画期间修改玩家/怪物的Animator.speed</summary>
internal class AttackSpeedManager
{
    private static AttackSpeedManager _instance;

    /// <summary>注：单例全局实例，懒加载模式</summary>
    public static AttackSpeedManager Instance => _instance ?? (_instance = new AttackSpeedManager());

    /// <summary>注：构造函数，挂载Harmony补丁</summary>
    public AttackSpeedManager() => new Harmony("AttackSpeedManagerPatch").PatchAll(typeof(AttackSpeedPatch));

    /// <summary>注：攻击速度配置字典，Key=预制件名称（无Clone后缀），Value=攻击速度配置（增量值）</summary>
    private Dictionary<string, AttackSpeedConfig> AttackSpeedDict = [];


    /// <summary>注：Harmony补丁类，挂载到CharacterAnimEvent.CustomFixedUpdate</summary>
    private static class AttackSpeedPatch
    {
        [HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.CustomFixedUpdate)), HarmonyPostfix, HarmonyPriority(0)]
        private static void CharacterAnimEvent_CustomFixedUpdate_Postfix(CharacterAnimEvent __instance)
        {
            Instance.Modify(__instance.m_character, __instance.m_animator);
        }
    }

    /// <summary>注：修改攻击速度（累加增量，正数加速，负数减速）</summary>
    public void ModifyAttackSpeed(string itemName, AttackSpeedConfig config)
    {
        if (AttackSpeedDict.TryGetValue(itemName, out var existing))
        {
            existing.PrimarySpeed += config.PrimarySpeed;
            existing.SecondarySpeed += config.SecondarySpeed;

            if (existing.PrimarySpeed < 0.01f) existing.PrimarySpeed = 0.01f;
            if (existing.SecondarySpeed < 0.01f) existing.SecondarySpeed = 0.01f;

            if (existing.PrimarySpeed > 5f) existing.PrimarySpeed = 5f;
            if (existing.SecondarySpeed > 5f) existing.SecondarySpeed = 5f;
        }
        else
        {
            AttackSpeedDict[itemName] = config;
        }
    }

    /// <summary>注：核心修改入口，根据角色类型分发到玩家或怪物逻辑</summary>
    private void Modify(Character character, Animator animator)
    {
        if (character == null || animator == null) return;

        if (character is Player player)
        {
            ModifyPlayer(player, animator);
            return;
        }

        if (character is Humanoid humanoid && !humanoid.IsPlayer())
        {
            ModifyMonster(humanoid, animator);

        }
    }

    /// <summary>注：玩家速度修改逻辑，通过武器名称查表获取倍率</summary>
    private void ModifyPlayer(Player player, Animator animator)
    {
        if (!player.InAttack()) return;
        if (player.GetCurrentWeapon() is not ItemDrop.ItemData itemData) return;
        if (itemData.m_dropPrefab == null) return;

        string weaponName = Utils.GetPrefabName(itemData.m_dropPrefab);
        if (!AttackSpeedDict.TryGetValue(weaponName, out var config)) return;

        float multiplier = player.m_currentAttackIsSecondary ? config.SecondarySpeed : config.PrimarySpeed;
        ApplySpeed(animator, multiplier);
    }

    /// <summary>注：怪物速度修改逻辑，通过怪物名称查表获取倍率</summary>
    private void ModifyMonster(Humanoid humanoid, Animator animator)
    {
        if (!humanoid.InAttack()) return;

        string monsterName = Utils.GetPrefabName(humanoid.gameObject);
        if (!AttackSpeedDict.TryGetValue(monsterName, out var config)) return;

        ApplySpeed(animator, config.PrimarySpeed);
    }

    /// <summary>注：统一速度应用逻辑，使用尾数标记防止每帧累乘</summary>
    private void ApplySpeed(Animator animator, float multiplier)
    {
        float currentSpeed = animator.speed;

        // 提取尾数标记（与旧代码一致，用于判断速度是否已被本补丁修改过）
        double marker = currentSpeed * 10000000.0 % 100.0;

        // 如果尾数在 10~30 之间，说明已被标记，跳过本帧防止累乘
        if (marker > 10.0 && marker < 30.0) return;

        // 速度太小（接近0）时跳过，防止异常
        if (currentSpeed <= 0.001f) return;

        // 基于当前速度乘以倍率（保留原版游戏自己的速度调整）
        float newSpeed = currentSpeed * multiplier;
        newSpeed = Mathf.Clamp(newSpeed, 0.01f, 5f);

        // 修约到 1e-5 精度，去除随机尾数
        newSpeed = Mathf.Round(newSpeed / 1e-5f) * 1e-5f;

        // 添加微小尾数标记（1.9e-6），标记已修改
        newSpeed += 1.9e-6f;

        animator.speed = newSpeed;
    }
}

