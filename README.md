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
# backend — http://localhost:5130, Swagger UI at /swagger
cd egeszsegkalauz.app/egeszsegkalauz.api && dotnet run --launch-profile http

# frontend — http://localhost:4200
cd client && npm install && npm start
```

### The dev database

In Development with an empty `ConnectionStrings:Default`, the API starts a throwaway
`postgres:17-alpine` container through Testcontainers on `localhost:54320`
(user/password `postgres`, database `egeszsegkalauz`). On every start:

1. if `egeszsegkalauz.app/dev-data/dev.dump` exists, it is restored (`pg_restore` runs inside the container);
2. EF migrations are applied;
3. `DevDataSeeder` fills the database **if it is still empty**.

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

## Docker images

```bash
docker build -t egeszsegkalauz-api egeszsegkalauz.app                  # needs ConnectionStrings__Default at runtime
docker build --build-arg API_URL=https://api.example.com -t egeszsegkalauz-client client
```

Outside Development the API refuses to start without `ConnectionStrings__Default`, and
`Cors__Origins__0=...` must list the frontend origin.
