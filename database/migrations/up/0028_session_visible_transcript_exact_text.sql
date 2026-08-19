-- Persist permitted participant (and agent transcript) exact text so Invocation
-- context can disclose the words the participant typed. Do not rewrite 0001-0027.

ALTER TABLE session_visible_transcript_items
    ADD COLUMN exact_utf8_text TEXT;
