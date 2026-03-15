# .NET Initiative Progression

This board is the lightweight visual overlay for the `.NET` initiative.

Authoritative note:
- This file is the portable source of truth for progression-visual prompting behavior in this initiative.
- Do not rely on Copilot memory for this rule; keep the rule here and in `.squad/decisions.md`.

Usage rule:
- When asking Matthew to choose among work progression, prioritization, delegation, creation, or related next-step concerns, reference this board and the relevant node IDs.
- Keep statuses limited to `done`, `in progress`, `next`, `not started`, or `deferred`.
- Prefer updating this board after meaningful work batches rather than tracking every micro-task.

## Current Board

```mermaid
flowchart LR
  P0C1["P0-C1\nBridge contract hardening\nDONE"] --> P0C2["P0-C2\n.NET runtime/config scaffold\nDONE"]
  P0C2 --> P0C3["P0-C3\nFirst proof examples + CI lane\nDONE"]
  P0C3 --> P1A1["P1-A1\nExecutable acquisition hardening\nDONE"]
  P0C3 --> P1B1["P1-B1\nC# typegen deepening\nDONE"]
  P1A1 --> P1A2["P1-A2\nBroader proof matrix\nDONE"]
  P1B1 --> P2B1["P2-B1\nMSBuild integration\nNEXT"]
  P1A2 --> P2A1["P2-A1\nReload/options/hosting helpers\nDONE"]
  P2A1 --> P3A1["P3-A1\nWider platform proof\nNOT STARTED"]
  P2B1 --> P3A1
  P3A1 --> P4A1["P4-A1\nAnalyzer/native-runtime decisions\nDEFERRED"]

  classDef done fill:#d1fae5,stroke:#047857,color:#064e3b;
  classDef next fill:#fef3c7,stroke:#b45309,color:#78350f;
  classDef progress fill:#dbeafe,stroke:#1d4ed8,color:#1e3a8a;
  classDef idle fill:#f3f4f6,stroke:#6b7280,color:#111827;
  classDef deferred fill:#fee2e2,stroke:#b91c1c,color:#7f1d1d;

  class P0C1,P0C2,P0C3,P1A1,P1B1,P1A2,P2A1 done;
  class P2B1 next;
  class P3A1 idle;
  class P4A1 deferred;
```

## Node Glossary

- `P0-C1`: Bridge contract hardening in the CLI and machine-readable fixtures.
- `P0-C2`: Startup-only `.NET` runtime and configuration provider scaffold.
- `P0-C3`: Initial runnable examples, proof command, and one CI lane.
- `P1-A1`: Executable acquisition and version-handshake hardening.
- `P1-B1`: C# type generation deepening beyond the initial specimen.
- `P1-A2`: Broader proof matrix expansion after acquisition hardening.
- `P2-A1`: Reload, options, and hosting-helper work.
- `P2-B1`: MSBuild integration for generated C# output.
- `P3-A1`: Wider platform and framework proof coverage.
- `P4-A1`: Deferred analyzer or native-runtime decisions.
