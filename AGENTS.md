# Repository Guidelines

## Project Structure & Module Organization
The mod is a single C# project targeting `net472`. Most production code lives in the repository root as feature handlers such as `BarracksHandler.cs`, `CombatHandler.cs`, and `OptionsHandler.cs`. Shared infrastructure includes `Main.cs`, `AccessStateManager.cs`, `ScreenReader.cs`, `DebugLogger.cs`, and `Loc.cs`. Supporting material lives in `docs/`, `docs-de/`, `scripts/`, and `templates/`.

## Build, Test, and Development Commands
Use the PowerShell scripts instead of ad hoc build steps.

- `.\scripts\Build-Mod.ps1 -Configuration Debug` builds `GladiatorManagerAccess.csproj`.
- `.\scripts\Deploy-Mod.ps1 -Configuration Debug` builds and relies on the project’s `CopyToMods` target to place the DLL in the game’s `Mods` folder.
- `.\scripts\Test-ModSetup.ps1 -GamePath "C:\Program Files (x86)\Steam\steamapps\common\Gladiator Manager" -Architecture x64` validates MelonLoader, Tolk DLLs, references, and project setup.

## Coding Style & Naming Conventions
Follow `.editorconfig`: 4-space indentation for C# and PowerShell, CRLF endings, and Allman braces. Keep handler classes in the `[Feature]Handler` pattern and private fields in `_camelCase`. Public classes and methods should include XML `<summary>` comments. All screen-reader-visible strings must go through `Loc.Get()`; do not hardcode user-facing text in handlers.

## Testing Guidelines
There is no dedicated unit test project in this repository. Minimum validation is:

- Run `.\scripts\Build-Mod.ps1`.
- Run `.\scripts\Test-ModSetup.ps1` after changing references or deployment behavior.
- Manually verify affected flows in game, especially keyboard navigation, state transitions, and spoken output for NVDA/JAWS users.

Document notable findings in `project_status.md` when work changes behavior or leaves follow-up work.

## Commit & Pull Request Guidelines
Current history uses short, plain-English commit subjects such as `Update README.md` and `Made the source public.` Prefer concise imperative summaries under 72 characters. Pull requests should describe the accessibility change, list affected screens, and note manual test coverage.

## Accessibility & Contributor Notes
This project exists for blind screen-reader users. Keep output linear, concise, and keyboard-first. Before changing game integrations, check `docs/game-api.md`, `docs/ACCESSIBILITY_MODDING_GUIDE.md`, `GEMINI.md`, and `CLAUDE.md`.

- When referencing files in discussion, use repo-relative paths with the repository root as the base.
- When listing multiple files, give each file its own list item and place any explanatory note for that file on the line directly above or below that specific file entry to keep screen reader navigation clear.

## Codex Workflow Notes
For Codex work in this repository, follow the durable repo rules from `CLAUDE.md` and `GEMINI.md`:

- Read `project_status.md` before continuing ongoing feature work.
- Use Windows-native PowerShell commands and the scripts in `scripts\`.
- Treat `decompiled\Assembly-CSharp\` as the source of truth for game classes and method names; verify against `docs\game-api.md` before guessing.
- Prefer targeted searches when inspecting large decompiled files.
- Keep logs, comments, and contributor-facing documentation in English.
