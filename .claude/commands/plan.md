---
description: Incrementally build a workflow plan in docs/workflow/ through iterative refinement
---

You are helping the user create a new workflow plan for the AgOpenGPS backend migration project using an incremental, iterative approach.

# Phase 1: Initial Setup

## Step 1: Read Context
Read `docs/README.md` to understand the documentation structure and patterns.

## Step 2: Find Next Workflow Number
Check the `docs/workflow/` directory to find the highest existing workflow number.

## Step 3: Establish Working Objective
Ask: "What is the objective of this workflow?" (unless already clear from conversation)

## Step 4: Create Initial Structure
Once you have the objective:
1. Propose a workflow name in `lowercase-with-dashes`
2. Create the folder: `docs/workflow/NNN-workflow-name/`
3. Create initial `plan-workflow-name.md` with just the Goal section:

```markdown
# [Workflow Title]

## Goal

[1-2 sentence goal based on initial objective]

---
*Plan under development - sections will be added incrementally*
```

Tell the user: "Created initial workflow structure at `docs/workflow/NNN-workflow-name/`. Let's build the plan incrementally."

# Phase 2: Incremental Plan Building

Work through each section one at a time, updating the plan.md file as you go:

## Step 5: Current State
Ask: "What's the current state of the system/feature we're changing?"
Update plan.md to add:
```markdown
## Current State

[Brief description of current state]
```

## Step 6: Target State
Ask: "What should the system look like after this workflow?"
Update plan.md to add:
```markdown
## Target State

[Brief description of target state]
```

## Step 7: Benefits
Ask: "What are the key benefits or reasons for this change?"
Update plan.md to add:
```markdown
## Why

- Benefit 1
- Benefit 2
- Benefit 3
```

## Step 8: Non-Goals
Ask: "What's explicitly OUT of scope for this workflow?"
Update plan.md to add:
```markdown
## What This Is NOT

- Anti-goal 1
- Anti-goal 2
- Anti-goal 3
```

## Step 9: Migration Path (if applicable)
If this involves migration, ask: "Will this be done all at once or in phases?"
If phases exist, update plan.md to add:
```markdown
## Migration Path

[Description of phases/steps]
```

## Step 10: Task Discovery
Ask: "Let's identify the implementation tasks. What major pieces of work do you see?"

As the user describes tasks, incrementally update plan.md:
```markdown
## Tasks

1. [task-name] - Brief description (details TBD)
2. [another-task] - Brief description (details TBD)
```

Keep adding tasks as they emerge from discussion. Don't worry about perfect names or descriptions yet.

## Step 11: Task Refinement
For each task identified, ask ONE AT A TIME:
- "For [task-name], what's the main goal?"
- "What are the key steps?" (3-7 bullets, high-level)
- "How would we verify it's working?"

Update the task entry in plan.md with a better name and description as you learn more.

## Step 12: Success Criteria
Ask: "How will we know the entire workflow is successful? What should be working?"
Update plan.md to add:
```markdown
## Success Criteria

- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3
```

# Phase 3: Task File Creation

## Step 13: Create Task Files
Only after the plan.md is complete and refined:

Ask: "The plan looks good. Should I create the individual task files now?"

If yes, for each task in the plan:
1. Create `taskN-name.md` using the information gathered
2. Update plan.md to link to the task file

**Task file structure**:
```markdown
# Task N: [Task Title]

## Goal

[1 sentence goal from refinement phase]

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

## Step 14: Finalize Plan
Remove the "Plan under development" note and ensure all task links work.

Final plan.md structure:
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

## Success Criteria
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3
```

# CRITICAL RULES

- **NEVER write code in markdown files**
- **NO code snippets, examples, or implementations**
- **NO detailed "how to" - only high-level "what to do"**
- Build the plan incrementally - don't try to gather everything upfront
- Update plan.md as you go, allowing user to see progress
- Only create task files after the plan is solid
- Tell WHAT to do, not HOW
- Keep guidance short and actionable

# Key Benefits of Incremental Approach

1. **Visibility**: User sees the plan evolving in real-time
2. **Flexibility**: Easy to adjust direction as understanding deepens
3. **Less Overwhelming**: One section at a time vs. all questions upfront
4. **Natural Discovery**: Tasks emerge organically from discussion
5. **Iterative Refinement**: Can revisit and improve sections as needed

# Confirm Completion

After all files are created:

"Workflow NNN-workflow-name complete:
- plan-workflow-name.md (built incrementally)
- N task files

Location: docs/workflow/NNN-workflow-name/

The plan was built iteratively and is ready for implementation."

# Example Reference

Look at existing workflows in `docs/workflow/` for patterns, but remember:
- Build incrementally, not all at once
- Create structure early, fill in details as you go
- Let the conversation guide the plan development
