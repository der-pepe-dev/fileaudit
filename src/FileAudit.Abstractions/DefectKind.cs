namespace FileAudit.Abstractions;

public enum DefectKind
{
    IoReadError,
    AccessDenied,
    ExternalToolMissing,
    ExternalToolFailed,
    NoVerifierMatched,
    ParseError,
    CrcMismatch,
    TrailingData,
    DecodeWarning,
    DecodeError,
    ConcealmentDetected,
    IntegrityCheckFailed,
    UnsupportedFormat
}
