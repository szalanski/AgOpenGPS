---
description: Create a new workflow plan in docs/workflow/ that captures only the context and tasks required for implementation
---

You are helping the user create a workflow plan for the AgOpenGPS backend migration. Keep the interaction lean: get enough detail to start, create the plan files immediately, and then refine them together through fast iterations.

# Guiding Principles

- Ask only for context that changes implementation (objective, scope, dependencies, constraints, acceptance).
- Turn new information into written notes immediately so the user can correct or confirm.
- Create the workflow files as soon as you have a viable objective and initial task outline; leave TODO markers where detail is missing.
- Make each plan and task file stand on its own: capture the minimum background, scope, and dependencies someone needs without reading the conversation.
- Stay high-level - no code, no detailed instructions - but make each task actionable.
- Revisit the plan often. Each new detail should trigger an edit to the existing files instead of waiting for a big batch update.
- Treat the plan and task files as shared scratchpads - keep them lightweight, current, and implementation-focused.

# Working Loop

1. **Bootstrapping** - Read `docs/README.md` and skim similar entries in `docs/workflow/` to match tone and structure.
2. **Quick Objective & Setup** - Confirm the workflow objective, rough scope, and any obvious constraints or dependencies. If the name/number is clear enough, propose it and get light approval.
3. **Seed the Files Early** - Create the workflow folder and minimal `plan-*.md` (plus task stubs if obvious) using the templates. Capture what you know, include any context needed for standalone reading, and mark unknowns with TODO notes.
4. **Iterative Refinement** - Whenever new information arrives, update the existing plan/task files immediately: adjust scope, flesh out context and actions, add acceptance checks, prune outdated notes.
5. **Shared Check-ins** - Periodically summarize the current plan back to the user, verify alignment, and keep refining until both objective and tasks feel implementation-ready.

# File Creation Workflow

Create `docs/workflow/NNN-workflow-name/` using the next number and a lowercase objective name, then drop `plan-workflow-name.md` (and any task stubs) inside it right away. Treat those files as the live plan, updating them in place as new context arrives and clearing TODOs once resolved.

# Templates

Use these scratchpad-style templates. Add or trim bullets to match the context, but keep things high-level and implementation-oriented.

````markdown
# {Workflow Title}

## Context
- Objective: {one sentence}
- Current state: {phrase}
- Constraints / dependencies: {bullets for anything that affects execution}
- Extra notes: {links, reminders, open questions}

## Task Board
- [task-one.md](task-one.md): {one-line summary or outcome}
- {Add more task entries as needed}

## Success Criteria
- [ ] Criterion 1
- [ ] Criterion 2
````

````markdown
# Task: {Task Title}

Objective: {sentence describing the outcome}

Context:
- {fact, dependency, or rationale}
- {add bullets as needed}

Plan:
- {high-level action or experiment}
- {another action}
- {optional scratch notes or TODOs}

Definition of Done:
- [ ] Criterion 1
- [ ] Criterion 2

Scratchpad:
- {loose notes, links, follow-ups}
````


# Completion Message Template

```
Created workflow NNN-workflow-name with:
- plan-workflow-name.md
- {N} task file(s) / 'plan-workflow-name.md only'

Location: docs/workflow/NNN-workflow-name/
Ready for implementation.
```

