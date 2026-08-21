-- Persist activation actor and correlation on attempts and baselines so
-- failure records and required-durable audit can name the authorizing actor.
-- Additive after 0035. Do not edit 0001-0035.

ALTER TABLE assessment_activation_attempts
    ADD COLUMN actor_id UUID,
    ADD COLUMN correlation_id UUID;

UPDATE assessment_activation_attempts
SET actor_id = '00000000-0000-0000-0000-000000000000',
    correlation_id = '00000000-0000-0000-0000-000000000000'
WHERE actor_id IS NULL OR correlation_id IS NULL;

ALTER TABLE assessment_activation_attempts
    ALTER COLUMN actor_id SET NOT NULL,
    ALTER COLUMN correlation_id SET NOT NULL;

ALTER TABLE assessment_activation_baselines
    ADD COLUMN actor_id UUID,
    ADD COLUMN correlation_id UUID;

UPDATE assessment_activation_baselines
SET actor_id = '00000000-0000-0000-0000-000000000000',
    correlation_id = '00000000-0000-0000-0000-000000000000'
WHERE actor_id IS NULL OR correlation_id IS NULL;

ALTER TABLE assessment_activation_baselines
    ALTER COLUMN actor_id SET NOT NULL,
    ALTER COLUMN correlation_id SET NOT NULL;
