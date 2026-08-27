# Agent Presence — Agent Core

The **Agent Core** is the primary visual embodiment of the active Flex Agent
and the design system's most distinctive product primitive.

It should create the feeling that the intelligence is present on the console —
observing, processing, and responding — without pretending to be a human
avatar.

Conversational persona and visual identity are separate concerns. A warm,
distinctive, or person-like communication style does not authorize a human
likeness, a claim of human identity, or real-person impersonation. Preserve the
honest-identity boundary in approved
[`PROP-AGENT-1`](../../../product/concept-model.md#person-like-persona-and-honest-identity-boundary),
visible Agent attribution, and the abstract Agent Core direction in
`DS-DEC-4`. Any later human-likeness treatment requires an owning approved
specification.

## Form

A living phosphor orb: layered radial glows, a hairline ring, and a core dot.
Idle is phosphor teal. Processing shifts the orb toward signal amber while
keeping the same size and silhouette. Prefer abstract machine intelligence over
humanoid faces, robot heads, emoji, or generic assistant sparkles.

## Examiner plate

On Session surfaces, seat the Core in the examiner/instrument plate (smoked
glass, notched bezel) with reassurance copy and chrono instruments. Keep
transcript, composer, forms, tables, Evidence, and review content on stable
readable planes. Do not place those materials beneath blur, reflection, or
animated fields (`DS-DEC-8`).

Administration and audit use a Compact or Micro Core, or omit the Core when
the Agent is not the focal actor.

## Size Classes

- **Micro Core (16–24px):** identity mark in navigation/timeline; static or minimally animated.
- **Compact Core (32–72px):** agent header, session toolbar, narrow examiner band.
- **Primary Core (80–118px):** live interaction surfaces.
- **Hero Core (160px+):** onboarding/marketing only.

## Canonical Visual States

- **Dormant:** dim teal; no repeated motion.
- **Ready:** stable low-energy teal structure.
- **Processing:** amber-shifted pulse tied to actual work.
- **Speaking:** directional pulse tied to actual playback or token stream.
- **Interrupted:** active motion cuts immediately.
- **Error:** retain recognizable core shape; add a danger cue and explicit text.
- **Listening:** only when an approved voice specification is in scope.

`Offline` / `Disconnected` is connectivity, not an Agent Core visual state.
When connectivity is lost, use the explicit connectivity label from
`status.md`; the core may resolve to `Dormant`.

Production Session time and Agent turns are runtime-owned (`PC-08`). Do not
ship the prototype simulator in production.

## Presence Rules

- At most one **Primary Core** dominates a screen.
- Do not animate an idle core merely to make the UI feel alive.
- The same agent preserves a recognizable core identity across sessions.
- Important state remains understandable without color/animation and is paired
  with text.

## Interaction

The core is not automatically a button. If interactive, provide explicit
affordance, keyboard behavior, accessible naming, and a clear action.

## Integration

`voice.md` controls live semantics when in scope; `dither.md` field behavior;
`motion.md` transitions; `status.md` visible state labels; `conversation.md`
turn identity.
