# Voice Interaction

Voice is where Agent presence becomes most tangible. When in scope, map
listening/speaking onto the Shipboard Core (teal live, amber processing) while
preserving exact session semantics. This module does not enable P0 voice.

Voice is deferred from the assessment MVP. Apply this shared visual preparation
only after an approved voice feature and interaction specification define floor
ownership, generated/sent/played/interrupted/cancelled/playback-confirmed
semantics, permissions, failure, and recovery.

## Primary States

- Idle
- Ready
- Listening
- Participant speaking
- Processing
- Agent speaking
- Interrupted
- Paused
- Error

## Agent Core Coupling

Voice-controller states map into the canonical Agent Core visual states from `agent-presence.md`:

- Idle → Dormant
- Paused → Dormant, plus explicit `Paused` session/voice text when applicable
- Ready → Ready
- Listening → Listening
- Participant speaking → Listening with stronger audio-reactive signal
- Processing → Processing
- Agent speaking → Speaking, with directional blue/cyan pulses tied to actual playback
- Interrupted → Interrupted; playback signal stops/fractures immediately and floor state changes
- Error → Error; danger cue plus text, never simply turn the whole core red

## Voice Control

Primary microphone target: 44–52px, circular or tightly rounded, icon 20–24px, explicit label nearby when ambiguity is possible. Live state may use restrained `emission-live`; idle microphone does not continuously glow.

## Partial Transcript / Playback

- provisional speech-to-text appears visibly provisional and is not committed transcript content until finalized
- committed transcript remains stable when partial hypotheses change
- generated and sent Agent content must remain distinguishable from played,
  interrupted, cancelled, and playback-confirmed content
- floor ownership remains explicit during rapid turn changes

## Waveform / Signal Field

Waveform, vector field, or dither activity must reflect actual audio state. A circular/core-based signal visualization is preferred over decorative equalizer bars when it fits the composition.

## Interruptions

When Participant speech interrupts Agent playback: update voice state
immediately, stop speaking-state emission/motion at once, preserve authoritative
event order, and record the played boundary, interruption, cancellation, and
playback-confirmed state separately. Never infer that generated, sent, or
played content was heard or completed.
