# Project Status: GladiatorManagerAccess

## Project Info

- **Game:** Gladiator Manager
- **Engine:** Unity
- **Architecture:** 64-bit
- **Mod Loader:** MelonLoader (Recommended)
- **Runtime:** (To be determined from log)
- **Status:** Complete Core Loop

## Core Features Implemented

- **Full Controller/Keyboard Support:** Native arrows and WASD for all menus.
- **Dynamic Localization:** Integration with game's `Loc.Get()`.
- **Screen Reader Hooks:** Intercepts `Debug.Log` and `ReceiveChatMessage`.
- **Harmony Patches:** Deep integration with combat logic and UI.

## Recent Changes

- **Simplified Shortcut System:** Removed the overloaded `F2` and `Ctrl+F2` shortcuts in favor of a more direct slot-based system.
    - **Unit Stats Shortcuts:** Press `Ctrl + 1` through `Ctrl + 6` to read out the stats of your fighters in those slots. Press `Alt + 1` through `Alt + 6` to hear the stats of enemy gladiators in the corresponding slots. This works during Team Selection, Placement, Combat, and on the Results screen.
    - **Home Screen Status:** The general status (Week, Year, Balance) on the Home screen is now mapped to `Tab` instead of `F2`, aligning it with other management screens.
    - **Automatic Results Stats:** The detailed battle statistics list is now active by default on the Results screen. Use Up and Down arrows to read through the data immediately upon fight completion.
- **Barracks Status Update:** Added budget spending information to the status display. Players can now hear their current weekly salary total and their allocated salary budget alongside gold and fighter counts.
- **Centralized Class Descriptions:** Moved all gladiator class descriptions to `Loc.cs` to ensure consistency across the mod.
- **Fixed Freestyler Description:** Resolved an issue where the Freestyler class would incorrectly show the Gladiator description in the barracks.
- **Combat Leadership & Placement Accessibility:** Implemented a full accessible flow for the initial combat phase. 
    - **Leadership Roll Announcement:** The mod now detects and clearly announces "Leadership Victory" or "Leadership Defeat" based on the initial roll, letting players know if they can arrange their formation.
    - **Dedicated Placement Mode:** When victorious, players enter a placement mode where they can navigate slots 1-6 using Left/Right arrows.
    - **Enemy Awareness:** The placement mode now announces which enemy gladiator is facing the current slot, allowing for tactical positioning.
    - **Gladiator Swapping:** Use Space to select a gladiator and Space again on another slot to swap them. The mod now fully syncs the game's internal state and UI (highlights, cards, and targets).
    - **Tactical Overview:** Press F2 during placement to hear the full tactical map of both teams. (Note: replaced by Ctrl/Alt + 1-6 in latest update)
    - **Confirm Placement:** Press Enter to finalize the formation and start the battle.
    - **Enemy Class Advantages:** Pressing Alt + 1-6 on the Team Selection and Combat screens now includes the enemy gladiator's "Strong against" and "Vulnerable to" information, helping players make better tactical choices.
- **Fixed Gladiator Hiring & Sacking (v2):** Resolved a persistent issue where hiring from the Barracks would sometimes target the wrong gladiator slot.
    - **Grid Index Alignment:** Discovered that the game uses a split `rearrangeGrid` where recruited gladiators start at index 1 and hirable gladiators are added from index 24 downwards.
    - **Direct Grid Mapping:** Updated `BarracksHandler.RefreshLists` to use `Populator.rearrangeGrid` directly rather than assuming a sequential layout. This ensures that the `gladiatorDisplayID` passed to the game's recruitment logic always points to the intended gladiator.
    - **Navigation Order:** Adjusted the hirable list navigation to correctly handle the game's reverse-order placement of new recruits in the grid.
- **Global Popup Accessibility:** Implemented a new `PopupHandler` that automatically intercepts and reads all modal dialogues in the game (opened via `ZUIManager`).
    - **Automatic Reading:** When a popup appears (e.g., random events, achievements, tutorials), the mod now clearly announces the title and body text.
    - **Simplified Dismissal:** Players can now dismiss any standard popup by pressing Enter, Space, or Escape.
    - **State Management:** Popups now have a dedicated input state to prevent background keys from triggering during a modal.
