using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimCatManager.Data;
using ValheimCatManager.Tool;

namespace CatIndepMonsterSet.Patch
{

    class MonsterResistanceBalance
    {

        private static MonsterResistanceBalance _instance;

        public static MonsterResistanceBalance Instance => _instance ?? (_instance = new MonsterResistanceBalance());

        private MonsterResistanceBalance () => new Harmony("MonsterResistanceBalance").PatchAll(typeof(MonsterResistanceBalancePatch));


        public void Enable() { }

        public  List<string> 亡灵怪物表 { get; set; } = new List<string>()
        {
             "Abomination","Blob","BlobElite","BlobLava","BlobTar","Bonemass","Charred_Archer","Charred_Archer_Fader",
             "Charred_Mage","Charred_Melee","Charred_Melee_Dyrnwyn","Charred_Melee_Fader","Charred_Twitcher","Charred_Twitcher_Summoned","Draugr","Draugr_Elite",
             "Draugr_Ranged","FallenValkyrie","Morgen","Morgen_NonSleeping","Skeleton","Skeleton_Hildir","Skeleton_Hildir_nochest","Skeleton_NoArcher",
             "Skeleton_Poison","Troll_Summoned","Unbjorn"


        };

        private static class MonsterResistanceBalancePatch
        {

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(Priority.High)]
            static void ResistanceBalancePatch()
            {
                if (Instance.亡灵怪物表.Count == 0) return;

                if (SceneManager.GetActiveScene().name == "main")
                {
                    foreach (var monster in Instance.亡灵怪物表)
                    {
                        var CreaturePrefab = CatToolManager.GetGameObject(monster);
                        Humanoid humanoid = CreaturePrefab.GetComponent<Humanoid>();
                        humanoid.m_damageModifiers.m_spirit = HitData.DamageModifier.VeryWeak;
                    }
                    Instance.亡灵怪物表.Clear();
                }

            }

        }


    }






}
