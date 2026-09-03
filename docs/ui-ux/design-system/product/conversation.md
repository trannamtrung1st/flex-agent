# Conversation Surface

Flex Agent conversations are structured, auditable interactions with an onboard
intelligence — not consumer chat.

The participant should feel they are working at a **session console**, while
the transcript remains readable and reviewable for long sessions.

## Default Composition

A live interaction surface typically contains:

1. compact session/agent telemetry on the instrument rail
2. Agent Core in the examiner plate
3. transcript ledger
4. composer with commit key
5. optional contextual task/evidence panel

Use a **transcript ledger of channel plates**, not left/right speech bubbles.
Agent plates align start with a restrained amber wash; Participant plates align
end with teal glass. Hierarchy comes from speaker mark, warmed border, and
Bright Text on the active turn — not a type-size jump and not chat bubbles.

The examiner plate establishes presence; the ledger and composer remain on
stable readable planes. Do not place long conversation text beneath glass blur,
reflection, dither, or animated fields.

## Conversational Turn Anatomy

Each turn contains speaker identity, useful time/sequence metadata, content,
attachments/structured content, optional references/evidence, and permitted
turn actions. Index badges are tabular mono inside the plate.

## Agent Turn

- Agent name plus identity mark linked to the Agent Core
- current/streaming turn may warm the border and speaker mark (`is-active` on
  the latest live turn; drop the mark when the Session is terminal)
- reading typography: 15–18px; max width ~68–78ch
- streaming marker only at the current generation boundary
- arriving turns may use a 640ms phosphor sweep; reduced motion cuts to static
- after durable Agent text is present, production may reveal it at a bounded
  character cadence so a chunked SSE delivery still reads as incremental.
  That cadence presents committed text only (`UI-SESS-DEC-6`); it must not
  invent tokens. Reduced motion shows the full committed string immediately
- a sealed Session replaces live composer chrome with a centered complete
  plate (`ledger-complete`, max 560px): seal, **Session Complete**, two
  consequence lines, Record · Sealed, and **Return to Assignment**. Never
  imply a score or Result

## Participant Turn

- participant name or configured anonymized label
- same readable hierarchy as Agent content
- teal glass rather than a competing brand bubble

## Input zone

Bezeled composer slot, Bright Text, amber caret, commit key sharing the
trailing edge. Connection readout uses teal signal bars plus text (for example
connected / reconnecting), never a hex-only icon.

Do not sacrifice familiar input affordances for theatrical styling. Send,
pause, and completion follow the Text Session specification and runtime
(`PC-08`).

## Actions

Copy, cite, inspect, retry, or review actions should be quiet keys on
focus/hover or in a small action row.

## Multi-Participant

Use consistent speaker labels and role metadata. Color may help but cannot be
the only speaker cue.
