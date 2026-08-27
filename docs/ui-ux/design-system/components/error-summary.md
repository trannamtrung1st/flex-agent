# Error summary

When a form or command fails validation, present a named error summary before
the fields, then field-level amber errors. Follow the governing UI spec for
focus move, announcement, and preservation of input.

- Summary uses an attention advisory strip (amber triangle + text), not a red
  banner identity.
- Each item links to the invalid control.
- Server-bounded reason categories stay opaque; do not leak internals.
- Duplicate-submit and pending states disable the commit key and show occupied
  wait, not a second error.
