# Shared design-system implementation

Production-safe Shipboard Terminal modules. Approved design-system v1.0 is
visual acceptance authority. This tree owns promoted component implementations
for both production features and the design lab.

```text
foundations/   # re-exports shared lib helpers; CSS lives in web/src/styles/
components/    # keys, chrome, fields, overlays, navigation, tables, …
patterns/      # cross-component recipes (table actions and selection)
```

Rules:

- Generic typed props only. No fixtures, demo query params, or route-owned
  business state.
- Production and the isolated lab may import this tree.
- This tree must not import lab fixtures, lab routes, or lab surfaces.
- Candidate CSS loads `web/src/styles/shared.css` only. Lab-only demo/surface
  sheets stay behind `web/src/styles/design-lab.css`.
