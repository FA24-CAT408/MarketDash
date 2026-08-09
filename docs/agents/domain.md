# Domain Docs

This repository uses a single-context domain-documentation layout.

## Before exploring

Read these when they exist:

- `CONTEXT.md` at the repository root
- Relevant architectural decisions under `docs/adr/`

If they do not exist, proceed silently. `/domain-modeling`, normally reached
through `/grill-with-docs`, creates them when terminology or architectural
decisions are resolved.

## Layout

```text
/
├── CONTEXT.md
├── docs/
│   └── adr/
└── Assets/
```

## Use the glossary vocabulary

Use terms as defined in `CONTEXT.md` in issues, proposals, hypotheses, and code.
Avoid synonyms that the glossary explicitly rejects.

If a needed concept is absent, reconsider whether it belongs to the existing
language or record the gap for `/domain-modeling`.

## Flag ADR conflicts

If proposed work contradicts an existing ADR, surface the conflict explicitly
instead of silently overriding the decision.
