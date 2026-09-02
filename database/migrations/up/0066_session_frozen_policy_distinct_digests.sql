-- Resolved configuration digest is independent of the frozen runtime-policy digest.
-- Do not edit 0001-0065.

ALTER TABLE session_frozen_policy_snapshots
    DROP CONSTRAINT chk_session_frozen_policy_snapshots_digest_lowercase;

ALTER TABLE session_frozen_policy_snapshots
    ADD CONSTRAINT chk_session_frozen_policy_snapshots_digest_lowercase
        CHECK (
            configuration_digest = lower(configuration_digest)
            AND policy_digest = lower(policy_digest)
            AND char_length(configuration_digest) = 64
            AND char_length(policy_digest) = 64
        );
