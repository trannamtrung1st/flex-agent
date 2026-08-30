# Accordion

Use hairline-underlined disclosures for catalog indexes and progressive
administrative sections. One open group at a time is allowed on narrow catalog
rails.

There is **no promoted `Accordion` primitive** in `web/src/design-system/`.
Catalog indexes and optional gangway groups compose native
`<details>`/`<summary>` through `SectionedNavigation`. Gallery:
`nav-groups`.

- Button is the disclosure trigger; heading semantics stay in the trigger or
  wrap it correctly.
- Chevron via Lucide; teal when expanded.
- Do not use accordion to hide primary Participant actions.
