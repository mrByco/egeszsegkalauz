using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace egeszsegkalauz.data.Dev;

public sealed class DevDatabase : IAsyncDisposable
{
    public const string ContainerLabel = "egeszsegkalauz.dev-db";

    private const string ContainerDumpPath = "/tmp/dev.dump";

    private readonly PostgreSqlContainer _container;
    private readonly DevDatabaseOptions _options;

    private DevDatabase(PostgreSqlContainer container, DevDatabaseOptions options)
    {
        _container = container;
        _options = options;
    }

    public string ConnectionString => _container.GetConnectionString();

    public static async Task<DevDatabase> StartAsync(DevDatabaseOptions options, string contentRoot)
    {
        var builder = new PostgreSqlBuilder(options.Image)
            .WithDatabase(options.Database)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(options.HostPort, PostgreSqlBuilder.PostgreSqlPort)
            .WithLabel(ContainerLabel, "true");

        if (!string.IsNullOrWhiteSpace(options.PersistentVolume))
            builder = builder.WithVolumeMount(options.PersistentVolume, "/var/lib/postgresql/data");

        var db = new DevDatabase(builder.Build(), options);

        Log($"Starting dev PostgreSQL ({options.Image}) on localhost:{options.HostPort}...");
        await db.StartContainerAsync();

        var dumpPath = Path.GetFullPath(options.DumpPath, contentRoot);
        if (!File.Exists(dumpPath))
        {
            Log($"No dump at {dumpPath} — starting empty; migrations and the seeder will fill it.");
        }
        else if (options.PersistentVolume is not null && await db.HasSchemaAsync())
        {
            Log($"Volume '{options.PersistentVolume}' already has data — skipping dump restore. " +
                $"Remove it to force a fresh restore: docker volume rm {options.PersistentVolume}");
        }
        else
        {
            await db.RestoreAsync(dumpPath);
        }

        return db;
    }

    private async Task StartContainerAsync()
    {
        // A force-killed previous run holds the fixed host port until the reaper removes its container.
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _container.StartAsync();
                return;
            }
            catch (Exception e) when (attempt < maxAttempts)
            {
                Log($"Dev PostgreSQL failed to start (attempt {attempt}/{maxAttempts}): {e.Message}. Retrying...");
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Could not start the dev PostgreSQL container after {maxAttempts} attempts. Is Docker " +
                    $"running, and is port {_options.HostPort} free? (check `docker ps`)", e);
            }
        }
    }

    private async Task RestoreAsync(string dumpPath)
    {
        Log($"Restoring {dumpPath}...");
        await _container.CopyAsync(await File.ReadAllBytesAsync(dumpPath), ContainerDumpPath);

        // Without --if-exists, --clean fails on an empty database and the exit code becomes useless.
        var result = await _container.ExecAsync(
        [
            "pg_restore", "-U", "postgres", "-d", _options.Database,
            "--clean", "--if-exists", "--no-owner", "--no-privileges", ContainerDumpPath,
        ]);

        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"pg_restore of '{dumpPath}' failed with exit code {result.ExitCode}:\n{result.Stderr}");

        Log("Dump restored.");
    }

    private async Task<bool> HasSchemaAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL";
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static void Log(string message) => Console.WriteLine($"[dev-db] {message}");

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