- **The Cup (Tournament) Support:** Updated the `LeagueHandler` to fully support the Cup view starting from week 23.
    - **Bracket Visibility:** Players can now navigate through all six rounds of the Cup to see their draws and bracket progression.
    - **Detection Fix:** Resolved an issue where the accessibility handler would disable itself during Cup weeks.
- **Detailed Armour & Injury Access:** Added deep access to gladiator health and equipment status.
    - **Combat Tactics:** During combat and placement, press `Shift + 1 to 6` (Player) or `Shift + Alt + 1 to 6` (Enemy) to hear a detailed report of damaged armour parts and active injuries. This allows for informed tactical targeting.
    - **Barracks Profile:** The Gladiator Profile now includes a "Status Details" section listing every active injury (with its specific stat penalties and recovery time). Note: Damaged armor is no longer shown here as it is automatically repaired weekly (and paid for at the end of the week).
    - **Injury Mechanics:** Descriptions now include the exact impact on attributes (e.g., "Ini -5, Str -2") and whether the injury is temporary or permanent.
- **Modal Box & Decision Support:** Extended the popup system to handle `UIModalBox` elements.
    - **Automatic Reading:** When a decision box appears (e.g., deleting a character), the mod now reads both the primary and secondary text fields.
    - **Interactive Decisions:** Players can now use **Enter** to confirm and **Escape** to cancel a modal box, with the mod correctly triggering the game's internal events.
- **Architectural Polish:** Cleaned up the internal state management.
    - **Dedicated States:** Added `SaveScreen` and `PerkScreen` to the `AccessStateManager` enum, replacing the temporary use of the `Inventory` state.
    - **Refined Transitions:** Updated all handlers and the main mod loop to use these specific states, ensuring more reliable context-sensitive help and input handling.
- **Battle Log Access on Results Screen:** Players can now press F3 (Narrative Log) or Shift+F3 (Detailed Log) while on the Battle Results screen to review the entire fight history before returning to the Home screen.
- **Enhanced Combat Statistics:** The Results screen now includes XP gain for each gladiator and total money earned (Gate Receipts and Kill Bounties). These are read line-by-line using Up/Down arrows.
- **Week End Summary Screen:** Added a comprehensive summary screen that appears after processing the week.
    - **Navigation:** Now fully navigable using Up and Down arrow keys to read line-by-line.
    - **Training Results:** Automatically calculates and lists all attribute gains for every gladiator.
    - **Financial Report:** Fixed timing issues where values were captured after battle results. Now accurately shows:
        - Net Weekly Income/Expense for the whole week.
        - Weekly Salaries (now includes all standard contracts).
        - Gate Receipts and Kill Bounties from the week's battle.
        - Armour repairs and new gladiator signing fees.
        - Total Balance.

    - **Event Integration:** Consolidates all weekly news, urgent alerts, and training updates into the summary.
    - **League Results:** Now includes a full report of every match across all five divisions. Players can hear the winning and losing teams, the final result, and the top-performing gladiators for each match.
    - **Flow Control:** Pauses the game at the end of the week processing. Press Space or Enter to continue.
    - **Fixes:** Improved input responsiveness by moving processing to the top of the update loop. Added support for Keypad Enter. Added robust re-entry into the Home state after dismissing the summary, resolving an issue where the user could get stuck with no active input handler.
- **Standardized Screen Reader Announcements:** Implemented a unified format across the entire mod to ensure a consistent experience for screen reader users.
    - **Global Format Update:** Updated `Loc.cs` and all `IAccessibleHandler` implementations to ensure a professional, comma-separated format.
    - **Index Information:** Restored "x of y" index information to all gladiator lists (Barracks, Market, Team Selection) and management screens (League, Records, Perks) to aid navigation.
    - **Barracks & Profile:** Gladiator profiles now include the weekly salary amount. Indices have also been added to the profile item list and training adjustment options for better orientation.
    - **Team Selection:** Added the gladiator's salary to the detailed stats (F3) view.
    - **Combat & Results:** Standardized placement slots, battle actions, and log entries to provide a cleaner narrative flow.
    - **Menu & Management:** Updated Home, Finance, Market, Records, League, and Options screens to use the standardized comma-separated format instead of parentheses.
    - **Creation & Options:** Ensured the Character Creation and New Game Options screens follow the standard: `{Name}. {Type}, {Value}.` (Indices removed where appropriate).
