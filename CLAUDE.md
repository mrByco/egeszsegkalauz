# CLAUDE.md

See `README.md` for layout, commands and the dev database workflow. Product UI is Hungarian; code
and comments are English.

## Conventions

- **No comment spam.** Readable code needs no explanatory comments; make code readable instead of
  explaining it. Comment only what the code cannot say: a workaround, a surprising constraint, a
  non-obvious "why". No doc comments restating names, no banners, no narration in configs, scripts
  or Dockerfiles.
- Backend layering: `egeszsegkalauz.api` (thin controllers, DI, startup) → `egeszsegkalauz.services`
  (all logic; DTOs one per file in `DTO/`) → `egeszsegkalauz.data` (EF Core, models, migrations) and
  `egeszsegkalauz.integrations` (external sources, no database access). Controllers hold no business
  logic and never return `object`.
- Each external health data source lives in its own folder in `egeszsegkalauz.integrations`, behind an
  interface (`I<Source>AdapterService`), with all of its wire models in one `<Source>Models.cs`. Adapters
  return the source-neutral types from `Common/`, never their wire models, plus the verbatim payload as
  `RawRecord`s. Pacing, retries and rate-limit handling are the adapter's (`DailyImportPolicy`,
  `IntegrationRateLimitedException`); the generic queue in the services only follows them.
- DTOs and service results are classes with one property per value, `required` on every non-nullable
  one. No positional (inline) records.
- No primary constructors: write an explicit constructor and keep dependencies in
  `private readonly IFoo foo;` fields (no underscore prefix), assigned with `this.foo = foo;`.
- Public methods and methods with business logic get a block body. Expression bodies (`=>`) are for
  properties and tiny private helpers only.
- Nullable reference types are on and violations fail the build; don't silence them with `!`.
- Package versions live only in `egeszsegkalauz.app/Directory.Packages.props` (central package
  management); shared MSBuild settings in `Directory.Build.props`.
- Entities derive from `Entity`: the integer `Id` stays inside `egeszsegkalauz.data`; the API exposes
  `Nanoid`. `CreatedAt`/`UpdatedAt` are stamped by `AppDbContext` — don't set them by hand. Exception:
  high-volume time series (e.g. `HeartRateSample`) use a natural composite key instead.
- Swagger marks non-nullable C# properties as required (`RequireNonNullablePropertiesSchemaFilter`), so
  nullability in DTOs is what the generated TypeScript types get.
- `client/src/app/api/` is generated: after any controller or DTO change run `./regen-api.sh`.
- Client and API share one origin (dev: `ng serve` proxy on :4200; prod: the API serves the client
  from `wwwroot`). Keep API routes under `/api`, call them with relative URLs, and don't add CORS.
- Authorization is permission-based: guard endpoints with `[HasPermission(Permission.X)]`, never with
  role names. On the client use `auth.can('X')` and `permissionGuard('X')`.
- Services signal errors by throwing `NotFoundException` (404), `ConflictException` (409) or
  `InvalidInputException` (400); adapters throw `IntegrationUnavailableException` (502).
  `ServiceExceptionHandler` turns them into ProblemDetails. Controllers don't catch them.
- Secrets (third-party API keys) are never committed: dev values go in the API's user-secrets, stored
  values are encrypted with `IApiKeyProtector`.
- Dev fixtures belong in `DevDataSeeder`, or in a `dev-data/dev.dump` made with `./dev-db.sh dump`.

## Gotchas

- Once an action has any `[ProducesResponseType]` (including class-level ones), ASP.NET stops inferring
  the 200 response from the return type: add `[ProducesResponseType<T>(StatusCodes.Status200OK)]`, or
  the generated client returns `void`.
- Docker must be running: the dev database and the backend tests both use Testcontainers.
- Every backend restart resets the dev database unless `DevDatabase:PersistentVolume` is set.
- The dev DB binds the fixed host port 54320; a force-killed run holds it for a few seconds (the API
  retries startup).
