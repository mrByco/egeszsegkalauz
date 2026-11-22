namespace egeszsegkalauz.data.Models;

public abstract class Entity : IHasTimestamps
{
    public int Id { get; set; }
    public string Nanoid { get; set; } = NanoidDotNet.Nanoid.Generate(size: 12);
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
