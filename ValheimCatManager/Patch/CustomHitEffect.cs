using CatIndepMonsterSet;
using CatIndepMonsterSet.Patch;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;



namespace CatIndepMonsterSet
{
    public class CustomHitEffect : MonoBehaviour, IDestructible
    {
        // ========== 可配置参数 ==========
        public EffectList m_hitEffect = new EffectList(); // 击中效果（粒子、声音等）
        public float m_hitNoise = 100f; // 击中时产生的噪音（吸引敌人）

        // ========== 实现 IDestructible 接口 ==========
        public DestructibleType GetDestructibleType()
        {
            return DestructibleType.Default; // 返回默认类型，不影响效果播放
        }

        public void Damage(HitData hit)
        {
            // 1. 播放击中效果（位置=攻击点，旋转=默认，父物体=当前对象）
            m_hitEffect.Create(hit.m_point, Quaternion.identity, transform);

            // 2. （可选）触发击中噪音，吸引附近敌人
            if (m_hitNoise > 0f)
            {
                Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
                if (closestPlayer != null)
                {
                    closestPlayer.AddNoise(m_hitNoise);
                }
            }

        }
    }
}


