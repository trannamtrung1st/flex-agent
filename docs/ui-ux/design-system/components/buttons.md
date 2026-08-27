# Buttons (keys)

Buttons are engraved **console keys**: uppercase captions, 1px bezels, zero
radius, no fills at rest. They are not glossy consumer CTAs. Semantic roles
below map to Shipboard variants; product labels still come from governing
specs (`PC-10`). Prototype names such as TRANSMIT or APPROVE & RELEASE are not
production copy.

Implementation: `web/src/design-system/components/keys/`. Gallery: `keys`,
`key-group`. `Key` variants (`quiet`, `transmit`, `open`, `begin`, `activate`,
`inspect`, `release`, `back`) are Shipboard skins. `BackKey`, `IconButton`,
and `EllipsisKey` are named keys, not a second button family. Presentational
`ThemeToggle` lives here; production shell may wrap it with the theme hook.

## Core Specs

- Face: Michroma captions; tracking about 0.16em
- Radius: `none`; hot keys may use a 14px angled leading clip
- Border: 1px hairline
- Gap: 8–10px; wait-mark seats in the gap when occupied
- Transition: 150–160ms
- Default minimum target: 36px workspace, 40px interaction; 44px for
  touch-critical, destructive, timed, and primary Participant actions
- Focus-visible: 1px teal outline, 3px offset

## Sizes

| Size | Visual height | Padding | Caption |
| --- | ---: | --- | ---: |
| Compact | 30px | 6px 12px | 0.62–0.68rem |
| Standard / quiet | ≥36px | 10px 20px | 0.68rem |
| Large / ceremony | ≥44px | 12px 24px | 0.75rem |

Compact keys must still meet [accessibility](../foundation/accessibility.md)
target rules via hit area.

## Variants

### Quiet (secondary / tertiary)

Transparent, `fg-muted` text, hairline border. Hover/focus: teal text and
border. Active: Teal Glow fill. Use for Back, Cancel, View, and secondary
workspace actions.

### Commit (primary)

Amber text and border over a faint amber fill. Hover: Amber Bright and
`emission-attention`. Shipboard skins: `transmit`, `begin`, `activate`, `open`,
`inspect`, `release` (notched leading clip). At most **one** commit key is lit
in a region (amber ration). Map to the spec’s primary action (Save draft,
Submit version, Start Attempt, Release Result, and similar). Occupied commit
keys drop amber for teal wait.

### Destructive

Quiet danger text (`fg-danger`) plus a confirmation dialog. The dialog’s
confirm key may use commit anatomy with danger consequence copy. Do not use a
filled red rectangle as the resting identity.

### Live

Reserved for genuine live Session/voice controls when that capability is in
scope. Teal occupation, never decorative.

### Disabled / occupied

Disabled: about 0.4 opacity, no hover. Occupied: `aria-busy`, opacity 1,
wait-mark, teal voice.

## Rules

- Icon-only keys use Lucide or an approved glyph plus an accessible name
  (`IconButton` + `TooltipHost`).
- Truncation (`truncate`) ellipsizes a long caption; it does not stretch keys
  in a `KeyGroup`.
- Do not use pill shapes or permanent outer glow on quiet keys.
- Unapproved export/delete actions are absent or disabled in production
  (`PC-09`).
