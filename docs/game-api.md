# Game API Documentation: Gladiator Manager

## Overview

- **Game Name:** Gladiator Manager
- **Engine Version:** Unity (detected)
- **Architecture:** 64-bit

## Singleton Access Points

- **ZUIManager.Instance:** Main UI manager for the game's custom UI.
- **SFXManager.Instance:** Sound effects manager.
- **SteamManager.Instance:** Steam integration.
- **DuloGames.UI.UIWindowManager.Instance:** Handles UI windows (open/close).
- **DuloGames.UI.UITooltipManager.Instance:** Tooltip management.
- **DuloGames.UI.UISceneManager.instance:** Scene management.
- **DuloGames.UI.UIItemDatabase.Instance:** Database for items.
- **DuloGames.UI.UISpellDatabase.Instance:** Database for spells/actions.

## Game Key Bindings (Original)

| Key | Description | Context |
|-----|-------------|---------|
| A | Assign Slots | Combat (SlotAssignor) |
| S | Show Card Attributes | Combat (SlotAssignor) |
| D | Show Card Armour | Combat (SlotAssignor) |
| F | Show Card Health | Combat (SlotAssignor) |
| G | Show Card Stats | Combat (SlotAssignor) |
| Tab | Cycle Player Slots / Tabbing | Combat / UI (InputTabbing) |
| 1-9, 0 | Select Action (0-9) | Combat (SlotAssignor) |
| Z | Select Action (10) | Combat (SlotAssignor) |
| X | Select Action (11) | Combat (SlotAssignor) |
| Q, W, E, R, T, Y, U, I, O, P | Target Body Part | Combat (Targeting Mode) |
| [, ] | Target Body Part | Combat (Targeting Mode) |
| Space | Pause/Continue | Combat (FightProcessor) |
| +, - | Speed Up/Down | Combat (FightProcessor) |
| Escape | Close Menu / Back | UI (HelpScreen, ReportToggle) |
| Return | Submit Chat | UI (Demo_Chat) |

## Safe Mod Keys

- **Arrow Keys (Up, Down, Left, Right):** Not used for input/navigation in the game.
- **Function Keys (F1-F12):** Not used by the game.
- **Home, End, PageUp, PageDown:** Not used by the game.
- **Insert, Delete:** Not used by the game.
- **Right Control, Right Alt, Right Shift:** Game uses Ctrl/Alt/Shift generally, but safe to use for modifiers if needed.

## Mod Hotkeys (Accessibility Mod)

| Key | Description | Context |
|-----|-------------|---------|
| F1 | Context Help | All Screens |
| Ctrl+F1 | Game Internal Help | All Screens |
| Tab | Status (Week, Year, Balance, etc.) | Home, Team Selection, Barracks |
| Ctrl + 1 to 6 | Read Fighter Stats in Slot 1-6 | Selection, Placement, Combat, Results |
| Alt + 1 to 6 | Read Enemy Stats in Slot 1-6 | Selection, Placement, Combat, Results |
| F3 | Selected Gladiator Stats / Narrative Log | Selection / Combat & Results |
| Shift+F3 | Detailed Roll Log | Combat & Results |
| F4 | Quick Simulate | Placement & Combat |
| F12 | Toggle Debug Mode | All Screens |
| Space | Pause/Continue / Swap (Placement) | Combat / Placement |
| Enter | Select / Confirm | All Screens |
| Escape | Back / Cancel | All Screens |

## UI System Analysis

### UI Base Classes

- **ZUIElementBase:** The fundamental base class for all animated UI elements in the game's custom ZUI system.
- **Menu:** Inherits from `ZUIElementBase`. Represents a full screen or major menu. Managed by `ZUIManager`.
- **Popup:** Inherits from `ZUIElementBase`. Used for informational popups (e.g., `UpdateInformation(info, title)`).
- **SideMenu:** Inherits from `ZUIElementBase`. Used for side panels.
- **UIElement:** Inherits from `ZUIElementBase`. Individual animated elements (buttons, images, text holders).
- **DuloGames.UI.UIWindow:** A standard window class from the DuloGames UI asset.

### Text Access Pattern

The game uses standard Unity `UnityEngine.UI.Text` components. Most important UI classes have **public** fields for their text components:

- **Popup:** `TitleHolder` (Text), `BodyHolder` (Text).
- **Populator:** Extensive list of public Text fields (e.g., `pgName`, `teamName`, `balance`, `status`).
- **Tips:** `tipBox` (Text).
- **Translator:** Static `language` field (string) for language detection.

Access pattern is typically: `someComponent.someTextField.text`. Since most are public, Reflection is rarely needed for basic text reading.

## Financial System

### Team Finances
The `Team` class contains the core financial data for both player and AI teams.
- `Money`: Current gold balance.
- `SalaryBudget`: Allocated weekly budget for gladiator salaries.
- `FeeBudget`: Allocated budget for recruitment signing fees.
- `CurrentSalaryTotal`: The sum of all recruited gladiator salaries (weekly spending).
- `SalaryCostThisYear` / `SigningFeesThisYear`: Cumulative annual spending.

### MoneyManager
The `MoneyManager` component handles UI updates and budget recalculations.
- `playerBalance` (static): Current player gold.
- `allTheTeams` (static): List of all teams in the game.
- `ReCalculateBudgets()`: Updates `SalaryBudget` and `FeeBudget` based on team wealth and settings.

## Help System Analysis

The help system uses an array of panels (`helpPanels`). The mapping depends on whether the `melee` flag is set on the `HelpScreen` instance.

| Index | Title (Manual) | Context |
|-------|----------------|---------|
| 0 | The Basics | General |
| 1 | Classes Overview | General |
| 2-7 | Specific Classes | Gladiator, Leader, Defender, Barbarian, Rogue, Retarius |
| 8 | Attributes Overview | General |
| 9 | Attributes: Skill | General |
| 10 | Attributes: Physical | General |
| 11 | Attributes: Mental | General |
| 12 | Armour, Injuries and Defeat | Melee Only |
| 13 | Attack Types | Melee Only |
| 12-15+ | Origins and Lore | Non-Melee |

Tutorial Archive is hardcoded in the mod but extracted from `TutorialPopUps.cs`. It contains over 30 topics covering Home, Barracks, League, Selection, and Combat mechanics.

## Game Mechanics

(Document game systems like combat, recruitment, etc.)

## Status Systems

(Document health, resources, stats)

## Localization

(Document game's language detection - English only for mod)

## Event Hooks (Harmony)

(Document useful methods for patching)