- **Fixed AI Fight Blocking:** Identified and resolved an issue where the mod's keyboard shortcuts were skipping the `TeamResults` scene after a battle, which prevented AI league matches from being simulated.
- **Improved Results Flow:** Updated `CombatHandler` and `HomeScreenHandler` to properly transition through `TeamResults` level, ensuring all league matches are calculated before the weekly summary and advance.
- **UI & Architectural Polish:**
    - **Finance Screen Cleanup:** Removed redundant "--- Finances ---" and "--- Armour Repair ---" decorative headers to speed up screen reader navigation.
    - **Week End Summary Optimization:** Removed all empty spacer lines and decorative division headers. The summary is now a concise, continuous list of training results, league matches, and news.
    - **Build System Cleanup:** Resolved a persistent MSB3245 build warning by removing a redundant reference to `SLS.Widgets.Table.dll`, which is already part of the main game assembly.
- **Enhanced Combat Slot Status:** Pressing `Ctrl + 1` through `Ctrl + 6` (Player) or `Alt + 1` through `Alt + 6` (Enemy) during actual combat now provides a comprehensive status update similar to the arrow key navigation.
    - **Live Data:** Now announces Name, Health (%), Energy (%), Control (%), current State (e.g., Alert, Stunned), and current Order.
    - **Engagement Awareness:** Specifically mentions which opponent the unit is currently engaged with in their slot.
- **Fixed Results Screen Navigation:** Resolved an issue where players could not use arrow keys to navigate the battle summary immediately after a fight.
    - **Automatic Transition:** The mod now correctly detects the "Fight Over" state and automatically enters "Statistics Mode," enabling Up/Down arrow navigation for the battle report.
    - **Reliable Detection:** Added multiple detection layers (Music, UI Script, Button Text) to ensure the results screen is always recognized.
- **Refined Combat Log Filtering:** The F3 Battle Log now explicitly includes "control" messages for better tactical awareness, while "boosted by" messages have been filtered out to reduce narrative noise. Technical dice rolls (e.g., "roll", "rolls", "has to roll") remain excluded from the F3 log but available in the Shift+F3 Detailed Log.
- **Enhanced Level Up Accessibility:** Fixed major accessibility issues on the Perk/Level Up screen.
    - **Expanded Element Detection:** The mod now correctly identifies and allows navigation of `UITalentSlot` elements, which were previously ignored because they did not inherit from the standard Unity `Selectable` class.
    - **Perk Information:** Added deep integration with `UISpellSlot` to fetch and announce the actual names and detailed descriptions of perks and attribute increases.
    - **Fixed "Back" Navigation:** Robustly handled the "Back to Barracks" button by explicitly triggering the `Hide()` method on the perk screen window, resolving an issue where players could get stuck.
    - **State Management:** Integrated the perk screen with the `AccessStateManager` to ensure clean transitions between the barracks and the level-up interface.
