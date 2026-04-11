using HarmonyLib;
using DuloGames.UI;
using MelonLoader;
using UnityEngine;

namespace GladiatorManagerAccess
{
    [HarmonyPatch(typeof(Demo_Chat), "ReceiveChatMessage")]
    public static partial class CombatPatches
    {
        public static event System.Action<int, string> OnMessageReceived;
        public static event System.Action OnFightEnd;

        public static void Postfix(int tabId, string text)
        {
            // Forward to anyone listening
            OnMessageReceived?.Invoke(tabId, text);
        }
    }

    [HarmonyPatch(typeof(FightProcessor), "FightEnd")]
    public static class FightEndPatch
    {
        public static void Postfix()
        {
            CombatPatches.TriggerFightEnd();
        }
    }

    public static partial class CombatPatches
    {
        public static void TriggerFightEnd()
        {
            OnFightEnd?.Invoke();
        }
    }

    [HarmonyPatch(typeof(HelpScreen), "Update")]
    public static class HelpScreenPatches
    {
        public static bool Prefix()
        {
            // If the mod is in Help state and NOT in the main menu level or viewing content,
            // we want to handle Escape ourselves to go back, so we block the game's Update.
            if (AccessStateManager.IsIn(AccessStateManager.State.Help))
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    // Let the HelpHandler handle it in its own Update/ProcessInput
                    return false; 
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Calendar), "AdvanceAWeek")]
    public static class CalendarPatches
    {
        public static void Postfix()
        {
            AccessStateManager.IsBattleDoneThisWeek = false;
            MelonLogger.Msg("Battle done flag reset for new week.");
        }
    }

    [HarmonyPatch(typeof(Calendar), "ToggleProcessing")]
    public static class CalendarToggleProcessingPatches
    {
        public static void Prefix()
        {
            // Call capture on the handler to get state BEFORE salaries are paid
            WeekEndHandler.Instance?.CaptureInitialState();
        }
    }

    [HarmonyPatch(typeof(Calendar), "Update")]
    public static class CalendarUpdatePausePatch
    {
        public static bool Prefix()
        {
            // If the summary is ready and active, we want to BLOCK the game's Calendar.Update
            // from finishing (which happens at progress 19).
            if (WeekEndHandler.Instance != null && 
                Calendar.startProcessingWeek && 
                Calendar.progress >= 19 && 
                AccessStateManager.IsIn(AccessStateManager.State.WeekEndSummary))
            {
                return false; // Skip game's update, effectively pausing it at step 19
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FightProcessor), "Update")]
    public static class FightProcessorUpdatePatch
    {
        public static bool Prefix(FightProcessor __instance)
        {
            // If we are in placement mode, we want to block the game's native Space handling
            // so the mod can use it for swapping units.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (__instance.continueText != null && __instance.continueText.text == "Confirm Placement")
                {
                    // Return false to skip the game's Update logic for this frame,
                    // preventing ProcessFight() from being called.
                    return false;
                }
            }
            return true;
        }
    }
}
