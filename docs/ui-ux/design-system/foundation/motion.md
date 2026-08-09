# Motion

Motion makes the onboard intelligence feel responsive. It must be tied to state, causality, or navigation.

## Timing

| Interaction | Duration |
| --- | ---: |
| hover / selection | 100–150ms |
| press feedback | 80–120ms |
| signal/focus sweep | 140–220ms |
| panel / popover | 160–220ms |
| modal | 180–240ms |
| layout transition | 180–260ms |
| Agent Core state transition | 220–420ms |

Use restrained ease-out for entrances and ease-in-out for state transitions. Avoid playful bounce/spring behavior in operational work surfaces.

## Functional Motion

Allowed continuous/repeated motion includes voice activity while audio is actually active, Agent Core activity tied to listening/processing/speaking, streaming signal markers, actual progress, live recording state, and a brief directional scan when focus/current context changes.

## Agent Core Motion

- **Ready:** nearly still; low-intensity stable field.
- **Listening:** cyan perimeter/local field responds to actual audio energy.
- **Processing:** bounded orbital/raster/dither phase shift; never full-screen.
- **Speaking:** directional pulses synchronized to playback/activity.
- **Interrupted:** active motion cuts/fractures immediately, then resolves to the new floor state.

## Reduced Motion

Respect `prefers-reduced-motion`. Replace sweeps, orbital motion, and waveform animation with stable edge illumination, state text, and simple opacity changes.

## Prohibited

- unrelated ambient floating particles
- idle full-screen starfields in core application screens
- looping decorative gradient motion
- random pulsing of idle controls
- constant scan lines across readable content
- unnecessary spring/bounce interactions
