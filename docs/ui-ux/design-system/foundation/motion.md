# Motion

Motion makes the console feel responsive. It must be tied to state, causality,
or navigation.

## Timing

| Interaction | Duration | Easing |
| --- | ---: | --- |
| hover / selection | 150–160ms | `--ease-out` `cubic-bezier(0.16, 1, 0.3, 1)` |
| press feedback | 80–120ms | same |
| focus / bezel shift | 160–180ms | same |
| panel / popover / toast | 240–320ms | same |
| gangway width | 240ms | same; disabled under reduced motion |
| bulkhead / dialog | 320ms | same |
| authored reveal (plate, ledger turn, seal-sweep) | 640ms | clip-path or gradient; one at a time |
| Agent Core color/state | 480ms | color; pulse period shortens while processing |

Avoid playful bounce/spring behavior in operational work surfaces.

## Functional Motion

Allowed continuous/repeated motion includes Agent Core activity tied to
processing/speaking, wait-mark scan while work is actually in progress, scan-
track indeterminate fill, streaming markers at the current generation
boundary, and a brief directional reveal when a record opens.

Wait is never a spinner. Use wait-mark, scan-track, skel-stack, and wait-plate
from [empty/loading](../product/empty-loading.md).

## Agent Core Motion

- **Ready / idle:** nearly still; slow teal aura (about 6.2s / 3.55s).
- **Processing:** same silhouette; shift toward amber; faster pulse
  (about 2.13s / 1.8s).
- **Speaking / streaming:** directional pulse tied to actual playback or
  token stream, plus visible state text.
- **Interrupted:** active motion cuts immediately, then resolves to the new
  floor state.
- Listening/voice motion applies only when an approved voice specification
  is in scope; MVP text Session does not imply a microphone beacon.

## Reduced Motion

Respect `prefers-reduced-motion`. Replace sweeps, pulses, and width
transitions with stable geometry, state text, and simple opacity changes. Wait
instruments remain seated (node fully lit, scan line centered, indeterminate
fill parked). Authored 640ms clip-path reveals cut straight to the end state.

## Prohibited

- unrelated ambient floating particles
- idle full-screen starfields in core application screens
- looping decorative gradient motion
- random pulsing of idle controls
- constant scan lines across readable content
- circular spinners or bouncing dots
- unnecessary spring/bounce interactions
