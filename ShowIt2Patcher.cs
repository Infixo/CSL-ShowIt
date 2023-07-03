using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using HarmonyLib;

namespace ShowIt2
{
    public static class ShowIt2Patcher
    {
        public const string HarmonyId = "Infixo.ShowIt2";
        public static bool patched = false;

        public static void PatchAll()
        {
            if (patched) { Debug.Log($"{HarmonyId}.PatchAll: already patched!"); return; }
            //Harmony.DEBUG = true;
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll();
            if (Harmony.HasAnyPatches(HarmonyId))
            {
                Debug.Log($"{HarmonyId}.PatchAll: OK methods patched");
                patched = true;
                var myOriginalMethods = harmony.GetPatchedMethods();
                foreach (var method in myOriginalMethods)
                    Debug.Log($"{HarmonyId}.PatchAll: ...method {method.Name}");
            }
            else
                Debug.Log($"{HarmonyId}.PatchAll: ERROR methods not patched");
            //Harmony.DEBUG = false;
        }

        public static void UnpatchAll()
        {
            if (!patched) { Debug.Log($"{HarmonyId}.UnpatchAll: not patched!"); return; }
            //Harmony.DEBUG = true;
            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            Debug.Log($"{HarmonyId}.UnpatchAll: OK methods unpatched");
            patched = false;
            //Harmony.DEBUG = false;
        }
    }
    
    [HarmonyPatch(typeof(CommonBuildingAI))]
    public static class CommonBuildingAI_Patches
    {
        // CommonBuildingAI.GetHomeBehaviour reverse patch
        [HarmonyReversePatch, HarmonyPatch("GetHomeBehaviour")]
        public static void GetHomeBehaviour_Reverse(CommonBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount, ref int homeCount, ref int aliveHomeCount, ref int emptyHomeCount)
        {
            Debug.Log($"{ShowIt2Patcher.HarmonyId}: ERROR GetHomeBehaviour reverse patch not applied");
        }
        // CommonBuildingAI.GetVisitBehaviour reverse patch
        [HarmonyReversePatch, HarmonyPatch("GetVisitBehaviour")]
        public static void GetVisitBehaviour_Reverse(CommonBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            Debug.Log($"{ShowIt2Patcher.HarmonyId}: ERROR GetVisitBehaviour reverse patch not applied");
        }
        // CommonBuildingAI.GetWorkBehaviour reverse patch
        [HarmonyReversePatch, HarmonyPatch("GetWorkBehaviour")]
        public static void GetWorkBehaviour_Reverse(CommonBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            Debug.Log($"{ShowIt2Patcher.HarmonyId}: ERROR GetWorkBehaviour reverse patch not applied");
        }
    }

} // namespace
