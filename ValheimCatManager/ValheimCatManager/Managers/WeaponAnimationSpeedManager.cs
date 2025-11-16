using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValheimCatManager;
using ValheimCatManager.ValheimCatManager.Managers;

namespace ValheimCatManager.ValheimCatManager.Managers
{
    public class WeaponAnimationSpeedManager
    {

        private WeaponAnimationSpeedManager _instance;

        public WeaponAnimationSpeedManager Instance => _instance ?? (_instance = new WeaponAnimationSpeedManager());


        private WeaponAnimationSpeedManager() => new Harmony("WeaponAnimationSpeedManagerPatch").PatchAll(typeof(WeaponAnimationSpeedManagerPatch));



        private static class WeaponAnimationSpeedManagerPatch
        { 
        
        
        
        
        }





    }
}
