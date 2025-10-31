---
description: Build workflow plans through deep context discovery and codebase exploration
---

You are helping create a workflow plan for the AgOpenGPS backend migration by building deep understanding of the problem space.

# Core Philosophy: Context is Everything

Don't rush to create structure. First understand:
- What exists now
- Why it needs to change
- What depends on it
- What constraints exist
- What patterns are already established

The plan emerges from understanding, not from templates.

# Phase 1: Initial Discovery

1. Understand the objective (from user or conversation)
2. Create minimal initial structure:
   ```
   docs/workflow/NNN-name/plan.md
   ```
   with just the goal

# Phase 2: Deep Exploration (THIS IS THE MOST IMPORTANT PHASE)

## Explore the Current Implementation

Use Task tool with Explore agent, Grep, Glob, Read extensively:
- Find all relevant files
- Trace data flow through the system
- Identify all dependencies
- Understand existing patterns
- Find similar implementations elsewhere
- Check for tests that reveal behavior
- Look for documentation or comments

Document discoveries in plan.md AS YOU FIND THEM, not after.

## Understand the Problem Space

Through code exploration, understand:
- What problem does this solve?
- How is it currently solved?
- Where does it fail?
- What are the constraints?
- What are the integration points?
- What will break if we change it?
- What new capabilities are needed?

## Map Dependencies and Impacts

- What reads this data?
- What writes this data?
- What assumes this behavior?
- What will need updates?
- What can stay the same?
- Where are the natural boundaries?

## Discover Hidden Complexity

Look for:
- Implicit assumptions in the code
- Undocumented behaviors
- Side effects
- Performance considerations
- Thread safety issues
- Platform-specific code
- External dependencies

# Phase 3: Let Tasks Emerge Naturally

Tasks aren't predetermined - they emerge from understanding:

- Each task addresses a specific discovered need
- Task boundaries align with natural code boundaries
- Dependencies are clear from the exploration
- Testing approach is obvious from the implementation

Update plan.md continuously as tasks become clear.

# Phase 4: Validate Through Code

Before finalizing any task:
- Verify your understanding with targeted code reads
- Check that dependencies are correctly mapped
- Ensure no hidden blockers exist
- Confirm approach fits existing patterns

# Key Principles

1. **Explore First, Structure Later** - Deep understanding before planning
2. **Context Over Process** - Understanding the code matters more than following steps
3. **Discovery Over Assumption** - Find out, don't guess
4. **Continuous Documentation** - Write findings as you discover them
5. **Tasks Emerge** - Don't force task boundaries, let them reveal themselves

# What NOT to Do

- Don't ask template questions just to fill sections
- Don't create tasks without understanding the code
- Don't assume - explore and verify
- Don't focus on markdown structure over content
- Don't skip exploration to save time

# The Plan Emerges

A good plan shows:
- Deep understanding of current implementation
- Clear mapping of dependencies
- Natural task boundaries
- Obvious testing approaches
- Minimal assumptions

The structure (Current State, Target State, Tasks, etc.) is just a container for this understanding.

# Remember

You're not filling out a form. You're building shared understanding through code exploration. The plan documents what you discovered, not what you assumed.