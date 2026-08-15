# PhrasePad — Plan

## Scope

A cross-platform (Windows 10/11 + macOS), privacy-first, system-wide text expander and snippet manager.

**In scope**
- System-wide trigger detection and inline text replacement in any focused application.
- Snippet library with groups, triggers, and rich plain-text/formatting-lite expansions.
- Dynamic tokens: `{date:...}`, `{time:...}`, `{clipboard}`, `{cursor}`.
- Fill-in placeholder forms (`{field}`) with a quick prompt dialog.
- Desktop manager UI to create/edit/organize/test snippets.
- Headless `phrasepad` CLI for list/expand-preview/import/export.
- Optional, local-only AI authoring assistance (rewrite/generate/suggest-trigger).
- Local JSON/SQLite storage; import/export for backup and sharing.

**Out of scope (non-goals)**
- Cloud sync, accounts, subscriptions, telemetry.
- Running expansions through any AI at type-time (expansions are always deterministic).
- Mobile platforms; Linux is a possible later target, not a launch goal.
- Rich WYSIWYG document editing — expansions are text-first (with lightweight formatting where the target app supports it).
- Global macro/automation scripting engine (keep it focused on text expansion).

## Architecture / tech approach

- **Language/runtime**: .NET 8.
- **UI**: Avalonia (MVVM) for a single cross-platform manager UI. (WPF rejected — Windows-only.)
- **UI-free `PhrasePad.Core` library** (fully unit-testable, no OS UI deps):
  - `Snippet` model: `{ Id, Trigger, Expansion, GroupId, Enabled, Tokens[], Placeholders[] }`.
  - `ISnippetStore` — load/save library (JSON file first; SQLite optional if scale requires).
  - `ITriggerMatcher` — efficient longest-trigger matching over a rolling keystroke buffer, with configurable trigger style (prefix char, whitespace-terminated, or explicit hotstring).
  - `IExpansionEngine` — resolves tokens (`{date}`, `{clipboard}`, `{cursor}`), fills placeholders, computes final text + caret offset + backspace count.
  - `ITokenResolver`, `IPlaceholderResolver`.
- **Platform expander adapters** behind `IInputHook` / `IKeystrokeSender`:
  - **Windows**: low-level keyboard hook (`SetWindowsHookEx WH_KEYBOARD_LL`) to observe typing; replacement via `SendInput` (backspaces + Unicode injection) or clipboard-paste strategy for large/rich text. Tray via NotifyIcon. Optional global hotkey (`RegisterHotKey`) to open the manager / pause expansion.
  - **macOS**: `CGEventTap` (requires Accessibility permission) to observe keystrokes; replacement via `CGEvent` keystroke synthesis or paste strategy. Menu-bar agent (`NSStatusItem`).
- **Replacement strategy**: default "backspace + retype" for reliability; automatic fallback to "set clipboard + paste + restore clipboard" for long/multiline/rich expansions, with a setting to prefer paste.
- **Storage location**: `%APPDATA%\phrasepad` (Windows) / `~/Library/Application Support/phrasepad` (macOS). JSON library + settings.
- **CLI**: `PhrasePad.Cli` sharing `PhrasePad.Core`; verbs `list`, `expand`, `import`, `export`, `--json` output for scripting.
- **Optional local-AI**: `ISnippetAiService` → OpenAI-compatible `localhost` endpoint (Ollama/llama.cpp). Reachability probe + graceful fallback. Sends only the snippet text the user is authoring; off by default; local-only.
- **Testing**: xUnit on `PhrasePad.Core` (matcher edge cases, token/placeholder resolution, caret math). Adapters kept thin behind interfaces so logic is testable without OS input.

## Milestones

1. **M1 — Core**: snippet model, `ISnippetStore` (JSON), `ITriggerMatcher`, `IExpansionEngine` with `{cursor}` + caret/backspace math. xUnit coverage.
2. **M2 — Windows expander**: `WH_KEYBOARD_LL` hook + `SendInput` replacement + tray + pause hotkey. End-to-end expansion in real apps.
3. **M3 — macOS expander**: `CGEventTap` + `CGEvent` synthesis + menu-bar agent + Accessibility onboarding flow.
4. **M4 — Manager UI**: Avalonia MVVM library/group tree, snippet editor, live expansion test pane, enable/disable, settings.
5. **M5 — Tokens & forms**: `{date}/{time}/{clipboard}` resolvers + placeholder fill-in dialog with ordered fields.
6. **M6 — CLI**: `list/expand/import/export` + JSON output; shared library format.
7. **M7 — Local-AI assist**: `ISnippetAiService` rewrite/generate/suggest-trigger with probe + fallback (off by default).
8. **M8 — Packaging & CI**: GitHub Actions matrix (windows-latest + macos-latest); Windows portable zip + MSIX; macOS universal `.app` + `.dmg`.

## Packaging / distribution

- **Windows**: self-contained `win-x64` portable zip + MSIX installer; auto-start-on-login option.
- **macOS**: universal (`arm64`+`x64`) `.app` bundled in a `.dmg`; documents Accessibility permission requirement; login-item option.
- **CI**: GitHub Actions building both targets on tag; attaches artifacts to Releases; runs `PhrasePad.Core` tests on every PR.
