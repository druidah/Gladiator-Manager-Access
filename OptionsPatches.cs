using HarmonyLib;
using UnityEngine;

namespace GladiatorManagerAccess
{
    /// <summary>
    /// Patches for the game's OptionsController to ensure settings are persisted immediately.
    /// </summary>
    [HarmonyPatch(typeof(OptionsController))]
    public static class OptionsPatches
    {
        [HarmonyPatch("MuteCrowd")]
        [HarmonyPostfix]
        public static void Postfix_MuteCrowd(OptionsController __instance)
        {
            Save(__instance);
        }

        [HarmonyPatch("MusicInBattle")]
        [HarmonyPostfix]
        public static void Postfix_MusicInBattle(OptionsController __instance)
        {
            Save(__instance);
        }

        [HarmonyPatch("HoldDetail")]
        [HarmonyPostfix]
        public static void Postfix_HoldDetail(OptionsController __instance)
        {
            Save(__instance);
        }

        [HarmonyPatch("SetupDropDowns")]
        [HarmonyPostfix]
        public static void Postfix_SetupDropDowns(OptionsController __instance)
        {
            if (DataManager.allTheSaveData == null || DataManager.allTheSaveData.Count == 0) return;
            var saveData = DataManager.allTheSaveData.Find(x => x.SaveID == 1);
            if (saveData == null) return;

            // Sync Toggles to the UI state from saved data
            if (__instance.muteToggle != null)
                __instance.muteToggle.isOn = saveData.SpareBool2;
            
            if (__instance.musicToggle != null)
                __instance.musicToggle.isOn = saveData.SpareInt4 == 1;

            if (__instance.detailHoldToggle != null)
                __instance.detailHoldToggle.isOn = saveData.SpareString4 == "true";
        }

        private static void Save(OptionsController instance)
        {
            if (DataManager.allTheSaveData == null || DataManager.allTheSaveData.Count == 0) return;
            
            var saveData = DataManager.allTheSaveData.Find(x => x.SaveID == 1) ?? DataManager.allTheSaveData[0];
            var db = DataManager.dbManager;
            var dm = Object.FindObjectOfType<DataManager>();

            if (saveData != null && db != null)
            {
                try
                {
                    // DIRECT SQL UPDATE for Toggles to bypass game's potentially flawed SaveSaveData
                    db.Execute("UPDATE SaveData SET SpareBool2 = ?, SpareInt4 = ?, SpareString4 = ? WHERE SaveID = ?",
                        saveData.SpareBool2 ? 1 : 0,
                        saveData.SpareInt4,
                        saveData.SpareString4,
                        saveData.SaveID);
                    
                    if (dm != null) dm.SaveSaveData();
                    DebugLogger.LogState("Options changed and saved via direct SQL.");
                }
                catch { }
            }
        }
    }
}
