---
description: Create a new workflow plan in docs/workflow/ following the established pattern
---

You are helping the user create a new workflow plan for the AgOpenGPS backend migration project.

# Step 1: Read Context

First, read `docs/README.md` to understand the documentation structure and patterns.

# Step 2: Find Next Workflow Number

Check `docs/workflow/` directory to find the highest existing workflow number (e.g., if 001-backend-tick-foundation exists, next is 002).

# Step 3: Ask for Objective

Ask the user: "What is the objective of this workflow?"

Wait for their response.

# Step 4: Interactive Planning

Guide the user through a planning conversation. Ask these questions ONE AT A TIME and wait for each answer:

1. **Goal**: "What is the main goal? (1-2 sentences)"
2. **Current State**: "What is the current state of the system? (brief description)"
3. **Target State**: "What is the target state after this workflow? (brief description)"
4. **Why**: "Why are we doing this? (key benefits, 2-3 bullet points)"
5. **Anti-goals**: "What is this NOT? (things explicitly out of scope, 2-3 bullet points)"
6. **Tasks**: "How many tasks do you think this needs? (Enter 0 if just plan.md is enough, or number 1-10)"

If tasks > 0, for EACH task ask:
- "Task N name? (e.g., 'create-api-project')"
- "Task N goal? (1 sentence)"
- "Task N key steps? (3-7 bullet points, high-level only)"
- "Task N how to test? (optional, 1-2 sentences)"

# Step 5: Ask for Workflow Name

Ask: "What should we name this workflow? (lowercase-with-dashes, e.g., 'state-broadcasting')"

# Step 6: Create Structure

Create the workflow folder and files:

**Folder**: `docs/workflow/NNN-workflow-name/`

**Files to create**:

1. **plan.md** - Follow this structure:
```markdown
# [Workflow Title]

## Goal

[1-2 sentence goal]

## Current State

[Brief description of current state]

## Target State

[Brief description of target state]

## Why

[2-3 bullet points of benefits]

## What This Is NOT

[2-3 bullet points of anti-goals]

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

## Test

[Optional: 1-2 lines on how to verify]
```

# CRITICAL RULES

❌ **NEVER write code in markdown files**
❌ **NO code snippets, examples, or implementations**
❌ **NO detailed "how to" - only high-level "what to do"**

✅ **Only high-level steps and concepts**
✅ **Tell WHAT to do, not HOW**
✅ **Short and actionable**
✅ **User will read actual code when implementing**

# Step 7: Confirm Completion

After creating all files, tell the user:

"✅ Created workflow NNN-workflow-name with:
- plan.md
- N task files (or 'plan.md only' if no tasks)

Location: docs/workflow/NNN-workflow-name/

You can now start implementation by following the plan."

# Example Reference

Look at `docs/workflow/001-backend-tick-foundation/` for the pattern to follow:
- Short plan.md with clear structure
- Task files with goal, steps, acceptance criteria
- NO CODE in any markdown files
- High-level and actionable
