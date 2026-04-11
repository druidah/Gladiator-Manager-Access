# Gemini Mandates for Accessibility Mod Projects

This file defines foundational mandates for Gemini CLI and other agents working in this repository. These instructions take absolute precedence over general defaults.

## User Persona & Communication
- **User:** Blind, screen reader user.
- **Output:** NO tables (pipe `|` symbols). Use Markdown lists or headings for structure.
- **Tone:** Professional, direct, and concise. Senior peer programmer.
- **Interaction:** Explain intent briefly before tool calls. Update `project_status.md` on significant progress and before session end.

## Environment & Tools
- **OS:** Windows. ALWAYS use Windows-native commands (PowerShell/cmd): `copy`, `move`, `del`, `mkdir`, `dir`, `type`, backslashes in paths. NEVER use Unix commands (`cp`, `mv`, `rm`, `cat`).
- **Build/Deploy:** Use `.\scripts\Build-Mod.ps1` and `.\scripts\Deploy-Mod.ps1`. Avoid raw `dotnet build`.
- **Search:** Use `grep_search` and `glob` to analyze the `decompiled/` directory.

## Engineering Standards
- **Source Code First:** NEVER guess class or method names. Always search `decompiled/` and verify against `docs/game-api.md`.
- **Architecture:** 
  - Follow the `[Feature]Handler` pattern (one class per screen/feature).
  - Private fields: `_camelCase`.
  - XML Documentation: `<summary>` required for all public classes/methods.
- **Localization:** Mandatory from day one. ALL screen reader strings MUST use `Loc.Get()`.
- **Principles:**
  - **Playability:** Aim for the same experience as sighted players. No cheats/shortcuts unless unavoidable and approved.
  - **Efficiency:** Cache object *references* (GameObjects, Components), but always read *values* (text, health, counts) live from the source.
  - **Robustness:** Use `DebugLogger` for internal state and `ScreenReader` for user announcements.

## Workflows
1. **Research:** Map the game's internal systems in `decompiled/`. Document findings in `docs/game-api.md`.
2. **Strategy:** Update the feature plan in `project_status.md`.
3. **Execution:**
   - **Plan:** Detail the implementation and testing strategy.
   - **Act:** Surgical edits. Include tests/verification.
   - **Validate:** Build and verify behavior.
4. **Completion:** Update `project_status.md` and suggest starting a new session if context is high.

## Reference Guides
- `project_status.md` — Central tracking (READ FIRST).
- `docs/ACCESSIBILITY_MODDING_GUIDE.md` — Core patterns and rules.
- `docs/game-api.md` — Discovered game keys, methods, and UI patterns.
- `docs/localization-guide.md` — Localization implementation.
- `docs/state-management-guide.md` — Handling multiple handlers.
- `docs/unity-reflection-guide.md` — Working with private game fields.
