namespace egeszsegkalauz.data.Dev;

public class DevDatabaseOptions
{
    public const string SectionName = "DevDatabase";

    public string Image { get; set; } = "postgres:17-alpine";
    public int HostPort { get; set; } = 54320;
    public string Database { get; set; } = "egeszsegkalauz";
    public string DumpPath { get; set; } = "../dev-data/dev.dump";
    public string? PersistentVolume { get; set; }
}
