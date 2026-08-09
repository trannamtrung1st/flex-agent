# Dither, Vector Fields & Agent Identity

Flex Agent uses a controlled **constellation/vector field** as a signature
visual language. It represents Agent identity, computation, memory, transition,
and presence.

The field should feel like an advanced onboard intelligence rendered through instrumentation — not retro pixel art and not a decorative starfield wallpaper.

## Allowed Uses

- Agent Core
- agent identity artwork
- onboarding/login
- empty states
- processing/thinking state
- live voice visualization
- memory/knowledge visualization
- subtle masks around high-salience identity regions
- documentation/marketing artwork

## Prohibited Uses

- behind transcript text
- behind form fields
- beneath tables/evidence
- beneath dense navigation
- as a full-page animated starfield on application screens

## Field States

- **Dormant:** sparse, dim points; core barely energized.
- **Ready:** stable regular constellation with faint blue center/rail.
- **Listening:** local cyan density responds to real audio energy.
- **Processing:** blue points phase, orbit, or propagate through a bounded region.
- **Speaking:** directional expansion/pulse follows actual playback state.
- **Interrupted:** propagation stops or splits immediately.
- **Dormant after completion:** stable low-energy structure.

## Geometry

For application-scale fields: base grid approximately 6–10px, mark size 1–2px, generally low opacity, and field boundaries masked before readable content. Direction may align with panel edges, rails, circular core geometry, or data flow.

Use `brand-primary`, `brand-signal`, `brand-live`, `brand-violet` sparingly, and neutral foreground tokens. Blue dominates; violet is secondary and rare.

## Accessibility

The field is decorative unless paired with explicit state text. Never rely on pattern alone to communicate listening, processing, failure, or completion. Under reduced motion, resolve animated fields to a stable pattern plus the same visible state label.
