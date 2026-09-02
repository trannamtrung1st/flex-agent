# Shared design-system implementation

Production-safe Shipboard Terminal modules. Approved design-system v1.1 is
visual acceptance authority. This tree owns promoted component implementations
for both production features and the design lab.

```text
foundations/   # re-exports shared lib helpers; CSS lives in web/src/styles/
components/    # keys, chrome, fields, feedback, overlays, navigation, tables, layout primitives, …
patterns/      # layouts (closed shell set) and table-action recipes
```

Pages and routes must import layout families from `patterns/layouts`, not
assemble `CommandStrip` / `Gangway` / instrument rails themselves. `reference`
is valid only in the design-lab entry graph via `web/src/design-system/lab.ts`.

Rules:

- Generic typed props only. No fixtures, demo query params, or route-owned
  business state. Named assignment and lab-wall chrome lives in
  `web/src/components/work/` or `web/src/design-lab/components/`, not this tree.
- Production and the isolated lab may import this tree.
- This tree must not import lab fixtures, lab routes, or lab surfaces.
- Candidate CSS loads `web/src/styles/shared.css` only. Lab-only demo/surface
  sheets stay behind `web/src/styles/design-lab.css`.
