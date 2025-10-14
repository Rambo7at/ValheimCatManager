using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ValheimCatManager.Managers
{
    /// <summary>
    /// 动画管理器：负责创建动画覆盖控制器、管理动画片段替换
    /// </summary>
    /// <summary>
    /// 动画管理器：负责创建动画覆盖控制器、管理动画片段替换
    /// </summary>
    public class AnimationManager
    {
        // 单例保持简洁
        private static AnimationManager _instance;
        public static AnimationManager Instance => _instance ?? (_instance = new AnimationManager());


        // 公开字段（外部添加数据）
        public readonly List<List<string>> animationList = new();
        public readonly Dictionary<string, AnimationClip> animationDict = new Dictionary<string, AnimationClip>();

        // 私有字段
        private RuntimeAnimatorController ValheimAnimationController;
        private string _asmName;
        private readonly Dictionary<string, KeyValuePair<RuntimeAnimatorController, string>> Controllers = new Dictionary<string, KeyValuePair<RuntimeAnimatorController, string>>();
        private bool _firstInit; // 补充初始化标记，替代老代码的FirstInit

        private AnimationManager() =>  new Harmony("AnimationPatch").PatchAll(typeof(AnimationPatch));


        // 补丁类：包含Player.Start和ZSyncAnimation.RPC_SetTrigger两个核心补丁
        private class AnimationPatch
        {
            [HarmonyPatch(typeof(Player), nameof(Player.Start)), HarmonyPostfix, HarmonyPriority(10000)]
            static void AddAnimationPatch(ref Player __instance) => Instance.Addanimation(ref __instance);

            [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.RPC_SetTrigger)), HarmonyPrefix]
            static bool RpcSetTriggerPatch(ZSyncAnimation __instance, string name) => Instance.HandleAnimationTrigger(__instance, name);
        }


        // 初始化动画控制器（补充_firstInit检查）
        private void Addanimation(ref Player __instance)
        {
            // 补充：确保初始化逻辑只执行一次
            if (_firstInit) return;
            _firstInit = true;

            if (__instance == null)
            {
                Debug.LogError("Addanimation失败：Player实例为null");
                return;
            }

            Animator animator = __instance.m_animator;
            if (animator == null)
            {
                Debug.LogError("Addanimation失败：Player的Animator组件为null");
                return;
            }

            // 初始化基础控制器
            ValheimAnimationController = CreateAnimationOverrideController(new Dictionary<string, string>(), animator.runtimeAnimatorController);
            if (ValheimAnimationController != null)
            {
                ValheimAnimationController.name = "CateAnimatorController";
            }
            else
            {
                Debug.LogWarning("基础动画控制器创建失败，可能影响后续动画替换");
                return;
            }

            // 处理动画集
            foreach (var animationName in animationList)
            {
                if (animationName == null)
                {
                    Debug.LogWarning("跳过空的动画集");
                    continue;
                }

                var dictionary = new Dictionary<string, string>();
                for (int i = 0; i < animationName.Count; i++)
                {
                    string animName = animationName[i];
                    if (string.IsNullOrEmpty(animName))
                    {
                        Debug.LogWarning($"动画集中第{i + 1}个元素为空，已跳过");
                        continue;
                    }
                    dictionary.Add($"Attack{i + 1}", animName);
                }

                var runtimeAnimatorController = CreateAnimationOverrideController(dictionary, animator.runtimeAnimatorController);
                if (runtimeAnimatorController == null)
                {
                    Debug.LogWarning("创建自定义动画控制器失败，已跳过该动画集");
                    continue;
                }

                runtimeAnimatorController.name = AssemblyName();

                for (int j = 0; j < animationName.Count; j++)
                {
                    string animName = animationName[j];
                    if (string.IsNullOrEmpty(animName)) continue;

                    if (Controllers.ContainsKey(animName))
                    {
                        Debug.LogWarning($"动画 {animName} 已存在映射，将覆盖");
                    }
                    Controllers[animName] = new KeyValuePair<RuntimeAnimatorController, string>(
                        runtimeAnimatorController,
                        $"swing_longsword{j}"
                    );
                }
            }
        }


        // 补充：处理动画触发逻辑（对应老代码的ZSyncAnimation补丁）
        private bool HandleAnimationTrigger(ZSyncAnimation zSyncAnim, string triggerName)
        {
            // 反射获取ZSyncAnimation中的m_animator字段（与老代码逻辑一致）
            FieldInfo animatorField = typeof(ZSyncAnimation).GetField("m_animator", BindingFlags.NonPublic | BindingFlags.Instance);
            if (animatorField == null) return true;

            Animator animator = (Animator)animatorField.GetValue(zSyncAnim);
            if (animator == null) return true;

            // 检查是否有对应的自定义控制器
            if (Controllers.TryGetValue(triggerName, out var kvp))
            {
                // 切换到自定义控制器并触发动画（实现老代码ReplacePlayerRAC的功能）
                if (animator.runtimeAnimatorController != kvp.Key)
                {
                    animator.runtimeAnimatorController = kvp.Key;
                    animator.Update(0f); // 强制更新动画状态
                }
                animator.SetTrigger(kvp.Value);
                return false; // 阻止原方法执行
            }

            // 若当前是自定义控制器，还原为默认控制器
            if (animator.runtimeAnimatorController?.name == AssemblyName() && ValheimAnimationController != null)
            {
                animator.runtimeAnimatorController = ValheimAnimationController;
                animator.Update(0f);
            }

            return true; // 执行原方法
        }


        // 程序集名称获取
        public string AssemblyName()
        {
            return _asmName ??= Assembly.GetExecutingAssembly().GetName().Name;
        }


        // 创建动画覆盖控制器
        private RuntimeAnimatorController CreateAnimationOverrideController(
            Dictionary<string, string> replacementControlle,
            RuntimeAnimatorController ValheimControlle)
        {
            if (ValheimControlle == null)
            {
                Debug.LogError("原始动画控制器为null");
                return null;
            }

            var animatorController = new AnimatorOverrideController(ValheimControlle);
            var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            foreach (AnimationClip animationClip in animatorController.animationClips)
            {
                if (animationClip == null) continue;

                string name = animationClip.name;
                if (replacementControlle.TryGetValue(name, out string text))
                {
                    if (animationDict.TryGetValue(text, out var targetClip))
                    {
                        list.Add(new KeyValuePair<AnimationClip, AnimationClip>(animationClip, Object.Instantiate(targetClip)));
                    }
                    else
                    {
                        Debug.LogError($"未找到动画片段: {text}");
                        list.Add(new KeyValuePair<AnimationClip, AnimationClip>(animationClip, animationClip));
                    }
                }
                else
                {
                    list.Add(new KeyValuePair<AnimationClip, AnimationClip>(animationClip, animationClip));
                }
            }

            animatorController.ApplyOverrides(list);
            return animatorController;
        }
    }

}