- **Project Path Update:** Updated all assembly references and deployment targets in `GladiatorManagerAccess.csproj` to point to the new game location: `C:\Program Files (x86)\Steam\steamapps\common\Gladiator Manager\`. Verified with a successful build and deployment to the new `Mods` folder.
- **Fixed Tactical Placement Input Conflict:** Resolved an issue where pressing Space during the placement phase would start the battle instead of swapping units.
    - **Harmony Patch:** Added a prefix patch to `FightProcessor.Update` that intercepts the Space key only while the game is in "Confirm Placement" mode, preventing the game from calling `ProcessFight()` prematurely.
    - **Confirmed Behavior:** Space can now be used exclusively for selecting and swapping unit slots during placement. Use Enter to confirm the formation and move to the action selection phase.
- **Fixed Empty Slot Announcements in Placement:** Resolved an issue where the mod would incorrectly announce the first gladiator when navigating to an empty slot during the formation phase.
    - **Active Slot Validation:** Updated the `CombatHandler` to explicitly check `pGladSlotActive` and `eGladSlotActive` flags before querying for gladiator indices.
    - **GetGladIntFromSlot Fix:** Discovered that the game's internal `GetGladIntFromSlot` method returns index 0 when no gladiator is found in a slot, leading to false announcements. Implemented a `SafeGetGladIntFromSlot` helper that correctly identifies empty slots as index 1000.
    - **Swap Logic Update:** Fixed the gladiator swapping logic to properly handle moving units to empty slots by manually updating the `pGladSlotActive` state, ensuring the UI and screen reader stay in sync.

- **Fixed Duplicate Perk Announcements:** Resolved an issue where perks (especially the class-specific starting perk) were announced twice on the Level Up screen.
    - **Identification:** Discovered that the `PerkHandler` was searching for all `UITalentSlot` objects in the scene, which included both the active level-up screen and the background gladiator profile.
    - **Restricted Search:** Updated the handler to only include elements that are children of the active `perkScreen` window, ensuring a clean and focused navigation experience.
- **Improved Level Up/Perks Access:** Fixed an issue where the Level Up button was only visible when a gladiator was ready for a level up.
    - **Contextual Actions:** The gladiator profile now always shows either "Level Up!" (if ready) or "Perks" (if not ready).
    - **Localization:** Both actions are fully localized using `Loc.Get()`.
    - **Seamless Navigation:** Selecting "Perks" opens the same Perk/Attribute screen, allowing players to review a gladiator's current perks and path at any time.

- **Fixed Options Toggles Persistence (v2):** Resolved a persistent issue where toggles in the options menu would not save correctly.
    - **Harmony Initialization:** Discovered and fixed a critical issue where Harmony patches were not being applied at startup. This ensures all mod hooks (Combat, Popups, Options) are now fully functional.
    - **Options Controller Patches:** Implemented dedicated Harmony patches for `OptionsController` methods (`MuteCrowd`, `MusicInBattle`, `HoldDetail`) to force an immediate database save whenever they are triggered.
    - **Robust Handler Logic:** Updated `OptionsHandler` to explicitly invoke game methods and provide a manual fallback for updating `DataManager` fields, ensuring settings persist even if the game's internal UI listeners fail.
    - **UI State Sync:** Added automatic `SetupDropDowns` calls when opening the options menu to ensure the accessible interface always matches the values stored in the save file.

## Current Status

- Core game loop is fully accessible and robust.
- Combat log is clean and focused on narrative events.
- Unit health and status tracking is comprehensive and real-time.
- **Weekly Flow Fix:** Resolved an issue where the Week End summary would not trigger or would not dismiss correctly. The mod now correctly intercepts the end of the week processing at progress step 19, pauses the game's transition to the Home screen, and presents the summary to the player. Pressing Space or Enter dismisses the summary and allows the game to complete its saving process, followed by an automatic return to the Home input state.
- **Handler Integration:** Properly registered the `WeekEndHandler` in the main mod controller for consistent state management.

## Mod Custom Hotkeys

- F1: Context Help
- Ctrl+F1: Game Internal Help
- Tab: Status (Home, Team Selection, Finance, etc.)
- Ctrl + 1 to 6: Read Fighter Stats in Slot 1-6 (Selection, Placement, Combat, Results)
- Alt + 1 to 6: Read Enemy Stats in Slot 1-6 (Selection, Placement, Combat, Results)
- F3: Selected Gladiator Stats (Selection) / Narrative Battle Log (Combat & Results)
- Shift+F3: Detailed Roll Log (Combat & Results)
- F4: Quick Simulate battle
- F12: Toggle debug mode

- **Fixed Combat Orders Refresh (Multiple Fighters):** Resolved an issue where combat orders (e.g., Engage, Disengage) would not clear and allow for movement (Left, Right) after a gladiator's opponent was killed or yielded.
    - **UI Refresh:** The accessibility handler now explicitly triggers the game's `SlotAssignor.SetUpSlots` method whenever actions are refreshed. This forces the game to recalculate the available actions based on the gladiator's *current* engagement state rather than relying on stale UI data.
    - **Navigation Update:** Improved unit navigation in the combat screen by using the game's `ShowGladiatorOnMouseOver` method, which correctly updates the side panel with the selected gladiator's live health, energy, and state.
    - **Engagement Awareness:** These changes ensure that once a fighter is free, the screen reader immediately offers the correct movement and positioning options, allowing for better tactical play in multi-unit battles.
- Tournament management accessibility.
- Injury management screen accessibility details.
