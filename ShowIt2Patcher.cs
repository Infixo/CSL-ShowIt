using UnityEngine;
using HarmonyLib;

namespace ShowIt2
{
    public static class ShowIt2Patcher
    {
        public const string HarmonyId = "Infixo.ShowIt2";
        private static bool patched = false;

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

    [HarmonyPatch(typeof(ZonedBuildingWorldInfoPanel))]
    public static class ZonedBuildingWorldInfoPanel_Patches
    {
        /*
        [HarmonyPostfix, HarmonyPatch("UpdateBindings")]
        public static void UpdateBindings_Postfix()
        {
            // Currently selected building.
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            Debug.Log($"{ShowIt2Patcher.HarmonyId}.UpdateBindings_Postfix: id={buildingID}");
        }
        */
        /*
        [HarmonyPostfix, HarmonyPatch("Start")]
        public static void Start_Postfix(ZonedBuildingWorldInfoPanel __instance)
        {
            // creates and initializes UI controls
            //ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            //Debug.Log($"{ShowIt2Patcher.HarmonyId}.Start_Postfix: id={buildingID} building={__instance.buildingName} component={__instance.component.name}");
            // Infixo.ShowIt2.Start_Postfix: id=0 building=Name component=(Library) ZonedBuildingWorldInfoPanel
            //ShowIt2Mod.Panel.CreateUI(); // the object is not yet created here
        }
        */

        [HarmonyPostfix, HarmonyPatch("OnSetTarget")]
        public static void OnSetTarget_Postfix(ZonedBuildingWorldInfoPanel __instance)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            Debug.Log($"{ShowIt2Patcher.HarmonyId}.OnSetTarget_Postfix: id={buildingID} building={__instance.buildingName}");
            // Infixo.ShowIt2.OnSetTarget_Postfix: id=17609 building=Nylons Galore
            ShowIt2Mod.Panel?.RefreshData(); // ?. is just a failsafe
        }
    }

} // namespace
