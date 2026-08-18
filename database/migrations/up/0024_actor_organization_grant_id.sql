-- Stable grant identity so access-control mutation audit can name the
-- authorizing actor_organization_grants row (REQ-AUTH-27). Do not rewrite
-- frozen 0001-0022. Additive after 0023.

ALTER TABLE actor_organization_grants
    ADD COLUMN IF NOT EXISTS grant_id UUID;

UPDATE actor_organization_grants
SET grant_id = gen_random_uuid()
WHERE grant_id IS NULL;

ALTER TABLE actor_organization_grants
    ALTER COLUMN grant_id SET NOT NULL;

ALTER TABLE actor_organization_grants
    ALTER COLUMN grant_id SET DEFAULT gen_random_uuid();

ALTER TABLE actor_organization_grants
    DROP CONSTRAINT IF EXISTS uq_actor_organization_grants_grant_id;

ALTER TABLE actor_organization_grants
    ADD CONSTRAINT uq_actor_organization_grants_grant_id UNIQUE (grant_id);
