---
name: serena-memory-maintenance
description: Create, update, review, rename, or validate Serena project memories and their references.
---

# Serena memory maintenance

Use this guidance when working with Serena memories:

## Discovery model

- Keep memory discovery progressive: `mem:core` is the top-level entry point and links to memories for major project domains.
- Domain memories should link to more specific memories as needed; set graph depth according to project complexity.
- Group memories by topic or project area using folders where that makes the structure clearer.
- Write references with the `mem:` prefix in backticks and describe what the referenced memory covers.
- Put guidance about when to read a memory in its referring memory, not in the referenced memory itself.

## Style and scope

- Prefer dense, durable agent notes and terse bullets over prose.
- Keep only stable, non-obvious project conventions that prevent costly rediscovery.
- Avoid quick-read facts, generic framework knowledge, one-off task notes, and volatile line-level details.

## Maintenance

- Use Serena's memory rename tool so references are updated automatically.
- Run `serena memories check` to identify stale references, for example after removing a memory.
