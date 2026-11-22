# Egészségkalauz

Angular frontend + ASP.NET Core (.NET 10) backend on PostgreSQL via Entity Framework Core.

```
egeszsegkalauz.app/            .NET solution (egeszsegkalauz.slnx)
  egeszsegkalauz.api/          controllers, Swagger, startup
  egeszsegkalauz.services/     business logic, DTOs
  egeszsegkalauz.data/         EF Core DbContext, models, migrations, dev database
  egeszsegkalauz.tests/        NUnit, against a real PostgreSQL in Docker
  dev-data/                    optional dev.dump restored into the dev DB on start
client/                        Angular 22 (zoneless, signals, Tailwind 4 + daisyUI)
regen-api.sh                   regenerate client/src/app/api from Swagger
dev-db.sh                      dump / reset / psql for the dev database
```

## Requirements

.NET 10 SDK, Node 24 + npm, Docker (running). No local PostgreSQL needed.

## Run locally

```bash
# backend — http://localhost:5130
cd egeszsegkalauz.app/egeszsegkalauz.api && dotnet run --launch-profile http

# frontend — open http://localhost:4200
cd client && npm install && npm start
```

Use the app on http://localhost:4200 only: `ng serve` proxies `/api` and `/swagger` to the backend
(`client/proxy.conf.json`), so client and API share one origin and cookies work without CORS. The
client calls the API with relative URLs. Swagger UI: http://localhost:4200/swagger.

### The dev database

In Development with an empty `ConnectionStrings:Default`, the API starts a throwaway
`postgres:17-alpine` container through Testcontainers on `localhost:54320`
(user/password `postgres`, database `egeszsegkalauz`). On every start:

1. if `egeszsegkalauz.app/dev-data/dev.dump` exists, it is restored (`pg_restore` runs inside the container);
2. EF migrations are applied;
3. `DevDataSeeder` fills the database **if it is still empty**.

Seeded users: `admin@egeszsegkalauz.local` and `teszt.elek@egeszsegkalauz.local`, password `Jelszo123!`.

The container is removed when the API stops, so **every restart resets the data**. To keep a data set:

```bash
./dev-db.sh dump    # snapshot the running dev DB -> dev-data/dev.dump (restored on every start)
./dev-db.sh reset   # delete the dump -> back to migrations + seeder only
./dev-db.sh psql    # shell into the running dev DB
```

To keep data across restarts instead, set `DevDatabase:PersistentVolume` in
`appsettings.Development.json` to a Docker volume name. To use a real PostgreSQL, set the connection
string: `dotnet user-secrets set ConnectionStrings:Default "Host=...;..."` in `egeszsegkalauz.api`.

The first EF start logs one `fail: ... SELECT "MigrationId" ...` line: EF probing a
`__EFMigrationsHistory` table that does not exist yet. That is expected.

## Authentication

`POST /api/auth/register` creates an account (password of 8–128 characters; email addresses are not
verified yet) and signs it in like a login does. A taken email, in any letter case, returns 409.

`POST /api/auth/login` checks the password (ASP.NET Core Identity's `PasswordHasher`) and sets two
`HttpOnly; Secure; SameSite=Strict` cookies scoped to `/api`:

- `access_token`: a JWT (HS256) valid for 15 minutes; its `sub` claim is the user's nanoid;
- `refresh_token`: a random 256-bit value valid for 30 days. Only its SHA-256 hash is stored, in
  `RefreshTokens`.

The client never refreshes anything itself. When a request arrives without a valid access token but
with a refresh token, `TokenRefreshMiddleware` rotates the refresh token and serves the request with a
fresh pair of cookies. Requests that race with the same refresh token within 30 seconds get a new access
token only. A rotated refresh token presented after that is treated as stolen, and every session of
that user is revoked. `POST /api/auth/logout` revokes the refresh token and clears the cookies, and
`GET /api/auth/me` returns the signed-in user. Lifetimes are set in the `Auth` config section.

Every dev DB reset drops all refresh tokens, so after a backend restart you have to log in again.

## Roles and permissions

Permissions are the members of the `Permission` enum (`egeszsegkalauz.data/Models/Permission.cs`).
Roles are created in the admin panel (`/admin/szerepkorok`) as named sets of permissions, and a user
can hold any number of roles (`/admin/felhasznalok`); they get the union of their permissions.
Endpoints declare what they need with `[HasPermission(Permission.X)]`. The check reads the user's
roles from the database on every request, so role changes apply immediately. Missing permission → 403.

The built-in `Admin` role (created by a migration) always has every permission, including ones added
later. It cannot be edited or deleted, and the last user holding it cannot lose it. In development,
`admin@egeszsegkalauz.local` is an admin and `teszt.elek@egeszsegkalauz.local` has a read-only
`Megtekintő` role. In production, set `Bootstrap__AdminEmail`: at startup, if nobody has the Admin
role yet, that (already registered) user gets it.

Adding a permission: add the enum member, a Hungarian label in
`client/src/app/auth/permission-labels.ts` (the build fails without it), and run `./regen-api.sh`.
Permissions are stored by name, so renaming or removing a member needs a migration that rewrites
`Roles.Permissions`.

## Common tasks

```bash
# new migration (run in egeszsegkalauz.app/; dotnet-ef is a local tool: `dotnet tool restore` once)
dotnet ef migrations add <Name> --project egeszsegkalauz.data --startup-project egeszsegkalauz.data

# regenerate the Angular API client after any controller/DTO change (starts the backend if needed)
./regen-api.sh

# tests
cd egeszsegkalauz.app && dotnet test        # needs Docker
cd client && npm test
```

`client/src/app/api/` is generated — never edit it by hand. Call endpoints through `Api`:
`inject(Api).invoke(usersList)`.

## Docker image

One image serves both: the API on `/api`, the built Angular app from `wwwroot` for everything else
(unknown client routes fall back to `index.html`, unknown `/api` routes are 404).

```bash
docker build -t egeszsegkalauz .
docker run -p 8080:8080 -e ConnectionStrings__Default="Host=...;..." egeszsegkalauz
```

Outside Development the API refuses to start without `ConnectionStrings__Default` and an
`Auth__SigningKey` of at least 32 characters (e.g. `openssl rand -base64 48`). Swagger is only served
in Development.
