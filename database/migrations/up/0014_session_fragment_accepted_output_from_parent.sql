-- Derive accepted aout.* from the parent Agent Message. Fragment
-- accepted_agent_output_id was independently nullable, so a parent could
-- keep aout.* while the fragment stored NULL. Frozen 0005-0013 unchanged.
-- UTC-ordered; additive after applied 0013.

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS chk_session_message_fragments_accepted_output;

ALTER TABLE session_message_fragments
    DROP CONSTRAINT IF EXISTS fk_session_message_fragments_accepted_output;

ALTER TABLE session_message_fragments
    DROP COLUMN IF EXISTS accepted_agent_output_id;
