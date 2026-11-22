# CLAUDE.md

See `README.md` for layout, commands and the dev database workflow. Product UI is Hungarian; code
and comments are English.

## Conventions

- **No comment spam.** Readable code needs no explanatory comments; make code readable instead of
  explaining it. Comment only what the code cannot say: a workaround, a surprising constraint, a
  non-obvious "why". No doc comments restating names, no banners, no narration in configs, scripts
  or Dockerfiles.
- Backend layering: `egeszsegkalauz.api` (thin controllers, DI, startup) → `egeszsegkalauz.services`
  (all logic; DTOs one per file in `DTO/`) → `egeszsegkalauz.data` (EF Core, models, migrations).
  Controllers hold no business logic and never return `object`.
- Package versions live only in `egeszsegkalauz.app/Directory.Packages.props` (central package
  management); shared MSBuild settings in `Directory.Build.props`.
- Entities derive from `Entity`: the integer `Id` stays inside `egeszsegkalauz.data`; the API exposes
  `Nanoid`. `CreatedAt`/`UpdatedAt` are stamped by `AppDbContext` — don't set them by hand.
- Swagger marks non-nullable C# properties as required (`RequireNonNullablePropertiesSchemaFilter`), so
  nullability in DTOs is what the generated TypeScript types get.
- `client/src/app/api/` is generated: after any controller or DTO change run `./regen-api.sh`.
- Dev fixtures belong in `DevDataSeeder`, or in a `dev-data/dev.dump` made with `./dev-db.sh dump`.

## Gotchas

- Docker must be running: the dev database and the backend tests both use Testcontainers.
- Every backend restart resets the dev database unless `DevDatabase:PersistentVolume` is set.
- The dev DB binds the fixed host port 54320; a force-killed run holds it for a few seconds (the API
  retries startup).
