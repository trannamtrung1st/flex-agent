# Agent Presence — Agent Core

The **Agent Core** is the primary visual embodiment of the active Flex Agent and the design system's most distinctive product primitive.

It should create the feeling that the intelligence is present in the environment — observing, listening, processing, and responding — without pretending to be a human avatar.

Conversational persona and visual identity are separate concerns. A warm,
distinctive, or person-like communication style does not authorize a human
likeness, a claim of human identity, or real-person impersonation. Until an
owning approved specification says otherwise, preserve visible Agent
attribution and the abstract Agent Core direction in `DS-DEC-4`.

## Form

The core may be a circular/polygonal central field, concentric vector/dither rings, bounded constellation field, sensor/aperture-like form, or a notched intelligence plate for compact layouts.

Prefer abstract machine intelligence over humanoid faces, robot heads, emoji, or generic assistant sparkles.

## AI Observation Glass

Primary Agent-presence and live-interaction surfaces frame the Agent Core inside
a bounded **AI Observation Glass** viewport. The composition should feel like
looking through a spacecraft display at an onboard intelligence that is present
behind or within the interface.

- Use subtle depth, edge reflection, directional blue/cyan illumination,
  constellation fields, and precise framing around the Core.
- Concentrate transparency or glass-like layering inside the viewport; provide
  an opaque, high-contrast fallback.
- Keep state text, controls, transcript content, forms, tables, Evidence, and
  review content on stable readable planes rather than beneath blur or texture.
- The viewport may become more cinematic for live interaction and onboarding;
  administration and audit surfaces use a smaller, calmer expression.
- Do not apply frosted glass, backdrop blur, reflections, or luminous edges to
  every panel. Observation Glass is a focal product primitive, not a universal
  container style.

## Size Classes

- **Micro Core (16–24px):** identity mark in navigation/timeline; static or minimally animated.
- **Compact Core (32–56px):** agent header, session toolbar, inspector.
- **Primary Core (80–160px):** live interaction / voice-focused surfaces.
- **Hero Core (160px+):** onboarding/marketing only.

## Canonical Visual States

The Agent Core uses one canonical visual-state vocabulary across the system:

- **Dormant:** dim neutral/blue field, no repeated motion; used when no active agent interaction is occurring.
- **Ready:** stable low-energy electric-blue structure.
- **Listening:** cyan perimeter/beacon tied to actual microphone/listening state.
- **Processing:** electric-blue propagation/orbit/raster field.
- **Speaking:** blue/cyan directional pulse tied to actual playback.
- **Interrupted:** active propagation stops/splits immediately before resolving to the new floor state.
- **Error:** retain recognizable core shape; add a danger cue and explicit text instead of replacing the whole identity with red.

`Offline` / `Disconnected` is a connectivity condition, not an Agent Core visual state. When connectivity is lost, use the appropriate explicit connectivity label from `status.md`; the core may resolve visually to `Dormant` unless another authoritative interaction state applies.

## Presence Rules

- The core may use `emission-agent` or `emission-live` according to state.
- At most one **Primary Core** dominates a screen.
- At most one primary AI Observation Glass viewport establishes the Agent focal
  plane; subordinate Core instances do not duplicate its cinematic treatment.
- Micro/Compact Core instances remain subordinate.
- Do not animate an idle core merely to make the UI feel alive.
- The same agent preserves a recognizable core identity across sessions; configured variation is allowed, random per-render identity is not.
- Important state remains understandable without color/animation and is paired with text.

## Interaction

The core is not automatically a button. If interactive, provide explicit affordance, keyboard behavior, accessible naming, and a clear action.

## Integration

`voice.md` controls live semantics; `dither.md` field behavior; `motion.md` transitions; `status.md` visible state labels; `conversation.md` turn identity.
