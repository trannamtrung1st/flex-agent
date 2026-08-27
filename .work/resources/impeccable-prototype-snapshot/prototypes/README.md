# Flex Agent prototypes

Official Flex Agent UI prototypes: a self-contained React 19 + Vite 8 app.
Dev server: `127.0.0.1:5173` (IPv4). Canonical routes:

- `/` — channel index (prototype navigation)
- `/participant-home`
- `/participant-journey`
- `/participant-session`
- `/admin-console` — redirects to `/admin-console/enrollments`
- `/admin-console/campaigns`
- `/admin-console/cohorts`
- `/admin-console/enrollments`
- `/admin-console/sessions`
- `/admin-console/users-access`
- `/admin-console/policies`
- `/admin-console/audit-log`
- `/reviewer-console`
- `/shared/gallery`

Shared CSS lives in `src/styles/`. See `src/components/README.md` for shared-component
ownership and CSS import order.

```bash
pnpm install
pnpm dev
pnpm typecheck
pnpm lint
pnpm test
pnpm build
pnpm test:e2e
```
