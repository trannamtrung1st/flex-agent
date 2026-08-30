-- Store encrypted provider ID token ciphertext solely for RP-initiated logout
-- id_token_hint. Cleared when the live application session terminates.

ALTER TABLE application_sessions
    ADD COLUMN provider_id_token_ciphertext BYTEA NULL;
