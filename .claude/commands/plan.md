---
description: Create a new workflow plan in docs/workflow/ following the established pattern
---

You are helping the user create a new workflow plan for the AgOpenGPS backend migration project.

# Step 1: Read Context

Start by reading `docs/README.md` to understand the documentation structure and patterns.

# Step 2: Find Next Workflow Number

Check the `docs/workflow/` directory to find the highest existing workflow number (e.g., if `001-backend-tick-foundation` exists, the next is `002`).

# Step 3: Determine Objective

Identify the workflow objective. If the conversation already makes it clear, restate it to confirm alignment. Otherwise ask: "What is the objective of this workflow?"

Wait for their response.

# Step 4: Build Shared Context

Use the conversation to understand scope, architecture, dependencies, and risks before drafting the plan. Confirm that you understand both what needs to be done and how it should be approached.

Gather the following information, but only ask about items that are missing or unclear. Ask one focused question at a time and adapt based on the user's answers:

- Core goal (1-2 sentences)
- Current state of the relevant system or process
- Target state after the workflow
- Key benefits or drivers for the change
- Explicit non-goals/out-of-scope items
- Architectural or integration considerations (APIs, services, protocols, data flows)
- Constraints, risks, or sequencing concerns
- Expected implementation tasks (count, names, brief goals, high-level steps, test guidance)

If the user indicates tasks > 0, for each task ask (only for missing details):
- "Task N name? (e.g., 'create-api-project')"
- "Task N goal? (1 sentence)"
- "Task N key steps? (3-7 bullet points, high-level only)"
- "Task N how to test? (optional, 1-2 sentences)"

# Step 5: Choose Workflow Name

Derive a short, descriptive workflow name in `lowercase-with-dashes`. If the user has not provided one, propose a name that reflects the objective and confirm it briefly with them.

# Step 6: Create Structure

Create the workflow folder and files:

**Folder**: `docs/workflow/NNN-workflow-name/`

**Files to create**:

1. **plan-workflow-name.md** - Follow this structure:
```markdown
# [Workflow Title]

## Goal

[1-2 sentence goal]

## Current State

[Brief description of current state]

## Target State

[Brief description of target state]

## Why

- Benefit 1
- Benefit 2
- Benefit 3

## What This Is NOT

- Anti-goal 1
- Anti-goal 2
- Anti-goal 3

## Migration Path (if applicable)

[Optional: phases/steps if this is gradual migration]

## Tasks

1. [task1-name.md](task1-name.md) - Short description
2. [task2-name.md](task2-name.md) - Short description
...

(Or "No separate tasks - implementation described below" if tasks = 0)

## Success Criteria

- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3
```

2. **taskN-name.md** (if tasks > 0) - Follow this structure:
```markdown
# Task N: [Task Title]

## Goal

[1 sentence goal]

## Steps

1. Step 1 (high-level, NO CODE)
2. Step 2
3. Step 3
...

## Key Points

- Important note 1
- Important note 2

## Acceptance

- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3
```

# CRITICAL RULES

- **NEVER write code in markdown files**
- **NO code snippets, examples, or implementations**
- **NO detailed "how to" - only high-level "what to do"**
- Only high-level steps and concepts
- Tell WHAT to do, not HOW
- Keep guidance short and actionable
- The user will read actual code when implementing

# Step 7: Confirm Completion

After creating all files, tell the user:

"Created workflow NNN-workflow-name with:
- plan-workflow-name.md
- N task files (or 'plan-workflow-name.md only' if no tasks)

Location: docs/workflow/NNN-workflow-name/

You can now start implementation by following the plan."

# Example Reference

Look at `docs/workflow/001-backend-tick-foundation/` for the pattern to follow:
- Short plan.md with clear structure
- Task files with goal, steps, acceptance criteria
- NO CODE in any markdown files
- High-level and actionable
