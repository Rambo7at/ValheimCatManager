using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimCatManager.Managers
{
    public class DungeonManager
    {

        private static DungeonManager _instance;


        public static DungeonManager Instance => _instance ?? (_instance = new DungeonManager());


        private  DungeonManager() => new Harmony("DungeonManager").PatchAll(typeof(DungeonPatch));



        private class DungeonPatch
        { 
        
        
        
        
        
        
        
        
        
        }











    }
}
