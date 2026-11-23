namespace egeszsegkalauz.integrations.Common;

// What a source returned, verbatim: records to keep and records the source no longer has.
public class SyncChunk
{
    public List<RawRecord> Records { get; init; } = [];
    public List<RawRecordKey> Deleted { get; init; } = [];
}
