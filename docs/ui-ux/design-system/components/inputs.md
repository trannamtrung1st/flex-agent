# Inputs

Inputs should resemble integrated instrument controls: dark, precise, quiet at rest, clearly energized on focus.

## Core Specs

- height: 36px workspace / 40px interaction
- radius: sm
- border: 1px solid border-strong
- background: surface-inset or surface-primary according to composition
- text: fg-strong
- font: 14px
- padding: 10–12px horizontal
- placeholder: fg-subtle

## Label

- font: 13–14px, 500–600
- color: fg-default
- margin-bottom: 6px
- required/optional marker uses text, not color only

## States

### Hover

- border: border-hover

### Focus

- border: border-focus
- scanner focus ring from `interaction-states.md`
- dark mode may use faint `emission-focus`; never glow entered text

### Error

- border: border-danger
- helper text: fg-danger
- include error icon/text when appropriate

### Success

Use only when successful validation needs to be communicated; do not green-outline every valid field.

### Disabled

- background: surface-disabled
- text: fg-disabled
- border: border-subtle
- no emission

## Textareas

- minimum height: 96px
- long-form configuration inputs may use 160–320px
- vertical resize preferred where allowed
