# Buttons

Buttons should feel like precise command controls, not glossy consumer CTAs.

## Core Specs

- Font: 14px, 600 weight
- Radius: `sm`
- Border: 1px solid unless ghost
- Icon: 16px default
- Gap: 8px
- Transition: 100–150ms
- Default minimum target height: 36px workspace, 40px interaction
- Touch-critical controls: 44px minimum target

## Sizes

| Size | Height | Horizontal padding | Font |
| --- | ---: | ---: | ---: |
| XS | 28px | 8px | 12px |
| SM | 32px | 10px | 13px |
| Base | 36px | 12px | 14px |
| LG | 40px | 16px | 14px |
| XL | 44px | 18px | 15px |

## Variants

### Primary Command

- background: brand-primary
- text: fg-on-accent
- border: brand-primary
- hover: brand-strong
- active: brand-strong plus subtle inset/1px press feedback
- focus-visible: scanner ring
- no permanent outer glow

### Secondary

- background: surface-secondary or surface-primary according to adjacent plane
- text: fg-strong
- border: border-strong
- hover: surface-hover + border-hover
- active: surface-tertiary

### Tertiary / Ghost

- transparent background/border
- text: fg-default
- hover: surface-hover
- active: surface-tertiary

### Danger

- background: danger
- text: fg-on-accent
- border: danger
- hover/active: danger-strong
- only for destructive primary actions

### Quiet Danger

- transparent or neutral background
- text: fg-danger
- hover/active: danger-soft

### Live / Beacon

- background: brand-live
- text: fg-on-live
- border: brand-live
- hover/active: brand-live-strong
- may use restrained `emission-live` only while genuinely live
- reserved for microphone/listening/speaking/live-session controls

### Disabled

- background: surface-disabled
- text: fg-disabled
- border: border-subtle
- no hover/active/emission

## Rules

- One dominant primary command per local action group.
- Do not use bright blue buttons as decoration or status.
- Icon-only buttons need accessible labels/tooltips when meaning is not universally obvious.
- Destructive actions must not use brand-primary.
- Do not rename ordinary actions with fictional command terminology unless the product language calls for it.
