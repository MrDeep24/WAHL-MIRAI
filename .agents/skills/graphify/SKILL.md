---
name: graphify
description: Query or refresh the WAHL-MIRAI Graphify knowledge graph. Use for repository architecture questions and after code changes.
---

# Graphify

For repository architecture, structure, component, or implementation-location questions, query the versioned graph first whenever `graphify-out/graph.json` exists:

- Run `graphify query "<question>"` or use the Graphify MCP `query_graph` tool.
- Use `graphify path "<A>" "<B>"` / `shortest_path` for relationships and `graphify explain "<concept>"` / `get_node` for a focused concept.
- If `graphify-out/wiki/index.md` exists, use it for broad navigation. Read `graphify-out/GRAPH_REPORT.md` only for broad architecture review or when graph queries are insufficient.
- Read source files when implementing/debugging, or when the graph is missing or stale.

`graphify-out/` is versioned. Do not rebuild it just to query it. After code changes, refresh it only through the repository script from the repository root:

```powershell
.\scripts\graphify-refresh.ps1
```

Do not run `graphify update` directly. The script runs the supported forced update and invokes its rebuild fallback if the graph file was not refreshed.

When invoked as `/graphify` without a path in this repository, treat it as a request to refresh this repository's graph with that script; do not run Graphify's generic full extraction pipeline.

To install Graphify when needed, follow the optional Graphify setup in `README.md` (`uv tool install graphifyy --with tree-sitter-sql`). The package is `graphifyy`; the CLI is `graphify`.
