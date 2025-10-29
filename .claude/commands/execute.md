---
description: Execute a workflow by number (e.g., /execute 005)
---

You are helping the user execute a workflow task. The workflow number is provided as an argument.

# Step 1: Find and Validate Workflow

1. Check the `docs/workflow/` directory for a folder matching the pattern `NNN-workflow-name/` where NNN is the provided workflow number (zero-padded, e.g., `005`).
2. If the workflow folder does not exist, inform the user and list available workflows.
3. If found, proceed to Step 2.

# Step 2: Read Workflow Plan

1. Read the `plan-*.md` file in the workflow folder to understand the objective, current state, target state, and tasks.
2. List all task files (taskN-*.md) in the workflow folder.
3. Identify if this is the first time executing this workflow or if work has already started.

# Step 3: Determine Execution Context

Ask the user (if not clear from context):
- "Is this your first time executing this workflow, or are you continuing from a previous session?"
- If continuing: "What is the current status? Which tasks are complete?"

# Step 4: Create and Switch to Workflow Branch

1. Ensure you are on the `cross-platform-support` branch (check `git branch`).
2. Create a new branch with the format: `workflow/NNN-workflow-name`
3. Switch to the new branch.
4. Provide confirmation of the branch switch.

# Step 5: Read First Task (If Not Already Done)

If no tasks have been started:
1. Read the first task file (task1-*.md or equivalent).
2. Extract: goal, steps, key points, and acceptance criteria.
3. Confirm with the user: "Ready to execute Task 1: [Task Title]?"

# Step 6: Gather Information and Execute Task 1

1. For each step in the task file, gather any missing context or clarification needed from the user.
2. Ask one focused question at a time if information is unclear.
3. Once you have sufficient context, proceed with implementation following the task steps.
4. Use the TodoWrite tool to track progress on sub-steps as you work.
5. Mark the task as complete once all acceptance criteria are met.

# Step 7: Transition to Next Task

After Task 1 completes:
1. Ask: "Ready to move to Task 2?"
2. If yes, read the next task file and repeat Step 6.
3. If no, summarize progress and suggest a follow-up session.

# Important Notes

- **Read the full task before implementing** - understand steps, key points, and acceptance criteria first.
- **Use TodoWrite to track progress** - especially for multi-step tasks.
- **Commit frequently** - create git commits after each task completes (include task number and name in commit message).
- **Ask for clarification** - if any step is ambiguous or has missing context.
- **Respect the plan** - the workflow plan is the source of truth for scope and sequencing.
