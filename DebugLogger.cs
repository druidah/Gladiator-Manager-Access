using MelonLoader;

namespace GladiatorManagerAccess
{
    /// <summary>
    /// Utility class for logging game state and input events.
    /// Only outputs when Main.DebugMode is true.
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Logs a screenreader announcement.
        /// </summary>
        public static void LogScreenReader(string text)
        {
            if (Main.DebugMode)
            {
                MelonLogger.Msg($"[Speech] {text}");
            }
        }

        /// <summary>
        /// Logs a user input event.
        /// </summary>
        public static void LogInput(string key, string action)
        {
            if (Main.DebugMode)
            {
                MelonLogger.Msg($"[Input] {key} -> {action}");
            }
        }

        /// <summary>
        /// Logs a game state change.
        /// </summary>
        public static void LogState(string state)
        {
            if (Main.DebugMode)
            {
                MelonLogger.Msg($"[State] {state}");
            }
        }

        /// <summary>
        /// Logs a UI navigation event.
        /// </summary>
        public static void LogNavigation(string element)
        {
            if (Main.DebugMode)
            {
                MelonLogger.Msg($"[Nav] {element}");
            }
        }
    }
}
