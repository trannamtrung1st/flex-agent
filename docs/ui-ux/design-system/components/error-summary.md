# Error summary

When a form or command fails validation, present a named error summary before
the fields, then field-level amber errors. Follow the governing UI spec for
focus move, announcement, and preservation of input.

Implementation: `ErrorSummary` in `web/src/design-system/components/feedback/`.
Gallery: `error-summary` (parts) and `form-recipes` (commission invalid submit).

- Summary uses an attention advisory strip (amber triangle + **Error** label)
  plus a focusable heading (`tabIndex={-1}`), not a red banner identity.
  `role="alert"` on the summary root.
- Default heading copy is **There is a problem** unless the governing spec
  supplies a tighter title.
- Each item may link to the invalid control (`href`) as a `.text-link` (color
  only, no underline).
- Server-bounded reason categories stay opaque; do not leak internals.
- Duplicate-submit and pending states disable the commit key and show occupied
  wait, not a second error.
- After submitted validation or a correctable server failure, focus the summary
  heading once; activating an entry focuses its field.
