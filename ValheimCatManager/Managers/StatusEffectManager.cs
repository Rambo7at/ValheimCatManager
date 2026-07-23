using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using ValheimCatManager;
using ValheimCatManager.Managers;

namespace ValheimCatManager.Managers
{
    public class StatusEffectManager
    {
        private static StatusEffectManager _instance;

        public static StatusEffectManager Instance => _instance ?? (_instance = new StatusEffectManager());

        private StatusEffectManager() => HarmonyPatchManager.OnObjectDBAwakeModify += RegisterStatusEffectPatch;

        /// <summary>
        /// 注：自定义效果的字典
        /// </summary>
        public readonly Dictionary<string, StatusEffect> customStatusEffectDict = new Dictionary<string, StatusEffect>();

        private void RegisterStatusEffectPatch(ObjectDB __instance)
        {
            if (SceneManager.GetActiveScene().name == "main") Instance.RegisterStatusEffect(__instance, Instance.customStatusEffectDict);
        }


        private void RegisterStatusEffect(ObjectDB objectDB, Dictionary<string, StatusEffect> statusEffectDict)
        {
            foreach (var statusEffect in statusEffectDict) objectDB.m_StatusEffects.Add(statusEffect.Value);
        }
    }
}
