# PhrasePad

Local, privacy-first **system-wide text expander & snippet manager** for **Windows 10/11 & macOS**. Type a short trigger like `;addr` or `:sig` anywhere — in your browser, email, editor, chat — and PhrasePad instantly expands it into full text, complete with dynamic tokens (date/time, clipboard), fill-in placeholders, and precise cursor placement.

> Offline by default. No account, no cloud, no telemetry. Your snippets never leave your machine.

## Overview

PhrasePad watches for abbreviation triggers you define and replaces them with expansions in real time, in **any** application. It combines a fast, always-available background expander with a friendly desktop manager for organizing snippets into groups, editing content, and testing expansions.

Core pieces:
- **Background expander** — a lightweight tray/menu-bar agent that detects triggers and performs the replacement via synthetic keystrokes.
- **Snippet manager** — a desktop GUI to create/edit/organize snippets, groups, and settings.
- **`phrasepad` CLI** — headless import/export, list, and expand-preview for scripting and backups.

## Motivation

Everyone retypes the same things: email signatures, addresses, canned replies, boilerplate code, support macros, meeting notes headers. Cloud text-expanders exist, but they route your most personal snippets (addresses, phone numbers, replies) through third-party servers and require subscriptions. PhrasePad keeps everything **local and free**, works **offline**, and is **cross-platform**.

## Use cases

- **Email & support**: `;greet`, `;followup`, `;refund` → full canned replies with `{clipboard}` and `{date}` tokens.
- **Developers**: `;gpl`, `;shebang`, `;guid` → license headers, boilerplate, generated GUIDs.
- **Personal**: `;addr`, `;phone`, `;iban` → contact details typed once, reused everywhere.
- **Forms/templates**: `;invoice` → a multi-field fill-in form that prompts for `{name}`, `{amount}`, `{due}` and inserts the assembled text.
- **Writers**: reusable phrases, footnotes, and formatting snippets with cursor left exactly where you want to keep typing.

## How to use

### Windows 10/11 quickstart
1. Download the latest `phrasepad-win-x64.zip` from Releases (or build from source).
2. Unzip and run `PhrasePad.exe`. It starts in the system tray.
3. Open the manager (double-click tray icon), click **New Snippet**, set a trigger (`;addr`) and expansion text.
4. Switch to any app and type `;addr` — it expands instantly.

### macOS quickstart
1. Download `PhrasePad.dmg` from Releases (or build from source).
2. Drag **PhrasePad.app** to Applications and launch it (menu-bar app).
3. Grant **Accessibility** permission when prompted (System Settings → Privacy & Security → Accessibility) — required to detect typing and send keystrokes.
4. Add a snippet in the manager, then type your trigger in any app.

## Example workflow

```text
Trigger:   ;sig
Expansion: Best regards,
           Alex Rivera
           {date:yyyy-MM-dd}
```

Typing `;sig` in your mail client produces:

```text
Best regards,
Alex Rivera
2026-08-15
```

Placeholder form example:

```text
Trigger:   ;mtg
Expansion: Meeting notes — {topic}
           Attendees: {who}
           Action items:
           - {cursor}
```

Typing `;mtg` pops a small fill-in dialog for `topic` and `who`, then inserts the text with the cursor parked on the action-items line.

### CLI

```bash
phrasepad list                       # list all snippets
phrasepad list --json                # machine-readable library
phrasepad expand ";sig"              # preview with an ⟦cursor⟧ marker
phrasepad expand ";invoice" \
  --field name=Ada --field amount=42 # fill placeholders without typing
phrasepad export snippets.json       # back up your library
phrasepad import snippets.json       # merge without replacing conflicts
```

The CLI uses the same platform library as the GUI by default. Pass
`--library <path>` to work with a specific library file. `list`, `expand`,
`import`, and `export` accept `--json` where structured output is useful.
Import conflicts leave the existing snippet or group unchanged and return exit
code `4`; usage errors return `2`, missing triggers return `3`, and storage or
JSON errors return `1`.

## Local-AI integration (optional, off by default)

PhrasePad can optionally call a **local** small model to help you author snippets — never to run your expansions. When enabled and reachable, it talks to an OpenAI-compatible endpoint on `localhost` (e.g. **Ollama** or **llama.cpp**) using small models such as **Llama 3.2**, **Qwen2.5**, **Phi-3-mini**, or **MiniCPM** class.

- **Rewrite**: tighten/shorten/adjust tone of a snippet you're editing.
- **Generate**: describe a canned reply and get a draft snippet to save.
- **Suggest triggers**: propose a short abbreviation for new snippets.

All AI features are **opt-in**, run **locally**, include a reachability probe, and degrade gracefully to manual editing if no model is available. Expansion at type-time is always deterministic and never depends on AI.

## Current status / milestones

Early scaffold. Tracking issues drive incremental delivery:

- [ ] M1 — Core snippet model + storage + trigger matching engine
- [ ] M2 — Windows background expander (hook + keystroke injection)
- [ ] M3 — macOS background expander (Accessibility + CGEvent)
- [ ] M4 — Snippet manager desktop UI (Avalonia)
- [ ] M5 — Dynamic tokens + placeholder fill-in forms
- [ ] M6 — `phrasepad` CLI (import/export/list/expand)
- [ ] M7 — Optional local-AI authoring assist
- [ ] M8 — Packaging & CI (Windows zip/MSIX, macOS .app/.dmg)

See [PLAN.md](./PLAN.md) for scope, architecture, and non-goals.
