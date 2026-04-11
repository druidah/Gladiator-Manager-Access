using System;
using UnityEngine;
using MelonLoader;

namespace GladiatorManagerAccess
{
    /// <summary>
    /// Central state manager for accessibility handlers.
    /// Solves the core problem: the same keys (arrows, Enter, Escape) need to do
    /// different things depending on what's currently active.
    /// </summary>
    public static class AccessStateManager
    {
        public enum State
        {
            None,
            MainMenu,
            Home,
            Barracks,
            Inventory,
            Shop,
            Finance,
            Help,
            Options,
            CharacterCreation,
            TeamSelection,
            Combat,
            League,
            Records,
            WeekEndSummary,
            Popup,
            SaveScreen,
            PerkScreen
        }

        public static State Current { get; private set; } = State.None;

        public static bool IsBattleDoneThisWeek { get; set; } = false;

        public static event Action<State, State> OnStateChanged;

        /// <summary>
        /// Try to enter a new state. Automatically exits the previous state if one is active.
        /// </summary>
        /// <returns>true if state was entered successfully</returns>
        public static bool TryEnter(State state)
        {
            if (state == State.None)
            {
                return false;
            }

            if (Current == state)
            {
                return true; // Already in this state
            }

            // Auto-exit previous state if one is active
            if (Current != State.None)
            {
                var previousState = Current;
                Current = State.None;
                OnStateChanged?.Invoke(previousState, State.None);
            }

            var oldState = Current;
            Current = state;
            DebugLogger.LogState($"Entered {state}");
            OnStateChanged?.Invoke(oldState, state);
            return true;
        }

        /// <summary>
        /// Exit from a state. Only exits if currently in that state.
        /// </summary>
        public static void Exit(State state)
        {
            if (Current != state) return;

            var oldState = Current;
            Current = State.None;
            DebugLogger.LogState($"Exited {state}");
            OnStateChanged?.Invoke(oldState, State.None);
        }

        /// <summary>
        /// Force exit from any state.
        /// </summary>
        public static void ForceReset()
        {
            if (Current != State.None)
            {
                var oldState = Current;
                Current = State.None;
                DebugLogger.LogState($"Force reset from {oldState}");
                OnStateChanged?.Invoke(oldState, State.None);
            }
        }

        public static bool IsIn(State state)
        {
            return Current == state;
        }
    }
}
