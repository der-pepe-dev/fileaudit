namespace FileAudit.Abstractions;

public enum ScanMode
{
    Audit,
    Read
}

public enum ReadMode
{
    Never,
    Unmatched,
    OnFail,
    Always
}

public sealed record ScanOptions(
    ScanMode Mode,
    ReadMode ReadMode,
    bool PluginsAfterReadError,
    int ReadBufferBytes,
    int MaxEventsPerFile,
    string? ToolsPath
);
