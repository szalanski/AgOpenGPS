---
description: Update docs/implementation/ based on completed workflow plan and merged code
---

You are helping the user capture what actually shipped. Use the workflow plan for context, but always let the current source code win if there is any mismatch.

# Step 1: Collect Context

1. Ask which workflow (folder in `docs/workflow/NNN-name/`) was executed if it is not already clear.
2. Read `plan-*.md` plus any task files inside that workflow folder to understand the intended scope.
3. Identify the code areas that were touched. Ask the user if unsure, then open and review the relevant source files to confirm final behaviour.

# Step 2: Decide Documentation Scope

Work with the user to confirm which files under `docs/implementation/` need to be added or updated. Typical choices:
- `sections/system-architecture.md` for high-level component responsibilities.
- `sections/gnss-capabilities.md` for positioning concepts and health rules.
- `sections/communication-protocols.md` for UDP and SignalR contracts.
- `sections/operational-workflows.md` for end-to-end process descriptions.
- `sections/simulator-architecture.md` for simulation behaviours and limits.
- ADR files inside `docs/implementation/adrs/` when a new decision is made or an existing decision changes.
- Any new area should land in `sections/` (and be surfaced from `README.md`) unless it is an ADR.

# Step 3: Draft Updates

1. Produce clear, high-level prose in Markdown; no code blocks unless the user explicitly wants them.
2. Reference concrete files and line numbers using the repo-relative format (`SourceCode/AgOpenGPS.Api/...:42`).
3. When describing behaviour, prioritise what the code does today over what the plan said. Call out differences if they matter.
4. Keep everything ASCII only unless the file already uses other characters.

# Step 4: Apply Changes

1. Edit or add the necessary files under `docs/implementation/` (respecting the `sections/` and `adrs/` subfolders).
2. Update `docs/implementation/README.md` so the navigation reflects new or renamed documents.
3. Run a final read-through to ensure consistency, typos, and alignment with the code.
4. If an ADR was created or updated, verify numbering and links (including `docs/implementation/README.md`) are consistent.

# Step 5: Report Back

Summarise the documentation updates you made, the areas covered, and any open questions or follow-up actions you noticed while reviewing the code.
