namespace FileAudit.Abstractions;

public enum FileStatus
{
    Ok,
    Warn,
    Fail,
    Skip
}

public sealed record FileReport(
    string File,
    DateTime Utc,
    ScanMode Mode,
    FileStatus Status,
    IReadOnlyList<string> VerifiersRun,
    IReadOnlyList<DefectEvent> Events,
    string? Coverage = null,
    string? ReadMode = null,
    string? ReadPhase = null,
    bool? ReadPerformed = null,
    bool? IoFailed = null,
    bool? PluginsRanAfterIoError = null
);
