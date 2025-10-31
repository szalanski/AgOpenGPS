---
description: Create a workflow plan in docs/workflow/ that stays lean, actionable, and continuously updated
---

Capture enough context to start, create the workflow files immediately, and refine them in place as the conversation evolves.

## Principles

- Ask only for information that changes implementation (objective, scope, dependencies, constraints, definition of done).
- Write down new facts at once; use TODOs for gaps.
- Make plan and task files self-contained scratchpads—no code, just actionable guidance.
- Update the files every time context shifts; remove stale notes promptly.

## Loop

1. Read `docs/README.md`
2. Ask user for the objective
3. Confirm objective, scope boundary, key dependencies, constraints, and success signal.
4. Create the workflow folder and minimal files as soon as you have a workable outline.
5. Revise plan/tasks with each new detail until implementation feels clear.
6. Recap the latest plan to the user and adjust as needed.

## File Workflow

Create `docs/workflow/NNN-workflow-name/` using the next number plus a lowercase objective name. Drop `plan-workflow-name.md` (and any task stubs) there immediately; treat them as the live source of truth and keep editing in place.

## Templates

Use these scratchpad templates; trim or expand sections to fit the work.

```markdown
# {Workflow Title}

## Context

- Objective: {one sentence}
- Current state: {phrase}
- Constraints / dependencies: {bullets}
- Notes: {links, reminders, open questions}

## Tasks

- [task-one.md](task-one.md): {outcome or deliverable}
- {More tasks as needed}

## Success

- [ ] Criterion 1
- [ ] Criterion 2
```

```markdown
# Task: {Task Title}

Objective: {desired result}

Context:

- {fact, dependency, rationale}

Plan:

- {high-level action or experiment}
- {additional action or TODO}

Definition of Done:

- [ ] Criterion 1
- [ ] Criterion 2

Scratchpad:

- {notes, links, follow-ups}
```
