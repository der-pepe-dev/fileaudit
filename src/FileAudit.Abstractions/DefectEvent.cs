namespace FileAudit.Abstractions;

public sealed record DefectEvent(
    Severity Severity,
    DefectKind Kind,
    string Code,
    string Message,
    string? Location = null,
    string? Tool = null
);
