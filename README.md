# Gladiator Manager Access Mod

Gladiator Manager Access is a comprehensive accessibility mod designed to make Gladiator manager fully playable for blind and screen reader users. It provides deep integration with screen readers (like NVDA and JAWS) and implements a standardized keyboard navigation system across all game screens.

## What you will need:
* Melon loader(https://github.com/LavaGang/MelonLoader.Installer/releases/)
* Gladiator Manager (https://store.steampowered.com/app/1251970/Gladiator_Manager/)
* This mod
* A love for number crunching
* A love for turn based, 1v1 through 4v4 tactical combat.

## Installation

To install the mod, follow these steps:

1.  **Install MelonLoader:**
    *   Download the MelonLoader Installer from [LavaGang's GitHub](https://github.com/LavaGang/MelonLoader.Installer/releases).
    *   Run the installer and add the game if MelonLoader detects it, if not: Click on add game manually, and select the `GladiatorManager.exe` file in your game directory.
    *   I tested this with 0.7.2. Select that and click Install.

2.  **Install the mod itself to the game
    *   Download the latest release from this repository, direct link: https://github.com/druidah/Gladiator-Manager-Access/releases/download/v0.51/Gladiator-Manager-Access.zip.
    *   Copy `Tolk.dll` and `nvdaControllerClient64.dll` from the downloaded zip into your main game folder (where `GladiatorManager.exe` is located).
    *   Place the DLL into the `Mods` folder inside your game directory. (Create the folder if it doesn't exist yet, or just run the game with MelonLoader installed once).

3.  **Verify Installation:**
    *   Launch the game. A console window should appear indicating that MelonLoader is starting and the mod is being loaded.
	*   Once the game reaches the main menu, your screen reader should announce "Splash screen screen loaded.".

---

## How to Use

### 1. Core Navigation
The mod follows a standardized navigation pattern across all screens:
*   **Up / Down Arrows:** Navigate through lists, menu items, or gladiator profiles.
*   **Left / Right Arrows:** Adjust sliders, cycle dropdown options, or navigate tactical slots in combat.
*   **Enter / Space:** Confirm selections, click buttons, or toggle options.
*   **Escape:** Go back, close sub-menus, or cancel a selection.
*   **F2:** Hear a status report (current year, week, money.) If you are in the barracks it will tell you how much you're spending on salaries and what's the budget.
*   **F1:** Context-sensitive mod help for your current screen.
* Tab: Switches from your gladiators to hirable ones in the barracks.
### 2. Management & The Barracks
*   **Barracks (Gladiator Profiles):** Use Up/Down to hear name, origin and class. Press Enter to open a detailed profile.
    *   **Attributes:** Use Up/Down to hear all 11 attributes.
    *   **Status Details:** The bottom of every profile lists active injuries (with penalties and recovery time).
    *   **Perks & Leveling:** Select "Level Up!" to choose new perks. Descriptions are read automatically as you navigate.

### 3. Combat
Combat is divided into four phases:
*   **Phase 1: Team Selection:** Assign gladiators to your team. Use `Alt + 1` to `6` to scout enemies and hear their class advantages.
*   **Phase 2: Leadership & Placement:** If you win the Leadership Roll, use Left/Right arrows to move between slots. Press `Space` to pick up a unit and `Space` again on another slot to swap them. Press `Enter` to confirm.
*   **Phase 3: Action Selection:** Cycle through orders (Charge, Engage, Rest) using Up/Down. For the "Target" action, hold `Ctrl + Up/Down` to choose a body part.
*   **Phase 4: Battle Logs:** Press `F3` for a Narrative log or `Shift + F3` for a Detailed log (including dice rolls).

**Tip:** Use `Shift + 1 to 6` (Player) or `Shift + Alt + 1 to 6` (Enemy) to hear which armour parts are broken before targeting.


---

## Hotkey Reference Summary

*   **F1:** Context-Sensitive Help
*   **F2:** Status Report (Gold, Week, Salaries, reputation)
*   **Ctrl + 1 to 6:** Read Fighter Stats in Slot
*   **Alt + 1 to 6:** Read Enemy Stats in Slot
*   **Shift + 1 to 6:** Read Detailed Armour/Injuries (Player)
*   **Shift + Alt + 1 to 6:** Read Detailed Armour/Injuries (Enemy)
*   **F3:** Narrative Battle Log / Selected Gladiator Stats
*   **Shift + F3:** Detailed battle Log
*   **F4:** Quick Simulate Battle
*   **F12:** Toggle Debug Mode 
